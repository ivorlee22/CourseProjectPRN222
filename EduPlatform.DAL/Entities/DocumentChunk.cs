using Pgvector;

namespace EduPlatform.DAL.Entities;

public sealed class DocumentChunk : BaseEntity
{
    public Guid DocumentId { get; set; }

    public Document Document { get; set; } = null!;

    public int Sequence { get; set; }

    public string Content { get; set; } = string.Empty;

    public int? PageNumber { get; set; }

    public string? Section { get; set; }

    public string MetadataJson { get; set; } = "{}";

    public Vector? Embedding { get; set; }

    public ICollection<RetrievalLog> RetrievalLogs { get; set; } = [];
}
