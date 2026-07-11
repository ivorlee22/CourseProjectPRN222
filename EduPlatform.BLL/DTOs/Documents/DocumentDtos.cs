using EduPlatform.BLL.Enums;

namespace EduPlatform.BLL.DTOs.Documents;

public sealed record DocumentSummaryDto(
    Guid Id,
    Guid CourseId,
    string CourseTitle,
    string OriginalFileName,
    DocumentFileType FileType,
    long SizeBytes,
    DocumentStatus Status,
    string? FailureReason,
    int ChunkCount,
    DateTimeOffset CreatedAtUtc);

public sealed record DocumentDetailsDto(
    Guid Id,
    Guid CourseId,
    string CourseTitle,
    string OriginalFileName,
    DocumentFileType FileType,
    long SizeBytes,
    DocumentStatus Status,
    string? FailureReason,
    int ChunkCount,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record DocumentChunkDto(
    Guid Id,
    int Sequence,
    string Content,
    int? PageNumber,
    string? Section,
    bool HasEmbedding,
    float[]? EmbeddingData);

public sealed record UploadDocumentCommand(
    Guid CourseId,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    Stream Content);