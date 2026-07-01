namespace EduPlatform.DAL.Entities;

public sealed class RetrievalLog : BaseEntity
{
    public Guid MessageId { get; set; }

    public Message Message { get; set; } = null!;

    public Guid DocumentChunkId { get; set; }

    public DocumentChunk DocumentChunk { get; set; } = null!;

    public double SimilarityScore { get; set; }

    public int Rank { get; set; }
}
