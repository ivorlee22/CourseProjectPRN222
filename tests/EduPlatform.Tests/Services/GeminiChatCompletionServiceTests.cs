using System.Net;
using System.Text;
using System.Text.Json;
using EduPlatform.BLL.Exceptions;
using EduPlatform.BLL.Options;
using EduPlatform.BLL.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EduPlatform.Tests.Services;

[TestClass]
public sealed class GeminiChatCompletionServiceTests
{
    [TestMethod]
    public async Task GenerateAsync_SendsConfiguredTokenAndThinkingLimits()
    {
        var handler = new StubHandler(
            """{"candidates":[{"content":{"parts":[{"text":"Answer"}]},"finishReason":"STOP"}]}""");
        using var client = new HttpClient(handler);
        var service = CreateService(client);

        var answer = await service.GenerateAsync("Prompt", CancellationToken.None);

        Assert.AreEqual("Answer", answer);
        using var payload = JsonDocument.Parse(handler.RequestBody);
        var config = payload.RootElement.GetProperty("generationConfig");
        Assert.AreEqual(2048, config.GetProperty("maxOutputTokens").GetInt32());
        Assert.AreEqual(
            256,
            config.GetProperty("thinkingConfig").GetProperty("thinkingBudget").GetInt32());
    }

    [TestMethod]
    public async Task GenerateAsync_MaxTokens_RejectsTruncatedAnswer()
    {
        var handler = new StubHandler(
            """{"candidates":[{"content":{"parts":[{"text":"Partial"}]},"finishReason":"MAX_TOKENS"}]}""");
        using var client = new HttpClient(handler);
        var service = CreateService(client);

        await Assert.ThrowsExactlyAsync<BusinessValidationException>(
            () => service.GenerateAsync("Prompt", CancellationToken.None));
    }

    [TestMethod]
    public async Task StreamAsync_MaxTokens_RejectsTruncatedAnswerAfterDelta()
    {
        var handler = new StubHandler(
            "data: {\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"Partial\"}]},"
            + "\"finishReason\":\"MAX_TOKENS\"}]}\n\n",
            "text/event-stream");
        using var client = new HttpClient(handler);
        var service = CreateService(client);
        var deltas = new List<string>();

        await Assert.ThrowsExactlyAsync<BusinessValidationException>(async () =>
        {
            await foreach (var delta in service.StreamAsync("Prompt", CancellationToken.None))
            {
                deltas.Add(delta);
            }
        });

        Assert.AreEqual("Partial", string.Concat(deltas));
    }

    private static GeminiChatCompletionService CreateService(HttpClient client)
    {
        return new GeminiChatCompletionService(
            client,
            Options.Create(new GeminiOptions { ApiKey = "test-key" }),
            Options.Create(new ChatOptions()),
            NullLogger<GeminiChatCompletionService>.Instance);
    }

    private sealed class StubHandler(
        string responseBody,
        string mediaType = "application/json") : HttpMessageHandler
    {
        public string RequestBody { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, mediaType)
            };
        }
    }
}
