using EduPlatform.BLL.DTOs.Documents;
using EduPlatform.BLL.Enums;
using EduPlatform.BLL.Exceptions;
using EduPlatform.BLL.Interfaces;
using EduPlatform.BLL.Models;
using EduPlatform.BLL.Options;
using EduPlatform.DAL.Entities;
using EduPlatform.DAL.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pgvector;
using DalCourseType = EduPlatform.DAL.Entities.CourseType;
using DalDocumentStatus = EduPlatform.DAL.Entities.DocumentStatus;
using DalEnrollmentStatus = EduPlatform.DAL.Entities.EnrollmentStatus;
using BllDocumentStatus = EduPlatform.BLL.Enums.DocumentStatus;

namespace EduPlatform.BLL.Services;

public sealed class DocumentService(
    IDocumentRepository documentRepository,
    ICourseRepository courseRepository,
    ITextChunker textChunker,
    IEmbeddingService embeddingService,
    IEnumerable<ITextExtractor> extractors,
    IFileStorageService fileStorageService,
    IOptions<DocumentOptions> options,
    TimeProvider timeProvider,
    ILogger<DocumentService> logger) : IDocumentService
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        "text/plain",
        "text/markdown"
    };

    private readonly IDocumentRepository _documentRepository = documentRepository;
    private readonly ICourseRepository _courseRepository = courseRepository;
    private readonly ITextChunker _textChunker = textChunker;
    private readonly IEmbeddingService _embeddingService = embeddingService;
    private readonly IReadOnlyList<ITextExtractor> _extractors = [.. extractors];
    private readonly IFileStorageService _fileStorageService = fileStorageService;
    private readonly DocumentOptions _options = options.Value;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly ILogger<DocumentService> _logger = logger;

    public async Task<IReadOnlyList<DocumentSummaryDto>> ListByCourseAsync(
        Guid courseId,
        ActorContext actor,
        CancellationToken cancellationToken)
    {
        await EnsureCanReadCourseAsync(courseId, actor, cancellationToken);

        var documents = await _documentRepository.ListByCourseAsync(
            courseId,
            cancellationToken);

        return [.. documents.Select(MapSummary)];
    }

    public async Task<DocumentDetailsDto> GetByIdAsync(
        Guid id,
        ActorContext actor,
        CancellationToken cancellationToken)
    {
        var document = await _documentRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new ResourceNotFoundException("Không tìm thấy tài liệu.");

        await EnsureCanReadCourseAsync(document.CourseId, actor, cancellationToken);

        return MapDetails(document);
    }

    public async Task<IReadOnlyList<DocumentChunkDto>> ListChunksAsync(
        Guid id,
        ActorContext actor,
        CancellationToken cancellationToken)
    {
        var document = await _documentRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new ResourceNotFoundException("Không tìm thấy tài liệu.");

        await EnsureCanReadCourseAsync(document.CourseId, actor, cancellationToken);

        var chunks = await _documentRepository.ListChunksAsync(id, cancellationToken);
        return [.. chunks
            .Select(x => new DocumentChunkDto(
                x.Id,
                x.Sequence,
                x.Content,
                x.PageNumber,
                x.Section,
                x.Embedding is not null,
                EmbeddingData: x.Embedding?.ToArray()))];
    }

    public async Task<Guid> UploadAsync(
        UploadDocumentCommand command,
        ActorContext actor,
        CancellationToken cancellationToken)
    {
        ValidateUpload(command);

        await EnsureCanManageCourseAsync(command.CourseId, actor, cancellationToken);

        var fileType = ResolveFileType(command.ContentType, command.OriginalFileName);
        if (fileType == DocumentFileType.Unknown)
        {
            throw new BusinessValidationException(
                "Định dạng tệp chưa được hỗ trợ. Hãy dùng PDF, DOCX, PPTX, TXT hoặc Markdown.");
        }

        if (await _documentRepository.ExistsByCourseAndFileNameAsync(
                command.CourseId,
                command.OriginalFileName,
                cancellationToken))
        {
            throw new ResourceConflictException(
                "Tài liệu cùng tên đã được tải lên trong khóa học này. "
                + "Hãy đổi tên tệp hoặc xóa bản cũ trước khi tải lại.");
        }

        var extractor = ResolveExtractor(command.ContentType, command.OriginalFileName)
            ?? throw new BusinessValidationException(
                "Định dạng tệp chưa được hỗ trợ. Hãy dùng PDF, DOCX, PPTX, TXT hoặc Markdown.");

        var documentId = Guid.NewGuid();

        using var memoryStream = new MemoryStream();
        await command.Content.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;

        var uploadedPath = await _fileStorageService.UploadAsync(
            memoryStream,
            command.OriginalFileName,
            command.ContentType);

        var document = new Document
        {
            Id = documentId,
            CourseId = command.CourseId,
            UploadedById = actor.UserId,
            OriginalFileName = command.OriginalFileName,
            StorageKey = uploadedPath,
            ContentType = command.ContentType,
            SizeBytes = command.SizeBytes,
            Status = DalDocumentStatus.Pending
        };

        await _documentRepository.AddAsync(document, cancellationToken);
        await _documentRepository.SaveChangesAsync(cancellationToken);

        try
        {
            memoryStream.Position = 0;
            await ProcessAsync(
                document,
                memoryStream,
                extractor,
                cancellationToken);

            await _documentRepository.SaveChangesAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            document.Status = DalDocumentStatus.Failed;
            document.FailureReason = "Quá trình xử lý đã bị hủy.";
            await TrySaveChangesSafelyAsync(cancellationToken);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Document processing failed for document {DocumentId} ({FileName}).",
                document.Id,
                command.OriginalFileName);

            document.Status = DalDocumentStatus.Failed;
            document.FailureReason = ex.Message.Length > 2000
                ? ex.Message[..2000]
                : ex.Message;
            await TrySaveChangesSafelyAsync(cancellationToken);

            throw new DocumentProcessingException(
                "Không thể xử lý tài liệu. Vui lòng kiểm tra nội dung và thử lại.",
                ex);
        }

        _ = fileType;
        return document.Id;
    }

    public async Task DeleteAsync(
        Guid id,
        ActorContext actor,
        CancellationToken cancellationToken)
    {
        var document = await _documentRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new ResourceNotFoundException("Không tìm thấy tài liệu.");

        await EnsureCanManageCourseAsync(document.CourseId, actor, cancellationToken);

        var storagePath = document.StorageKey;
        _documentRepository.Remove(document);
        await _documentRepository.SaveChangesAsync(cancellationToken);

        try
        {
            await _fileStorageService.DeleteAsync(storagePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete document from cloud storage: {StorageKey}", storagePath);
        }
    }

    public async Task<string> GetDownloadUrlAsync(
        Guid id,
        ActorContext actor,
        CancellationToken cancellationToken)
    {
        var document = await _documentRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new ResourceNotFoundException("Không tìm thấy tài liệu.");

        await EnsureCanReadCourseAsync(document.CourseId, actor, cancellationToken);

        return await _fileStorageService.GetDownloadUrlAsync(
            document.StorageKey,
            document.OriginalFileName,
            document.ContentType);
    }

    private async Task ProcessAsync(
        Document document,
        Stream fileStream,
        ITextExtractor extractor,
        CancellationToken cancellationToken)
    {
        var pages = await extractor.ExtractAsync(fileStream, cancellationToken);

        if (pages.Count == 0)
        {
            document.Status = DalDocumentStatus.Failed;
            document.FailureReason = "Tài liệu không chứa văn bản có thể trích xuất.";
            return;
        }

        document.Status = DalDocumentStatus.Processing;
        await _documentRepository.SaveChangesAsync(cancellationToken);

        var chunks = _textChunker.Chunk(
            pages,
            _options.ChunkSize,
            _options.ChunkOverlap);

        if (chunks.Count == 0)
        {
            document.Status = DalDocumentStatus.Failed;
            document.FailureReason = "Không tạo được đoạn văn bản nào từ tài liệu.";
            return;
        }

        var newChunks = new List<DocumentChunk>(chunks.Count);
        for (var index = 0; index < chunks.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var chunk = chunks[index];
            var embedding = await _embeddingService.EmbedAsync(
                chunk.Content,
                cancellationToken);

            newChunks.Add(new DocumentChunk
            {
                DocumentId = document.Id,
                Sequence = index,
                Content = chunk.Content,
                PageNumber = chunk.PageNumber,
                Section = chunk.Section,
                MetadataJson = "{}",
                Embedding = new Vector(embedding)
            });
        }

        await _documentRepository.AddChunksAsync(newChunks, cancellationToken);

        document.Status = DalDocumentStatus.Ready;
        document.FailureReason = null;
    }

    private async Task EnsureCanReadCourseAsync(
        Guid courseId,
        ActorContext actor,
        CancellationToken cancellationToken)
    {
        var course = await _courseRepository.GetByIdAsync(courseId, cancellationToken)
            ?? throw new ResourceNotFoundException("Không tìm thấy khóa học.");

        if (actor.IsAdmin || course.OwnerId == actor.UserId)
        {
            return;
        }

        if (course.IsVisible && course.Type == DalCourseType.Public)
        {
            return;
        }

        var enrollment = await _courseRepository.GetEnrollmentAsync(
            courseId,
            actor.UserId,
            cancellationToken);

        if (enrollment?.Status != DalEnrollmentStatus.Active)
        {
            throw new ForbiddenOperationException(
                "Bạn không có quyền truy cập tài liệu của khóa học này.");
        }
    }

    private async Task EnsureCanManageCourseAsync(
        Guid courseId,
        ActorContext actor,
        CancellationToken cancellationToken)
    {
        var course = await _courseRepository.GetByIdAsync(courseId, cancellationToken)
            ?? throw new ResourceNotFoundException("Không tìm thấy khóa học.");

        if (!actor.IsAdmin && course.OwnerId != actor.UserId)
        {
            throw new ForbiddenOperationException(
                "Chỉ chủ sở hữu khóa học hoặc quản trị viên mới có thể quản lý tài liệu.");
        }
    }

    private void ValidateUpload(UploadDocumentCommand command)
    {
        if (command.CourseId == Guid.Empty)
        {
            throw new BusinessValidationException("Khóa học không hợp lệ.");
        }

        if (string.IsNullOrWhiteSpace(command.OriginalFileName))
        {
            throw new BusinessValidationException("Tên tệp không được để trống.");
        }

        if (command.SizeBytes <= 0)
        {
            throw new BusinessValidationException("Tệp tải lên trống.");
        }

        if (command.SizeBytes > _options.MaxFileSizeBytes)
        {
            throw new BusinessValidationException(
                $"Tệp vượt quá kích thước tối đa "
                + $"{_options.MaxFileSizeBytes / (1024 * 1024)} MB.");
        }

        if (ResolveFileType(command.ContentType, command.OriginalFileName)
            == DocumentFileType.Unknown)
        {
            throw new BusinessValidationException(
                "Định dạng tệp chưa được hỗ trợ. Hãy dùng PDF, DOCX, PPTX, TXT hoặc Markdown.");
        }
    }

    private static DocumentFileType ResolveFileType(string contentType, string fileName)
    {
        if (string.Equals(contentType, "application/pdf", StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return DocumentFileType.Pdf;
        }

        if (string.Equals(
                contentType,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
        {
            return DocumentFileType.Docx;
        }

        if (string.Equals(
                contentType,
                "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(".pptx", StringComparison.OrdinalIgnoreCase))
        {
            return DocumentFileType.Pptx;
        }

        if (fileName.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(".markdown", StringComparison.OrdinalIgnoreCase))
        {
            return DocumentFileType.Md;
        }

        if (string.Equals(contentType, "text/plain", StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
        {
            return DocumentFileType.Txt;
        }

        return DocumentFileType.Unknown;
    }

    private ITextExtractor? ResolveExtractor(string contentType, string fileName)
    {
        foreach (var extractor in _extractors)
        {
            if (extractor.Supports(contentType, fileName))
            {
                return extractor;
            }
        }

        return null;
    }

    private async Task TrySaveChangesSafelyAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _documentRepository.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to persist document status update after a processing error.");
        }
    }

    private static DocumentSummaryDto MapSummary(Document document)
    {
        return new DocumentSummaryDto(
            document.Id,
            document.CourseId,
            document.Course.Title,
            document.OriginalFileName,
            ResolveFileTypeFromName(document.OriginalFileName),
            document.SizeBytes,
            ToBll(document.Status),
            document.FailureReason,
            document.Chunks.Count,
            document.CreatedAtUtc);
    }

    private static DocumentDetailsDto MapDetails(Document document)
    {
        return new DocumentDetailsDto(
            document.Id,
            document.CourseId,
            document.Course.Title,
            document.OriginalFileName,
            ResolveFileTypeFromName(document.OriginalFileName),
            document.SizeBytes,
            ToBll(document.Status),
            document.FailureReason,
            document.Chunks.Count,
            document.CreatedAtUtc,
            document.UpdatedAtUtc);
    }

    private static BllDocumentStatus ToBll(DalDocumentStatus status)
    {
        return status switch
        {
            DalDocumentStatus.Pending => BllDocumentStatus.Pending,
            DalDocumentStatus.Processing => BllDocumentStatus.Processing,
            DalDocumentStatus.Ready => BllDocumentStatus.Ready,
            DalDocumentStatus.Failed => BllDocumentStatus.Failed,
            _ => throw new InvalidOperationException(
                "Unsupported persisted document status.")
        };
    }

    private static DocumentFileType ResolveFileTypeFromName(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            return DocumentFileType.Unknown;
        }

        return extension.ToLowerInvariant() switch
        {
            ".pdf" => DocumentFileType.Pdf,
            ".docx" => DocumentFileType.Docx,
            ".pptx" => DocumentFileType.Pptx,
            ".txt" => DocumentFileType.Txt,
            ".md" or ".markdown" => DocumentFileType.Md,
            _ => DocumentFileType.Unknown
        };
    }
}
