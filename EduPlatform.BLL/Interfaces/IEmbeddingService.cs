namespace EduPlatform.BLL.Interfaces;

/// <summary>
/// Generates an embedding vector for a single text chunk. Implementations talk
/// to the configured LLM provider (Gemini in production).
/// </summary>
public interface IEmbeddingService
{
    Task<float[]> EmbedAsync(
        string text,
        CancellationToken cancellationToken);

    Task<float[]> EmbedQueryAsync(
        string text,
        CancellationToken cancellationToken);

    int Dimensions { get; }
}
