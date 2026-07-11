namespace EduPlatform.BLL.DTOs.Chats;

public sealed record CreateChatSessionCommand(Guid CourseId, string? Title);

public sealed record SendChatMessageCommand(string Question);

public sealed record ChatSessionDto(
    Guid Id,
    Guid CourseId,
    string Title,
    DateTimeOffset? LastMessageAtUtc,
    DateTimeOffset CreatedAtUtc);

public sealed record ChatCitationDto(
    Guid DocumentChunkId,
    Guid DocumentId,
    string DocumentName,
    int Sequence,
    int? PageNumber,
    string? Section,
    string Content,
    double SimilarityScore,
    int Rank);

public sealed record ChatMessageDto(
    Guid Id,
    string Role,
    string Content,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<ChatCitationDto> Citations);

public sealed record ChatResponseDto(
    Guid SessionId,
    ChatMessageDto UserMessage,
    ChatMessageDto AssistantMessage);

public sealed record ChatStreamEventDto(
    string Type,
    string? Content = null,
    Guid? MessageId = null,
    IReadOnlyList<ChatCitationDto>? Citations = null);
