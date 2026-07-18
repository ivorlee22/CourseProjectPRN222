using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;
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

public sealed partial class ChatService(
    IChatRepository chatRepository,
    IEmbeddingService embeddingService,
    IChatCompletionService completionService,
    IChatQuotaService chatQuotaService,
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

    public async Task<ChatSessionDto> GetSessionAsync(
        Guid sessionId,
        ActorContext actor,
        CancellationToken cancellationToken)
    {
        var session = await GetOwnedSessionAsync(sessionId, actor, cancellationToken);
        await EnsureCourseAccessAsync(session.CourseId, actor, cancellationToken);
        return MapSession(session);
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
        var prepared = await PrepareMessageAsync(sessionId, command, actor, cancellationToken);

        var answer = prepared.Chunks.Length == 0
            ? _options.EmptyContextMessage
            : await completionService.GenerateAsync(
                BuildPrompt(prepared.Question, prepared.Chunks, prepared.History),
                cancellationToken);
        return await PersistResponseAsync(prepared, answer, actor.UserId, cancellationToken);
    }

    public async IAsyncEnumerable<ChatStreamEventDto> StreamMessageAsync(
        Guid sessionId,
        SendChatMessageCommand command,
        ActorContext actor,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var prepared = await PrepareMessageAsync(sessionId, command, actor, cancellationToken);
        var answerBuilder = new StringBuilder();

        if (prepared.Chunks.Length == 0)
        {
            answerBuilder.Append(_options.EmptyContextMessage);
            yield return new ChatStreamEventDto("delta", _options.EmptyContextMessage);
        }
        else
        {
            await foreach (var delta in completionService.StreamAsync(
                               BuildPrompt(prepared.Question, prepared.Chunks, prepared.History),
                               cancellationToken))
            {
                if (string.IsNullOrEmpty(delta))
                {
                    continue;
                }

                answerBuilder.Append(delta);
                yield return new ChatStreamEventDto("delta", delta);
            }
        }

        var answer = answerBuilder.ToString().Trim();
        if (answer.Length == 0)
        {
            throw new BusinessValidationException("Gemini did not return a usable answer.");
        }

        var response = await PersistResponseAsync(prepared, answer, actor.UserId, cancellationToken);
        yield return new ChatStreamEventDto(
            "completed",
            MessageId: response.AssistantMessage.Id,
            Citations: response.AssistantMessage.Citations);
    }

    private async Task<PreparedMessage> PrepareMessageAsync(
        Guid sessionId,
        SendChatMessageCommand command,
        ActorContext actor,
        CancellationToken cancellationToken)
    {
        var question = ValidateQuestion(command.Question);
        var session = await GetOwnedSessionAsync(sessionId, actor, cancellationToken);
        await EnsureCourseAccessAsync(session.CourseId, actor, cancellationToken);
        await EnsureCanSendMessageAsync(actor.UserId, cancellationToken);

        var history = await chatRepository.ListRecentMessagesAsync(
            session.Id,
            Math.Clamp(_options.HistoryMessageLimit, 0, 20),
            cancellationToken);

        var queryEmbedding = await embeddingService.EmbedQueryAsync(question, cancellationToken);
        if (queryEmbedding.Length != embeddingService.Dimensions)
        {
            throw new BusinessValidationException(
                $"Query embedding must contain {embeddingService.Dimensions} values.");
        }

        var retrievedChunks = await chatRepository.SearchChunksAsync(
            session.CourseId,
            new Vector(queryEmbedding),
            Math.Clamp(_options.RetrievalLimit, 1, 20),
            cancellationToken);
        var minimumSimilarity = Math.Clamp(_options.MinimumSimilarityScore, -1d, 1d);
        var chunks = retrievedChunks
            .Where(chunk => chunk.SimilarityScore >= minimumSimilarity)
            .ToArray();
        return new PreparedMessage(session, question, chunks, history.ToArray());
    }

    private async Task<ChatResponseDto> PersistResponseAsync(
        PreparedMessage prepared,
        string answer,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var session = prepared.Session;
        var question = prepared.Question;
        var citedChunks = SelectCitedChunks(answer, prepared.Chunks);

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
        var retrievalLogs = citedChunks.Select(citation => new RetrievalLog
        {
            Id = Guid.NewGuid(),
            MessageId = assistantMessage.Id,
            DocumentChunkId = citation.Chunk.Chunk.Id,
            SimilarityScore = citation.Chunk.SimilarityScore,
            Rank = citation.Rank,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        }).ToArray();

        await chatRepository.ExecuteInTransactionAsync(async transactionToken =>
        {
            await chatQuotaService.EnsureCanSendMessageAsync(userId, transactionToken);

            session.LastMessageAtUtc = now;
            if (session.Title == "Cuộc trò chuyện mới")
            {
                session.Title = question.Length <= 80 ? question : $"{question[..77]}...";
            }

            await chatRepository.AddMessagesAsync([userMessage, assistantMessage], transactionToken);
            if (retrievalLogs.Length > 0)
            {
                await chatRepository.AddRetrievalLogsAsync(retrievalLogs, transactionToken);
            }

            await chatRepository.SaveChangesAsync(transactionToken);
        }, cancellationToken);

        var citations = citedChunks
            .Select(citation => MapCitation(citation.Chunk, citation.Rank))
            .ToArray();
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

    public async Task DeleteSessionAsync(
        Guid sessionId,
        ActorContext actor,
        CancellationToken cancellationToken)
    {
        var session = await GetOwnedSessionAsync(sessionId, actor, cancellationToken);
        chatRepository.RemoveSession(session);
        await chatRepository.SaveChangesAsync(cancellationToken);
    }

    private Task EnsureCanSendMessageAsync(Guid userId, CancellationToken cancellationToken)
    {
        return chatRepository.ExecuteInTransactionAsync(
            transactionToken => chatQuotaService.EnsureCanSendMessageAsync(userId, transactionToken),
            cancellationToken);
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
        RetrievedDocumentChunk[] chunks,
        Message[] history)
    {
        var builder = new StringBuilder();
        builder.AppendLine("You are an educational assistant for EduPlatform.");
        builder.AppendLine("CRITICAL RULE: You MUST reply in the EXACT SAME LANGUAGE that the user uses in their question. If they ask in English, reply in English. If they ask in Vietnamese, reply in Vietnamese.");
        builder.AppendLine("Only answer based on the provided context.");
        builder.AppendLine("If the context does not contain enough information, clearly state that the documents do not provide enough information.");
        builder.AppendLine("Cite sources using [1], [2] corresponding to the document chunks provided below.");
        builder.AppendLine("Keep your answer concise, complete, and do not repeat the entire document.");
        builder.AppendLine();
        if (history.Length > 0)
        {
            builder.AppendLine("RECENT HISTORY (for understanding context only, not as a source of truth):");
            var remainingHistory = Math.Max(_options.MaxHistoryCharacters, 0);
            var selectedHistory = new List<(Message Message, string Content)>();
            foreach (var message in history.Reverse())
            {
                if (remainingHistory == 0)
                {
                    break;
                }

                var content = message.Content.Length <= remainingHistory
                    ? message.Content
                    : message.Content[..remainingHistory];
                selectedHistory.Add((message, content));
                remainingHistory -= content.Length;
            }

            foreach (var item in selectedHistory.AsEnumerable().Reverse())
            {
                var message = item.Message;
                builder.Append(message.Role == MessageRole.User ? "User: " : "Assistant: ");
                builder.AppendLine(item.Content);
            }

            builder.AppendLine();
        }

        builder.AppendLine("CONTEXT:");

        var remaining = Math.Max(_options.MaxContextCharacters, 1000);
        for (var index = 0; index < chunks.Length && remaining > 0; index++)
        {
            var chunk = chunks[index];
            var header = $"[{index + 1}] {chunk.DocumentName}"
                + (chunk.Chunk.PageNumber.HasValue ? $", page {chunk.Chunk.PageNumber}" : string.Empty);
            builder.AppendLine(header);
            var content = chunk.Chunk.Content.Length <= remaining
                ? chunk.Chunk.Content
                : chunk.Chunk.Content[..remaining];
            builder.AppendLine(content);
            builder.AppendLine();
            remaining -= content.Length;
        }

        builder.AppendLine("QUESTION:");
        builder.AppendLine(question);
        return builder.ToString();
    }

    private static CitedChunk[] SelectCitedChunks(
        string answer,
        RetrievedDocumentChunk[] chunks)
    {
        if (chunks.Length == 0)
        {
            return [];
        }

        var citedRanks = CitationPattern()
            .Matches(answer)
            .Select(match => int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture))
            .Where(rank => rank >= 1 && rank <= chunks.Length)
            .Distinct()
            .ToArray();

        return citedRanks
            .Select(rank => new CitedChunk(chunks[rank - 1], rank))
            .ToArray();
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

    private sealed record CitedChunk(RetrievedDocumentChunk Chunk, int Rank);

    private sealed record PreparedMessage(
        ChatSession Session,
        string Question,
        RetrievedDocumentChunk[] Chunks,
        Message[] History);

    [GeneratedRegex(@"\[(\d+)\]", RegexOptions.CultureInvariant)]
    private static partial Regex CitationPattern();
}
