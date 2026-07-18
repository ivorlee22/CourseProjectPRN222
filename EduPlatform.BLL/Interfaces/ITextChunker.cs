namespace EduPlatform.BLL.Interfaces;

/// <summary>
/// Splits the text produced by an <see cref="ITextExtractor"/> into smaller
/// chunks suitable for embedding and retrieval.
/// </summary>
public interface ITextChunker
{
    IReadOnlyList<ChunkResult> Chunk(
        IReadOnlyList<ExtractedPage> pages,
        int chunkSize,
        int chunkOverlap);
}

public sealed record ChunkResult(
    string Content,
    int? PageNumber,
    string? Section);