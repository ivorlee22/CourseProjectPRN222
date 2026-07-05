using System.Net.Http.Json;
using System.Text.Json.Serialization;
using EduPlatform.BLL.Exceptions;
using EduPlatform.BLL.Interfaces;
using EduPlatform.BLL.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EduPlatform.BLL.Services;

public sealed class GeminiChatCompletionService(
    HttpClient httpClient,
    IOptions<GeminiOptions> options,
    ILogger<GeminiChatCompletionService> logger) : IChatCompletionService
{
    private readonly GeminiOptions _options = options.Value;

    public async Task<string> GenerateAsync(
        string prompt,
        CancellationToken cancellationToken)
    {
        ValidateConfiguration();

        var url =
            $"{_options.ApiBaseUrl.TrimEnd('/')}/v1beta/models/"
            + $"{_options.ChatModel}:generateContent";

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.TryAddWithoutValidation("x-goog-api-key", _options.ApiKey);
        request.Content = JsonContent.Create(new GenerateContentRequest(
            [new GeminiContent("user", [new GeminiPart(prompt)])]));

        using var response = await httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseContentRead,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "Gemini chat completion failed with status {Status}",
                response.StatusCode);
            throw new BusinessValidationException(
                $"Gemini chat completion failed with status {(int)response.StatusCode}.");
        }

        var payload = await response.Content.ReadFromJsonAsync<GenerateContentResponse>(
            cancellationToken: cancellationToken);
        var answer = payload?.Candidates?
            .SelectMany(candidate => candidate.Content?.Parts ?? [])
            .Select(part => part.Text)
            .FirstOrDefault(text => !string.IsNullOrWhiteSpace(text));

        if (string.IsNullOrWhiteSpace(answer))
        {
            throw new BusinessValidationException(
                "Gemini did not return a usable answer.");
        }

        return answer.Trim();
    }

    private void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("Gemini:ApiKey is required for chat completion.");
        }

        if (string.IsNullOrWhiteSpace(_options.ApiBaseUrl)
            || string.IsNullOrWhiteSpace(_options.ChatModel))
        {
            throw new InvalidOperationException(
                "Gemini:ApiBaseUrl and Gemini:ChatModel are required for chat completion.");
        }
    }

    private sealed record GenerateContentRequest(
        [property: JsonPropertyName("contents")] IReadOnlyList<GeminiContent> Contents);

    private sealed record GeminiContent(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("parts")] IReadOnlyList<GeminiPart> Parts);

    private sealed record GeminiPart(
        [property: JsonPropertyName("text")] string Text);

    private sealed record GenerateContentResponse(
        [property: JsonPropertyName("candidates")] IReadOnlyList<GeminiCandidate>? Candidates);

    private sealed record GeminiCandidate(
        [property: JsonPropertyName("content")] GeminiResponseContent? Content);

    private sealed record GeminiResponseContent(
        [property: JsonPropertyName("parts")] IReadOnlyList<GeminiPart>? Parts);
}
