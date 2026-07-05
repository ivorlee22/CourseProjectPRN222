using EduPlatform.BLL.DTOs.Chats;
using EduPlatform.BLL.Enums;
using EduPlatform.BLL.Exceptions;
using EduPlatform.BLL.Interfaces;
using EduPlatform.BLL.Models;
using EduPlatform.BLL.Options;
using EduPlatform.BLL.Services;
using EduPlatform.DAL.Entities;
using EduPlatform.DAL.Repositories;
using Microsoft.Extensions.Options;
using Pgvector;
using BllUserRole = EduPlatform.BLL.Enums.UserRole;

namespace EduPlatform.Tests.Services;

[TestClass]
public sealed class ChatServiceTests
{
    private static readonly Guid UserId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid CourseId = Guid.Parse("20000000-0000-0000-0000-000000000001");
    private static readonly Guid SessionId = Guid.Parse("30000000-0000-0000-0000-000000000001");
    private static readonly DateTimeOffset Now = new(2026, 7, 5, 16, 0, 0, TimeSpan.Zero);
    private static readonly float[] Embedding = [1f, 0f, 0f];

    private readonly FakeChatRepository _repository = new();
    private readonly FakeEmbeddingService _embeddingService = new();
    private readonly FakeChatCompletionService _completionService = new();
    private readonly ChatService _service;
    private readonly ActorContext _actor = new(UserId, BllUserRole.Student);

    public ChatServiceTests()
    {
        _repository.Sessions.Add(new ChatSession
        {
            Id = SessionId,
            UserId = UserId,
            CourseId = CourseId,
            Title = "Cuộc trò chuyện mới"
        });
        _service = new ChatService(
            _repository,
            _embeddingService,
            _completionService,
            Options.Create(new ChatOptions()),
            new FixedTimeProvider(Now));
    }

    [TestMethod]
    public async Task SendMessageAsync_WithRetrievedChunks_PersistsAnswerAndCitations()
    {
        _repository.SearchResults.Add(CreateSearchResult());
        _completionService.Answer = "Đây là câu trả lời [1].";

        var result = await _service.SendMessageAsync(
            SessionId,
            new SendChatMessageCommand(" Dependency injection là gì? "),
            _actor,
            CancellationToken.None);

        Assert.AreEqual("Dependency injection là gì?", result.UserMessage.Content);
        Assert.AreEqual("Đây là câu trả lời [1].", result.AssistantMessage.Content);
        var citation = Assert.ContainsSingle(result.AssistantMessage.Citations);
        Assert.AreEqual("lesson.pdf", citation.DocumentName);
        Assert.AreEqual(1, citation.Rank);
        Assert.HasCount(2, _repository.AddedMessages);
        Assert.HasCount(1, _repository.AddedRetrievalLogs);
        Assert.AreEqual(1, _repository.SaveChangesCallCount);
        Assert.AreEqual(1, _completionService.CallCount);
        Assert.AreEqual("Dependency injection là gì?", _repository.Sessions[0].Title);
    }

    [TestMethod]
    public async Task SendMessageAsync_WithoutContext_UsesFallbackWithoutCallingGemini()
    {
        var result = await _service.SendMessageAsync(
            SessionId,
            new SendChatMessageCommand("Nội dung không có trong bài?"),
            _actor,
            CancellationToken.None);

        Assert.Contains("Không tìm thấy nội dung phù hợp", result.AssistantMessage.Content);
        Assert.AreEqual(0, _completionService.CallCount);
        Assert.IsEmpty(result.AssistantMessage.Citations);
        Assert.AreEqual(1, _repository.SaveChangesCallCount);
    }

    [TestMethod]
    public async Task SendMessageAsync_ForeignSession_ThrowsForbiddenBeforeEmbedding()
    {
        _repository.Sessions[0].UserId = Guid.NewGuid();

        await Assert.ThrowsExactlyAsync<ForbiddenOperationException>(
            () => _service.SendMessageAsync(
                SessionId,
                new SendChatMessageCommand("Câu hỏi hợp lệ"),
                _actor,
                CancellationToken.None));

        Assert.AreEqual(0, _embeddingService.QueryCallCount);
        Assert.AreEqual(0, _repository.SaveChangesCallCount);
    }

    [TestMethod]
    public async Task SendMessageAsync_NoCourseAccess_ThrowsForbidden()
    {
        _repository.CanAccessCourse = false;

        await Assert.ThrowsExactlyAsync<ForbiddenOperationException>(
            () => _service.SendMessageAsync(
                SessionId,
                new SendChatMessageCommand("Câu hỏi hợp lệ"),
                _actor,
                CancellationToken.None));

        Assert.AreEqual(0, _embeddingService.QueryCallCount);
        Assert.AreEqual(0, _repository.SaveChangesCallCount);
    }

    [TestMethod]
    public async Task SendMessageAsync_GeminiFailure_DoesNotPersistMessages()
    {
        _repository.SearchResults.Add(CreateSearchResult());
        _completionService.ExceptionToThrow = new BusinessValidationException("Gemini unavailable");

        await Assert.ThrowsExactlyAsync<BusinessValidationException>(
            () => _service.SendMessageAsync(
                SessionId,
                new SendChatMessageCommand("Câu hỏi hợp lệ"),
                _actor,
                CancellationToken.None));

        Assert.IsEmpty(_repository.AddedMessages);
        Assert.IsEmpty(_repository.AddedRetrievalLogs);
        Assert.AreEqual(0, _repository.SaveChangesCallCount);
    }

