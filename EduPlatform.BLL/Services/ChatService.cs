using System.Text;
using EduPlatform.BLL.DTOs.Chats;
using EduPlatform.BLL.Exceptions;
using EduPlatform.BLL.Interfaces;
using EduPlatform.BLL.Models;
using EduPlatform.BLL.Options;
using EduPlatform.DAL.Entities;
using EduPlatform.DAL.Repositories;
using Microsoft.Extensions.Options;
using Pgvector;

namespace EduPlatform.BLL.Services;

public sealed class ChatService(
    IChatRepository chatRepository,
    IEmbeddingService embeddingService,
    IChatCompletionService completionService,
    IOptions<ChatOptions> options,
    TimeProvider timeProvider) : IChatService
{
    private readonly ChatOptions _options = options.Value;

    public async Task<Guid> CreateSessionAsync(
        CreateChatSessionCommand command,
        ActorContext actor,
        CancellationToken cancellationToken)
    {
        await EnsureCourseAccessAsync(command.CourseId, actor, cancellationToken);

        var title = string.IsNullOrWhiteSpace(command.Title)
            ? "Cuộc trò chuyện mới"
            : command.Title.Trim();
        if (title.Length > 200)
        {
            throw new BusinessValidationException("Tiêu đề cuộc trò chuyện không được quá 200 ký tự.");
        }

        var session = new ChatSession
        {
            Id = Guid.NewGuid(),
            UserId = actor.UserId,
            CourseId = command.CourseId,
            Title = title
        };

        await chatRepository.AddSessionAsync(session, cancellationToken);
        await chatRepository.SaveChangesAsync(cancellationToken);
        return session.Id;
    }

    public async Task<IReadOnlyList<ChatSessionDto>> GetSessionsAsync(
        Guid? courseId,
        ActorContext actor,
        CancellationToken cancellationToken)
    {
        if (courseId.HasValue)
        {
            await EnsureCourseAccessAsync(courseId.Value, actor, cancellationToken);
        }

        var sessions = await chatRepository.ListSessionsAsync(
            actor.UserId,
            courseId,
            cancellationToken);
        return sessions.Select(MapSession).ToArray();
    }

    public async Task<IReadOnlyList<ChatMessageDto>> GetMessagesAsync(
        Guid sessionId,
        ActorContext actor,
        CancellationToken cancellationToken)
    {
        var session = await GetOwnedSessionAsync(sessionId, actor, cancellationToken);
        await EnsureCourseAccessAsync(session.CourseId, actor, cancellationToken);
        var messages = await chatRepository.ListMessagesAsync(sessionId, cancellationToken);
        return messages.Select(MapMessage).ToArray();
    }

    public async Task<ChatResponseDto> SendMessageAsync(
        Guid sessionId,
        SendChatMessageCommand command,
        ActorContext actor,
        CancellationToken cancellationToken)
    {
        var question = ValidateQuestion(command.Question);
        var session = await GetOwnedSessionAsync(sessionId, actor, cancellationToken);
        await EnsureCourseAccessAsync(session.CourseId, actor, cancellationToken);

        var queryEmbedding = await embeddingService.EmbedQueryAsync(question, cancellationToken);
        if (queryEmbedding.Length != embeddingService.Dimensions)
        {
            throw new BusinessValidationException(
                $"Query embedding must contain {embeddingService.Dimensions} values.");
        }

        var chunks = await chatRepository.SearchChunksAsync(
            session.CourseId,
            new Vector(queryEmbedding),
            Math.Clamp(_options.RetrievalLimit, 1, 20),
            cancellationToken);

        var answer = chunks.Count == 0
            ? _options.EmptyContextMessage
            : await completionService.GenerateAsync(
                BuildPrompt(question, chunks),
                cancellationToken);

        var now = timeProvider.GetUtcNow();
        var userMessage = new Message
        {
            Id = Guid.NewGuid(),
            ChatSessionId = session.Id,
            Role = MessageRole.User,
            Content = question,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        var assistantMessage = new Message
        {
            Id = Guid.NewGuid(),
            ChatSessionId = session.Id,
            Role = MessageRole.Assistant,
            Content = answer,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        var retrievalLogs = chunks.Select((chunk, index) => new RetrievalLog
        {
            Id = Guid.NewGuid(),
            MessageId = assistantMessage.Id,
            DocumentChunkId = chunk.Chunk.Id,
            SimilarityScore = chunk.SimilarityScore,
            Rank = index + 1,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        }).ToArray();

        session.LastMessageAtUtc = now;
        if (session.Title == "Cuộc trò chuyện mới")
        {
            session.Title = question.Length <= 80 ? question : $"{question[..77]}...";
        }

        await chatRepository.AddMessagesAsync([userMessage, assistantMessage], cancellationToken);
        if (retrievalLogs.Length > 0)
        {
            await chatRepository.AddRetrievalLogsAsync(retrievalLogs, cancellationToken);
        }

        await chatRepository.SaveChangesAsync(cancellationToken);

        var citations = chunks.Select((chunk, index) => MapCitation(chunk, index + 1)).ToArray();
        return new ChatResponseDto(
            session.Id,
            MapMessage(userMessage),
            new ChatMessageDto(
                assistantMessage.Id,
                assistantMessage.Role.ToString(),
                assistantMessage.Content,
                assistantMessage.CreatedAtUtc,
                citations));
    }

    private async Task<ChatSession> GetOwnedSessionAsync(
        Guid sessionId,
        ActorContext actor,
        CancellationToken cancellationToken)
    {
        var session = await chatRepository.GetSessionAsync(sessionId, cancellationToken)
            ?? throw new ResourceNotFoundException("Không tìm thấy cuộc trò chuyện.");
        if (session.UserId != actor.UserId)
        {
            throw new ForbiddenOperationException("Bạn không có quyền truy cập cuộc trò chuyện này.");
        }

        return session;
    }

    private async Task EnsureCourseAccessAsync(
        Guid courseId,
        ActorContext actor,
        CancellationToken cancellationToken)
    {
        if (!await chatRepository.CanAccessCourseAsync(
                courseId,
                actor.UserId,
                actor.IsAdmin,
                cancellationToken))
        {
            throw new ForbiddenOperationException("Bạn không có quyền chat trong khóa học này.");
        }
    }

    private string ValidateQuestion(string question)
    {
        var value = question?.Trim() ?? string.Empty;
        if (value.Length == 0 || value.Length > _options.MaxQuestionLength)
        {
            throw new BusinessValidationException(
                $"Câu hỏi phải có từ 1 đến {_options.MaxQuestionLength} ký tự.");
        }

        return value;
    }

    private string BuildPrompt(
        string question,
        IReadOnlyList<RetrievedDocumentChunk> chunks)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Bạn là trợ lý học tập của EduPlatform.");
        builder.AppendLine("Chỉ trả lời dựa trên ngữ cảnh được cung cấp.");
        builder.AppendLine("Nếu ngữ cảnh không đủ, hãy nói rõ rằng tài liệu chưa cung cấp đủ thông tin.");
        builder.AppendLine("Trích dẫn nguồn bằng ký hiệu [1], [2] tương ứng với các đoạn bên dưới.");
        builder.AppendLine();
        builder.AppendLine("NGỮ CẢNH:");

        var remaining = Math.Max(_options.MaxContextCharacters, 1000);
        for (var index = 0; index < chunks.Count && remaining > 0; index++)
        {
            var chunk = chunks[index];
            var header = $"[{index + 1}] {chunk.DocumentName}"
                + (chunk.Chunk.PageNumber.HasValue ? $", trang {chunk.Chunk.PageNumber}" : string.Empty);
            builder.AppendLine(header);
            var content = chunk.Chunk.Content.Length <= remaining
                ? chunk.Chunk.Content
                : chunk.Chunk.Content[..remaining];
            builder.AppendLine(content);
            builder.AppendLine();
            remaining -= content.Length;
        }

        builder.AppendLine("CÂU HỎI:");
        builder.AppendLine(question);
        return builder.ToString();
    }

    private static ChatSessionDto MapSession(ChatSession session)
    {
        return new ChatSessionDto(
            session.Id,
            session.CourseId,
            session.Title,
            session.LastMessageAtUtc,
            session.CreatedAtUtc);
    }

    private static ChatMessageDto MapMessage(Message message)
    {
        var citations = message.RetrievalLogs
            .OrderBy(log => log.Rank)
            .Select(log => new ChatCitationDto(
                log.DocumentChunkId,
                log.DocumentChunk.DocumentId,
                log.DocumentChunk.Document.OriginalFileName,
                log.DocumentChunk.Sequence,
                log.DocumentChunk.PageNumber,
                log.DocumentChunk.Section,
                log.DocumentChunk.Content,
                log.SimilarityScore,
                log.Rank))
            .ToArray();
        return new ChatMessageDto(
            message.Id,
            message.Role.ToString(),
            message.Content,
            message.CreatedAtUtc,
            citations);
    }

    private static ChatCitationDto MapCitation(RetrievedDocumentChunk chunk, int rank)
    {
        return new ChatCitationDto(
            chunk.Chunk.Id,
            chunk.Chunk.DocumentId,
            chunk.DocumentName,
            chunk.Chunk.Sequence,
            chunk.Chunk.PageNumber,
            chunk.Chunk.Section,
            chunk.Chunk.Content,
            chunk.SimilarityScore,
            rank);
    }
}
