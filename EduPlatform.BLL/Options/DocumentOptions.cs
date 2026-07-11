namespace EduPlatform.BLL.Options;

/// <summary>
/// Configuration for the Document upload pipeline: storage location, supported
/// file extensions, maximum size, chunking parameters, and the Gemini API
/// endpoint/key used by the embedding pipeline.
/// </summary>
public sealed class DocumentOptions
{
    public const string SectionName = "Documents";

    /// <summary>Absolute directory used to store uploaded document binaries.</summary>
    public string StorageRoot { get; init; } = string.Empty;

    /// <summary>Maximum accepted upload size in bytes.</summary>
    public long MaxFileSizeBytes { get; init; } = 25L * 1024L * 1024L;

    /// <summary>Maximum characters per chunk produced by the chunking pipeline.</summary>
    public int ChunkSize { get; init; } = 1500;

    /// <summary>Overlap in characters between consecutive chunks.</summary>
    public int ChunkOverlap { get; init; }

    /// <summary>Embedding dimension produced by the configured Gemini model.</summary>
    public int EmbeddingDimensions { get; init; } = 3072;

    /// <summary>Gemini embeddings model used to vectorize chunks.</summary>
    public string GeminiEmbeddingModel { get; init; } = "gemini-embedding-001";

    /// <summary>Gemini API key loaded from configuration or environment.</summary>
    public string GeminiApiKey { get; init; } = string.Empty;

    /// <summary>Gemini API base URL.</summary>
    public string GeminiApiBaseUrl { get; init; } = "https://generativelanguage.googleapis.com";
}