    [TestMethod]
    public async Task CreateSessionAsync_WithoutCourseAccess_ThrowsForbidden()
    {
        _repository.CanAccessCourse = false;

        await Assert.ThrowsExactlyAsync<ForbiddenOperationException>(
            () => _service.CreateSessionAsync(
                new CreateChatSessionCommand(CourseId, null),
                _actor,
                CancellationToken.None));

        Assert.AreEqual(0, _repository.SaveChangesCallCount);
    }

    [TestMethod]
    public async Task SendMessageAsync_AnswerCitesSecondChunk_PersistsOnlyThatCitation()
    {
        var first = CreateSearchResult();
        var second = new RetrievedDocumentChunk(
            new DocumentChunk
            {
                Id = Guid.Parse("40000000-0000-0000-0000-000000000002"),
                DocumentId = first.Chunk.DocumentId,
                Sequence = 3,
                Content = "Service lifetime có ba loại chính."
            },
            "lesson.pdf",
            0.84);
        _repository.SearchResults.Add(first);
        _repository.SearchResults.Add(second);
        _completionService.Answer = "Có ba service lifetime chính [2].";

        var result = await _service.SendMessageAsync(
            SessionId,
            new SendChatMessageCommand("Có những service lifetime nào?"),
            _actor,
            CancellationToken.None);

        var citation = Assert.ContainsSingle(result.AssistantMessage.Citations);
        Assert.AreEqual(second.Chunk.Id, citation.DocumentChunkId);
        Assert.AreEqual(2, citation.Rank);
        var log = Assert.ContainsSingle(_repository.AddedRetrievalLogs);
        Assert.AreEqual(second.Chunk.Id, log.DocumentChunkId);
    }

    [TestMethod]
    public async Task DeleteSessionAsync_Owner_RemovesSession()
    {
        await _service.DeleteSessionAsync(SessionId, _actor, CancellationToken.None);

        Assert.IsEmpty(_repository.Sessions);
        Assert.AreEqual(1, _repository.SaveChangesCallCount);
    }

    private static RetrievedDocumentChunk CreateSearchResult()
    {
        return new RetrievedDocumentChunk(
            new DocumentChunk
            {
                Id = Guid.Parse("40000000-0000-0000-0000-000000000001"),
                DocumentId = Guid.Parse("50000000-0000-0000-0000-000000000001"),
                Sequence = 2,
                PageNumber = 3,
                Content = "Dependency injection giúp đảo ngược quyền kiểm soát phụ thuộc."
            },
            "lesson.pdf",
            0.91);
    }

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;
    }

    private sealed class FakeEmbeddingService : IEmbeddingService
    {
        public int Dimensions => 3;

        public int QueryCallCount { get; private set; }

        public Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken)
        {
            return Task.FromResult(Embedding);
        }

        public Task<float[]> EmbedQueryAsync(string text, CancellationToken cancellationToken)
        {
            QueryCallCount++;
            return Task.FromResult(Embedding);
        }
    }

    private sealed class FakeChatCompletionService : IChatCompletionService
    {
        public string Answer { get; set; } = "Answer";

        public Exception? ExceptionToThrow { get; set; }

        public int CallCount { get; private set; }

        public Task<string> GenerateAsync(string prompt, CancellationToken cancellationToken)
        {
            CallCount++;
            return ExceptionToThrow is null
                ? Task.FromResult(Answer)
                : Task.FromException<string>(ExceptionToThrow);
        }
    }

    private sealed class FakeChatRepository : IChatRepository
    {
        public bool CanAccessCourse { get; set; } = true;

        public List<ChatSession> Sessions { get; } = [];

        public List<Message> AddedMessages { get; } = [];

        public List<RetrievalLog> AddedRetrievalLogs { get; } = [];

        public List<RetrievedDocumentChunk> SearchResults { get; } = [];

        public int SaveChangesCallCount { get; private set; }

        public Task<bool> CanAccessCourseAsync(
            Guid courseId,
            Guid userId,
            bool isAdmin,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(CanAccessCourse);
        }

        public Task<ChatSession?> GetSessionAsync(
            Guid sessionId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Sessions.SingleOrDefault(session => session.Id == sessionId));
        }

        public Task<IReadOnlyList<ChatSession>> ListSessionsAsync(
            Guid userId,
            Guid? courseId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<ChatSession>>(Sessions
                .Where(session => session.UserId == userId)
                .Where(session => !courseId.HasValue || session.CourseId == courseId)
                .ToArray());
        }

        public Task<IReadOnlyList<Message>> ListMessagesAsync(
            Guid sessionId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<Message>>(AddedMessages
                .Where(message => message.ChatSessionId == sessionId)
                .ToArray());
        }

        public Task<IReadOnlyList<RetrievedDocumentChunk>> SearchChunksAsync(
            Guid courseId,
            Vector queryEmbedding,
            int limit,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<RetrievedDocumentChunk>>(
                SearchResults.Take(limit).ToArray());
        }

        public Task AddSessionAsync(ChatSession session, CancellationToken cancellationToken)
        {
            Sessions.Add(session);
            return Task.CompletedTask;
        }

        public Task AddMessagesAsync(
            IEnumerable<Message> messages,
            CancellationToken cancellationToken)
        {
            AddedMessages.AddRange(messages);
            return Task.CompletedTask;
        }

        public Task AddRetrievalLogsAsync(
            IEnumerable<RetrievalLog> retrievalLogs,
            CancellationToken cancellationToken)
        {
            AddedRetrievalLogs.AddRange(retrievalLogs);
            return Task.CompletedTask;
        }

        public void RemoveSession(ChatSession session)
        {
            Sessions.Remove(session);
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCallCount++;
            return Task.FromResult(1);
        }
    }
}
