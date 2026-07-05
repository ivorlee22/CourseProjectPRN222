using EduPlatform.BLL.DTOs.Chats;
using EduPlatform.BLL.Interfaces;
using EduPlatform.BLL.Models;
using EduPlatform.BLL.Options;
using EduPlatform.BLL.Services;
using EduPlatform.DAL.Entities;
using EduPlatform.DAL.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Pgvector;
using BllUserRole = EduPlatform.BLL.Enums.UserRole;

namespace EduPlatform.Tests.Integration;

[TestClass]
public sealed class LiveGeminiSmokeTests
{
    private readonly TestContext _testContext;

    public LiveGeminiSmokeTests(TestContext testContext)
    {
        _testContext = testContext;
    }

    [TestMethod]
    [TestCategory("LiveGemini")]
    public async Task ChatService_WithLiveGemini_ReturnsAnswerAndCitation()
    {
        var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Assert.Inconclusive("Set GEMINI_API_KEY to run the live Gemini smoke test.");
        }

        var geminiOptions = Options.Create(new GeminiOptions { ApiKey = apiKey });
        using var embeddingHttpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(60)
        };
        using var completionHttpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(90)
        };
        var embeddingService = new GeminiEmbeddingService(
            embeddingHttpClient,
            Options.Create(new DocumentOptions()),
            geminiOptions,
            NullLogger<GeminiEmbeddingService>.Instance);
        IChatCompletionService completionService = new GeminiChatCompletionService(
            completionHttpClient,
            geminiOptions,
            NullLogger<GeminiChatCompletionService>.Instance);
        var repository = new LiveSmokeChatRepository();
        var service = new ChatService(
            repository,
            embeddingService,
            completionService,
            Options.Create(new ChatOptions()),
            TimeProvider.System);

        var result = await service.SendMessageAsync(
            repository.Session.Id,
            new SendChatMessageCommand("Dependency injection là gì?"),
            new ActorContext(repository.Session.UserId, BllUserRole.Student),
            CancellationToken.None);

        _testContext.WriteLine($"Embedding dimensions: {embeddingService.Dimensions}");
        _testContext.WriteLine($"Gemini answer: {result.AssistantMessage.Content}");
        _testContext.WriteLine(
            $"Citation: {result.AssistantMessage.Citations[0].DocumentName} "
            + $"(score {result.AssistantMessage.Citations[0].SimilarityScore:F2})");

        Assert.IsFalse(string.IsNullOrWhiteSpace(result.AssistantMessage.Content));
        Assert.HasCount(1, result.AssistantMessage.Citations);
        Assert.HasCount(2, repository.Messages);
        Assert.HasCount(1, repository.RetrievalLogs);
        Assert.AreEqual(1, repository.SaveChangesCallCount);
    }

    private sealed class LiveSmokeChatRepository : IChatRepository
    {
        public ChatSession Session { get; } = new()
        {
            Id = Guid.Parse("30000000-0000-0000-0000-000000000099"),
            UserId = Guid.Parse("10000000-0000-0000-0000-000000000099"),
            CourseId = Guid.Parse("20000000-0000-0000-0000-000000000099"),
            Title = "Live Gemini smoke test"
        };

        public List<Message> Messages { get; } = [];

        public List<RetrievalLog> RetrievalLogs { get; } = [];

        public int SaveChangesCallCount { get; private set; }

        public Task<bool> CanAccessCourseAsync(
            Guid courseId,
            Guid userId,
            bool isAdmin,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(courseId == Session.CourseId && userId == Session.UserId);
        }

        public Task<ChatSession?> GetSessionAsync(
            Guid sessionId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<ChatSession?>(sessionId == Session.Id ? Session : null);
        }

        public Task<IReadOnlyList<ChatSession>> ListSessionsAsync(
            Guid userId,
            Guid? courseId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<ChatSession>>([Session]);
        }

        public Task<IReadOnlyList<Message>> ListMessagesAsync(
            Guid sessionId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<Message>>(Messages);
        }

        public Task<IReadOnlyList<RetrievedDocumentChunk>> SearchChunksAsync(
            Guid courseId,
            Vector queryEmbedding,
            int limit,
            CancellationToken cancellationToken)
        {
            DocumentChunk chunk = new()
            {
                Id = Guid.Parse("40000000-0000-0000-0000-000000000099"),
                DocumentId = Guid.Parse("50000000-0000-0000-0000-000000000099"),
                Sequence = 1,
                PageNumber = 1,
                Content = "Dependency injection cung cấp các dependency từ bên ngoài thay vì để lớp tự khởi tạo chúng."
            };
            return Task.FromResult<IReadOnlyList<RetrievedDocumentChunk>>(
                [new RetrievedDocumentChunk(chunk, "dependency-injection.txt", 0.95)]);
        }

        public Task AddSessionAsync(ChatSession session, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task AddMessagesAsync(
            IEnumerable<Message> messages,
            CancellationToken cancellationToken)
        {
            Messages.AddRange(messages);
            return Task.CompletedTask;
        }

        public Task AddRetrievalLogsAsync(
            IEnumerable<RetrievalLog> retrievalLogs,
            CancellationToken cancellationToken)
        {
            RetrievalLogs.AddRange(retrievalLogs);
            return Task.CompletedTask;
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCallCount++;
            return Task.FromResult(1);
        }
    }
}
