using EduPlatform.BLL.Enums;
using EduPlatform.BLL.Exceptions;
using EduPlatform.BLL.Interfaces;
using EduPlatform.BLL.Models;
using EduPlatform.BLL.Options;
using EduPlatform.BLL.Services;
using EduPlatform.DAL.Entities;
using EduPlatform.DAL.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using DalCourseType = EduPlatform.DAL.Entities.CourseType;
using DalEnrollmentStatus = EduPlatform.DAL.Entities.EnrollmentStatus;
using DalUserRole = EduPlatform.DAL.Entities.UserRole;
using BllUserRole = EduPlatform.BLL.Enums.UserRole;

namespace EduPlatform.Tests.Services;

[TestClass]
public sealed class DocumentAccessTests
{
    private static readonly Guid CourseId = Guid.Parse("40000000-0000-0000-0000-000000000001");
    private static readonly Guid StudentId = Guid.Parse("40000000-0000-0000-0000-000000000002");
    private static readonly Guid OwnerId = Guid.Parse("40000000-0000-0000-0000-000000000003");
    private static readonly float[] FakeEmbedding = [1f, 0f, 0f];

    private readonly FakeDocumentRepository _documentRepository = new();
    private readonly FakeCourseRepository _courseRepository = new();
    private readonly DocumentService _service;

    public DocumentAccessTests()
    {
        _service = new DocumentService(
            _documentRepository,
            _courseRepository,
            new FakeTextChunker(),
            new FakeEmbeddingService(),
            Array.Empty<ITextExtractor>(),
            new FakeFileStorageService(),
            Options.Create(new DocumentOptions()),
            TimeProvider.System,
            NullLogger<DocumentService>.Instance);
    }

    [TestMethod]
    public async Task ListByCourseAsync_PublicVisibleCourse_AllowsStudentWithoutEnrollment()
    {
        _courseRepository.Courses.Add(CreateCourse(DalCourseType.Public, isVisible: true));
        var document = CreateDocument();
        document.Chunks.Add(new DocumentChunk { DocumentId = document.Id, Sequence = 0, Content = "First" });
        document.Chunks.Add(new DocumentChunk { DocumentId = document.Id, Sequence = 1, Content = "Second" });
        _documentRepository.Documents.Add(document);

        var documents = await _service.ListByCourseAsync(
            CourseId,
            new ActorContext(StudentId, BllUserRole.Student),
            CancellationToken.None);

        Assert.HasCount(1, documents);
        Assert.AreEqual(2, documents[0].ChunkCount);
    }

    [TestMethod]
    public async Task ListByCourseAsync_PrivateCourse_BlocksStudentWithoutEnrollment()
    {
        _courseRepository.Courses.Add(CreateCourse(DalCourseType.Private, isVisible: true));

        await Assert.ThrowsExactlyAsync<ForbiddenOperationException>(
            async () => await _service.ListByCourseAsync(
                CourseId,
                new ActorContext(StudentId, BllUserRole.Student),
                CancellationToken.None));
    }

    [TestMethod]
    public async Task ListByCourseAsync_HiddenPublicCourse_BlocksStudentWithoutEnrollment()
    {
        _courseRepository.Courses.Add(CreateCourse(DalCourseType.Public, isVisible: false));

        await Assert.ThrowsExactlyAsync<ForbiddenOperationException>(
            async () => await _service.ListByCourseAsync(
                CourseId,
                new ActorContext(StudentId, BllUserRole.Student),
                CancellationToken.None));
    }

    private static Course CreateCourse(DalCourseType type, bool isVisible)
    {
        return new Course
        {
            Id = CourseId,
            OwnerId = OwnerId,
            Owner = new User
            {
                Id = OwnerId,
                FullName = "Course Owner",
                Email = "owner@example.test",
                Role = DalUserRole.Teacher
            },
            Title = "Public course",
            Description = "Course description",
            Type = type,
            IsVisible = isVisible
        };
    }

