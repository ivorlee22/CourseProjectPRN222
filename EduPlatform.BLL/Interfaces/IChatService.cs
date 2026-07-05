using EduPlatform.BLL.DTOs.Chats;
using EduPlatform.BLL.Models;

namespace EduPlatform.BLL.Interfaces;

public interface IChatService
{
    Task<Guid> CreateSessionAsync(
        CreateChatSessionCommand command,
        ActorContext actor,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ChatSessionDto>> GetSessionsAsync(
        Guid? courseId,
        ActorContext actor,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ChatMessageDto>> GetMessagesAsync(
        Guid sessionId,
        ActorContext actor,
        CancellationToken cancellationToken);

    Task<ChatResponseDto> SendMessageAsync(
        Guid sessionId,
        SendChatMessageCommand command,
        ActorContext actor,
        CancellationToken cancellationToken);
}
