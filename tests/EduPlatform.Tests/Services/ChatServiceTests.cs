using System.Runtime.CompilerServices;
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
    private readonly FakeChatQuotaService _chatQuotaService = new();
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
            _chatQuotaService,
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
        Assert.AreEqual(2, _chatQuotaService.CallCount);
        Assert.AreEqual("Dependency injection là gì?", _repository.Sessions[0].Title);
    }

    [TestMethod]
    public async Task SendMessageAsync_QuotaExceeded_DoesNotCallGeminiOrPersistMessages()
    {
        _repository.SearchResults.Add(CreateSearchResult());
        _chatQuotaService.ExceptionToThrow =
            new ChatQuotaExceededException("Bạn đã sử dụng hết 1 tin nhắn trong ngày. Hãy nâng cấp gói để tiếp tục.");

        var exception = await Assert.ThrowsExactlyAsync<ChatQuotaExceededException>(
            () => _service.SendMessageAsync(
                SessionId,
                new SendChatMessageCommand("Câu hỏi hợp lệ"),
                _actor,
                CancellationToken.None));

        Assert.Contains("nâng cấp", exception.Message);
        Assert.AreEqual(0, _embeddingService.QueryCallCount);
        Assert.AreEqual(0, _completionService.CallCount);
        Assert.IsEmpty(_repository.AddedMessages);
        Assert.IsEmpty(_repository.AddedRetrievalLogs);
        Assert.AreEqual(0, _repository.SaveChangesCallCount);
        Assert.AreEqual(1, _chatQuotaService.CallCount);
    }

    [TestMethod]
    public async Task SendMessageAsync_QuotaExceededDuringSave_DoesNotPersistMessages()
    {
        _repository.SearchResults.Add(CreateSearchResult());
        _completionService.Answer = "Câu trả lời [1].";
        _chatQuotaService.ThrowOnCallNumber = 2;

        await Assert.ThrowsExactlyAsync<ChatQuotaExceededException>(
            () => _service.SendMessageAsync(
                SessionId,
                new SendChatMessageCommand("Câu hỏi hợp lệ"),
                _actor,
                CancellationToken.None));

        Assert.AreEqual(1, _completionService.CallCount);
        Assert.IsEmpty(_repository.AddedMessages);
        Assert.IsEmpty(_repository.AddedRetrievalLogs);
        Assert.AreEqual(0, _repository.SaveChangesCallCount);
        Assert.AreEqual(2, _chatQuotaService.CallCount);
    }

    [TestMethod]
    public async Task StreamMessageAsync_WithRetrievedChunks_StreamsThenPersistsCompleteAnswer()
    {
        _repository.SearchResults.Add(CreateSearchResult());
        _completionService.Answer = "Đây là |câu trả lời [1].";
        var events = new List<ChatStreamEventDto>();

        await foreach (var item in _service.StreamMessageAsync(
                           SessionId,
                           new SendChatMessageCommand("Dependency injection là gì?"),
                           _actor,
                           CancellationToken.None))
        {
            events.Add(item);
        }

        Assert.AreEqual("Đây là câu trả lời [1].", string.Concat(
            events.Where(item => item.Type == "delta").Select(item => item.Content)));
        Assert.AreEqual("completed", events[^1].Type);
        Assert.AreEqual("Đây là câu trả lời [1].", _repository.AddedMessages[1].Content);
        Assert.HasCount(1, _repository.AddedRetrievalLogs);
        Assert.AreEqual(1, _repository.SaveChangesCallCount);
    }

    [TestMethod]
    public async Task StreamMessageAsync_GeminiFailure_DoesNotPersistMessages()
    {
        _repository.SearchResults.Add(CreateSearchResult());
        _completionService.ExceptionToThrow = new BusinessValidationException("Gemini unavailable");

        await Assert.ThrowsExactlyAsync<BusinessValidationException>(async () =>
        {
            await foreach (var _ in _service.StreamMessageAsync(
                               SessionId,
                               new SendChatMessageCommand("Câu hỏi hợp lệ"),
                               _actor,
                               CancellationToken.None))
            {
            }
        });

        Assert.IsEmpty(_repository.AddedMessages);
        Assert.AreEqual(0, _repository.SaveChangesCallCount);
    }

    [TestMethod]
    public async Task StreamMessageAsync_FailureAfterPartialDelta_DoesNotPersistMessages()
    {
        _repository.SearchResults.Add(CreateSearchResult());
        _completionService.Answer = "Partial";
        _completionService.ThrowAfterFirstDelta = true;

        await Assert.ThrowsExactlyAsync<BusinessValidationException>(async () =>
        {
            await foreach (var _ in _service.StreamMessageAsync(
                               SessionId,
                               new SendChatMessageCommand("Câu hỏi hợp lệ"),
                               _actor,
                               CancellationToken.None))
            {
            }
        });

        Assert.IsEmpty(_repository.AddedMessages);
        Assert.AreEqual(0, _repository.SaveChangesCallCount);
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
    public async Task SendMessageAsync_AnswerWithoutCitation_DoesNotInventSource()
    {
        _repository.SearchResults.Add(CreateSearchResult());
        _completionService.Answer = "Dependency injection cung cấp dependency từ bên ngoài.";

        var result = await _service.SendMessageAsync(
            SessionId,
            new SendChatMessageCommand("Dependency injection là gì?"),
            _actor,
            CancellationToken.None);

        Assert.IsEmpty(result.AssistantMessage.Citations);
        Assert.IsEmpty(_repository.AddedRetrievalLogs);
    }

    [TestMethod]
    public async Task SendMessageAsync_WithHistory_IncludesRecentConversationInPrompt()
    {
        _repository.SearchResults.Add(CreateSearchResult());
        _repository.ExistingMessages.Add(new Message
        {
            ChatSessionId = SessionId,
            Role = MessageRole.User,
            Content = "SOLID là gì?"
        });
        _repository.ExistingMessages.Add(new Message
        {
            ChatSessionId = SessionId,
            Role = MessageRole.Assistant,
            Content = "SOLID gồm năm nguyên lý."
        });
        _completionService.Answer = "Nguyên lý đầu tiên là SRP [1].";

        await _service.SendMessageAsync(
            SessionId,
            new SendChatMessageCommand("Nguyên lý đầu tiên là gì?"),
            _actor,
            CancellationToken.None);

        Assert.Contains("Người học: SOLID là gì?", _completionService.LastPrompt);
        Assert.Contains("Trợ lý: SOLID gồm năm nguyên lý.", _completionService.LastPrompt);
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

        public bool ThrowAfterFirstDelta { get; set; }

        public int CallCount { get; private set; }

        public string LastPrompt { get; private set; } = string.Empty;

        public Task<string> GenerateAsync(string prompt, CancellationToken cancellationToken)
        {
            CallCount++;
            LastPrompt = prompt;
            return ExceptionToThrow is null
                ? Task.FromResult(Answer)
                : Task.FromException<string>(ExceptionToThrow);
        }

        public async IAsyncEnumerable<string> StreamAsync(
            string prompt,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            CallCount++;
            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }

            foreach (var part in Answer.Split('|'))
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return part;
                if (ThrowAfterFirstDelta)
                {
                    throw new BusinessValidationException("Truncated");
                }
                await Task.Yield();
            }
        }
    }

    private sealed class FakeChatQuotaService : IChatQuotaService
    {
        public Exception? ExceptionToThrow { get; set; }

        public int? ThrowOnCallNumber { get; set; }

        public int CallCount { get; private set; }

        public Task EnsureCanSendMessageAsync(Guid userId, CancellationToken cancellationToken)
        {
            CallCount++;
            if (ThrowOnCallNumber == CallCount)
            {
                return Task.FromException(new ChatQuotaExceededException(
                    "Bạn đã sử dụng hết 1 tin nhắn trong ngày. Hãy nâng cấp gói để tiếp tục."));
            }

            return ExceptionToThrow is null
                ? Task.CompletedTask
                : Task.FromException(ExceptionToThrow);
        }
    }

    private sealed class FakeChatRepository : IChatRepository
    {
        public bool CanAccessCourse { get; set; } = true;

        public List<ChatSession> Sessions { get; } = [];

        public List<Message> AddedMessages { get; } = [];

        public List<Message> ExistingMessages { get; } = [];

        public List<RetrievalLog> AddedRetrievalLogs { get; } = [];

        public List<RetrievedDocumentChunk> SearchResults { get; } = [];

        public int SaveChangesCallCount { get; private set; }

        public int TransactionCallCount { get; private set; }

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

        public Task<IReadOnlyList<Message>> ListRecentMessagesAsync(
            Guid sessionId,
            int limit,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<Message>>(ExistingMessages
                .Where(message => message.ChatSessionId == sessionId)
                .TakeLast(limit)
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

        public Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken)
        {
            TransactionCallCount++;
            return operation(cancellationToken);
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