    private static Document CreateDocument()
    {
        return new Document
        {
            Id = Guid.NewGuid(),
            CourseId = CourseId,
            Course = CreateCourse(DalCourseType.Public, isVisible: true),
            OriginalFileName = "lesson.pdf",
            ContentType = "application/pdf",
            StorageKey = "lesson.pdf",
            SizeBytes = 1024,
            Status = EduPlatform.DAL.Entities.DocumentStatus.Ready
        };
    }

    private sealed class FakeDocumentRepository : IDocumentRepository
    {
        public List<Document> Documents { get; } = [];

        public Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(Documents.SingleOrDefault(x => x.Id == id));
        }

        public Task<IReadOnlyList<DocumentListItem>> ListByCourseAsync(Guid courseId, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<DocumentListItem>>(Documents
                .Where(x => x.CourseId == courseId)
                .Select(x => new DocumentListItem(
                    x.Id,
                    x.CourseId,
                    x.Course.Title,
                    x.OriginalFileName,
                    x.SizeBytes,
                    x.Status,
                    x.FailureReason,
                    x.Chunks.Count,
                    x.CreatedAtUtc))
                .ToArray());
        }

        public Task<IReadOnlyList<DocumentChunk>> ListChunksAsync(Guid documentId, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<DocumentChunk>>(Array.Empty<DocumentChunk>());
        }

        public Task AddAsync(Document document, CancellationToken cancellationToken)
        {
            Documents.Add(document);
            return Task.CompletedTask;
        }

        public Task AddChunksAsync(IEnumerable<DocumentChunk> chunks, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public void Remove(Document document)
        {
            Documents.Remove(document);
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(1);
        }
    }

    private sealed class FakeCourseRepository : ICourseRepository
    {
        public List<Course> Courses { get; } = [];

        public Task<Course?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(Courses.SingleOrDefault(x => x.Id == id));
        }

        public Task<(IReadOnlyList<Course> Items, int TotalCount)> SearchAsync(
            string? keyword,
            int pageNumber,
            int pageSize,
            bool visibleOnly,
            Guid? ownerId,
            Guid? enrolledUserId,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<int> CountByOwnerAsync(Guid ownerId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<int> CountActiveEnrollmentsByUserAsync(Guid userId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<User>> FindActiveStudentsByEmailOrNameAsync(string lookup, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<PendingCourseInvitation>> GetPendingInvitationsAsync(
            Guid userId,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<int> CountPendingInvitationsAsync(
            Guid userId,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<CourseEnrollment?> GetEnrollmentAsync(Guid courseId, Guid userId, CancellationToken cancellationToken)
        {
            return Task.FromResult<CourseEnrollment?>(null);
        }

        public Task<IReadOnlyList<CourseEnrollment>> GetStudentsAsync(Guid courseId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task AddAsync(Course course, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task AddEnrollmentAsync(CourseEnrollment enrollment, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public void Remove(Course course)
        {
            throw new NotImplementedException();
        }

        public void RemoveEnrollment(CourseEnrollment enrollment)
        {
            throw new NotImplementedException();
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class FakeTextChunker : ITextChunker
    {
        public IReadOnlyList<ChunkResult> Chunk(IReadOnlyList<ExtractedPage> pages, int chunkSize, int chunkOverlap)
        {
            return Array.Empty<ChunkResult>();
        }
    }

    private sealed class FakeEmbeddingService : IEmbeddingService
    {
        public int Dimensions => 3;

        public Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken)
        {
            return Task.FromResult(FakeEmbedding);
        }

        public Task<float[]> EmbedQueryAsync(string text, CancellationToken cancellationToken)
        {
            return Task.FromResult(FakeEmbedding);
        }
    }

    private sealed class FakeFileStorageService : IFileStorageService
    {
        public Task<string> UploadAsync(Stream fileStream, string fileName, string contentType)
        {
            return Task.FromResult(fileName);
        }

        public Task<string> GetDownloadUrlAsync(string storedPath, string fileName, string contentType)
        {
            return Task.FromResult($"https://example.test/{fileName}");
        }

        public Task DeleteAsync(string storedPath)
        {
            return Task.CompletedTask;
        }
    }
}
