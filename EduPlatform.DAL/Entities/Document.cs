namespace EduPlatform.DAL.Entities;

public sealed class Document : BaseEntity
{
    public Guid CourseId { get; set; }

    public Course Course { get; set; } = null!;

    public Guid UploadedById { get; set; }

    public string OriginalFileName { get; set; } = string.Empty;

    public string StorageKey { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public DocumentStatus Status { get; set; } = DocumentStatus.Pending;

    public string? FailureReason { get; set; }

    public ICollection<DocumentChunk> Chunks { get; set; } = [];
}
