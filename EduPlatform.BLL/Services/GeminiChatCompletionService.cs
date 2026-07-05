using System.Runtime.CompilerServices;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
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
    IOptions<ChatOptions> chatOptions,
    ILogger<GeminiChatCompletionService> logger) : IChatCompletionService
{
    private readonly GeminiOptions _options = options.Value;
    private readonly int _maxOutputTokens = Math.Clamp(chatOptions.Value.MaxOutputTokens, 256, 8192);
    private readonly int _thinkingBudget = Math.Clamp(chatOptions.Value.ThinkingBudget, 0, 1024);

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
        request.Content = JsonContent.Create(CreatePayload(prompt));

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
        ThrowIfTruncated(payload);
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

    public async IAsyncEnumerable<string> StreamAsync(
        string prompt,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ValidateConfiguration();

        var url =
            $"{_options.ApiBaseUrl.TrimEnd('/')}/v1beta/models/"
            + $"{_options.ChatModel}:streamGenerateContent?alt=sse";
        using var request = CreateRequest(url, prompt);
        using var response = await httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "Gemini chat stream failed with status {Status}",
                response.StatusCode);
            throw new BusinessValidationException(
                $"Gemini chat stream failed with status {(int)response.StatusCode}.");
        }

        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(contentStream, Encoding.UTF8);
        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (!line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var json = line[5..].Trim();
            if (json.Length == 0 || json == "[DONE]")
            {
                continue;
            }

            var payload = JsonSerializer.Deserialize<GenerateContentResponse>(json);
            var delta = ExtractText(payload);
            if (!string.IsNullOrEmpty(delta))
            {
                yield return delta;
            }
            ThrowIfTruncated(payload);
        }
    }

    private HttpRequestMessage CreateRequest(string url, string prompt)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.TryAddWithoutValidation("x-goog-api-key", _options.ApiKey);
        request.Content = JsonContent.Create(CreatePayload(prompt));
        return request;
    }

    private static string? ExtractText(GenerateContentResponse? payload)
    {
        return payload?.Candidates?
            .SelectMany(candidate => candidate.Content?.Parts ?? [])
            .Select(part => part.Text)
            .FirstOrDefault(text => !string.IsNullOrEmpty(text));
    }

    private GenerateContentRequest CreatePayload(string prompt)
    {
        return new GenerateContentRequest(
            [new GeminiContent("user", [new GeminiPart(prompt)])],
            new GeminiGenerationConfig(
                _maxOutputTokens,
                new GeminiThinkingConfig(_thinkingBudget)));
    }

    private static void ThrowIfTruncated(GenerateContentResponse? payload)
    {
        if (payload?.Candidates?.Any(candidate =>
                string.Equals(candidate.FinishReason, "MAX_TOKENS", StringComparison.OrdinalIgnoreCase)) == true)
        {
            throw new BusinessValidationException(
                "Câu trả lời vượt quá giới hạn độ dài. Vui lòng chia câu hỏi thành nhiều phần.");
        }
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
        [property: JsonPropertyName("contents")] IReadOnlyList<GeminiContent> Contents,
        [property: JsonPropertyName("generationConfig")] GeminiGenerationConfig GenerationConfig);

    private sealed record GeminiGenerationConfig(
        [property: JsonPropertyName("maxOutputTokens")] int MaxOutputTokens,
        [property: JsonPropertyName("thinkingConfig")] GeminiThinkingConfig ThinkingConfig);

    private sealed record GeminiThinkingConfig(
        [property: JsonPropertyName("thinkingBudget")] int ThinkingBudget);

    private sealed record GeminiContent(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("parts")] IReadOnlyList<GeminiPart> Parts);

    private sealed record GeminiPart(
        [property: JsonPropertyName("text")] string Text);

    private sealed record GenerateContentResponse(
        [property: JsonPropertyName("candidates")] IReadOnlyList<GeminiCandidate>? Candidates);

    private sealed record GeminiCandidate(
        [property: JsonPropertyName("content")] GeminiResponseContent? Content,
        [property: JsonPropertyName("finishReason")] string? FinishReason);

    private sealed record GeminiResponseContent(
        [property: JsonPropertyName("parts")] IReadOnlyList<GeminiPart>? Parts);
}
