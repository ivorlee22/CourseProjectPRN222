using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using EduPlatform.BLL.Exceptions;
using EduPlatform.BLL.Interfaces;
using EduPlatform.BLL.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EduPlatform.BLL.Services;

/// <summary>
/// Calls the Gemini batchEmbedContents endpoint to vectorize a single text
/// chunk. The embedding dimensions come from configuration so callers stay
/// aligned with the pgvector column size.
/// </summary>
public sealed class GeminiEmbeddingService : IEmbeddingService
{
    private static readonly MediaTypeHeaderValue JsonMediaType =
        MediaTypeHeaderValue.Parse("application/json");

    private readonly HttpClient _httpClient;
    private readonly DocumentOptions _options;
    private readonly ILogger<GeminiEmbeddingService> _logger;

    public int Dimensions => _options.EmbeddingDimensions;

    public GeminiEmbeddingService(
        HttpClient httpClient,
        IOptions<DocumentOptions> options,
        ILogger<GeminiEmbeddingService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<float[]> EmbedAsync(
        string text,
        CancellationToken cancellationToken)
    {
        ValidateConfiguration();

        var url =
            $"{_options.GeminiApiBaseUrl.TrimEnd('/')}/v1beta/models/"
            + $"{_options.GeminiEmbeddingModel}:embedContent";

        var requestBody = new EmbeddingRequest(
            new EmbeddingContent(
                [new EmbeddingPart(text ?? string.Empty)]),
            EmbeddingTaskType.RetrievalDocument);

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.TryAddWithoutValidation("x-goog-api-key", _options.GeminiApiKey);
        request.Content = JsonContent.Create(requestBody);

        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseContentRead,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning(
                "Gemini embedding call failed with status {Status}: {Body}",
                response.StatusCode,
                body);
            throw new DocumentProcessingException(
                $"Gemini embedding call failed with status {(int)response.StatusCode}.");
        }

        var payload = await response.Content
            .ReadFromJsonAsync<EmbeddingResponse>(cancellationToken: cancellationToken)
            ?? throw new DocumentProcessingException(
                "Gemini embedding response is empty.");

        var values = payload.Embedding?.Values;
        if (values is null || values.Count == 0)
        {
            throw new DocumentProcessingException(
                "Gemini embedding response is missing the values array.");
        }

        var result = new float[values.Count];
        for (var i = 0; i < values.Count; i++)
        {
            result[i] = values[i];
        }

        return result;
    }

    private void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_options.GeminiApiKey))
        {
            throw new InvalidOperationException(
                "Documents:GeminiApiKey is required for embedding generation.");
        }

        if (string.IsNullOrWhiteSpace(_options.GeminiApiBaseUrl))
        {
            throw new InvalidOperationException(
                "Documents:GeminiApiBaseUrl is required for embedding generation.");
        }

        if (string.IsNullOrWhiteSpace(_options.GeminiEmbeddingModel))
        {
            throw new InvalidOperationException(
                "Documents:GeminiEmbeddingModel is required for embedding generation.");
        }
    }

    private sealed record EmbeddingRequest(
        [property: JsonPropertyName("content")] EmbeddingContent Content,
        [property: JsonPropertyName("taskType")] string TaskType);

    private sealed record EmbeddingContent(
        [property: JsonPropertyName("parts")] IReadOnlyList<EmbeddingPart> Parts);

    private sealed record EmbeddingPart(
        [property: JsonPropertyName("text")] string Text);

    private sealed record EmbeddingResponse(
        [property: JsonPropertyName("embedding")] EmbeddingValues? Embedding);

    private sealed record EmbeddingValues(
        [property: JsonPropertyName("values")] IReadOnlyList<float>? Values);

    private static class EmbeddingTaskType
    {
        public const string RetrievalDocument = "RETRIEVAL_DOCUMENT";
    }
}