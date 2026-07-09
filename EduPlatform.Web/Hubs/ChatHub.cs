using System.Runtime.CompilerServices;
using EduPlatform.BLL.DTOs.Chats;
using EduPlatform.BLL.Exceptions;
using EduPlatform.BLL.Interfaces;
using EduPlatform.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.SignalR;

namespace EduPlatform.Web.Hubs;

[Authorize]
public sealed class ChatHub(
    IChatService chatService,
    IAntiforgery antiforgery) : Hub
{
    public async IAsyncEnumerable<ChatStreamEventDto> SendMessage(
        Guid sessionId,
        string question,
        string requestVerificationToken,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var httpContext = Context.GetHttpContext()
            ?? throw new HubException("Không thể xác thực yêu cầu chat.");
        httpContext.Request.Headers["X-CSRF-TOKEN"] = requestVerificationToken;
        await antiforgery.ValidateRequestAsync(httpContext);

        var actor = Context.User!.GetRequiredActor();
        if (actor.IsAdmin)
        {
            yield return new ChatStreamEventDto("error", "Admin không cần hỏi từ tài liệu.");
            yield break;
        }

        await using var stream = chatService.StreamMessageAsync(
            sessionId,
            new SendChatMessageCommand(question),
            actor,
            cancellationToken).GetAsyncEnumerator(cancellationToken);

        while (true)
        {
            ChatStreamEventDto? item = null;
            string? userFacingError = null;
            var completed = false;
            try
            {
                if (!await stream.MoveNextAsync())
                {
                    completed = true;
                }
                else
                {
                    item = stream.Current;
                }
            }
            catch (Exception exception) when (IsUserFacingException(exception))
            {
                userFacingError = exception.Message;
            }

            if (userFacingError is not null)
            {
                yield return new ChatStreamEventDto("error", userFacingError);
                yield break;
            }

            if (completed)
            {
                yield break;
            }

            yield return item!;
        }
    }

    private static bool IsUserFacingException(Exception exception)
    {
        return exception is BusinessValidationException
            or ChatQuotaExceededException
            or ResourceConflictException;
    }
}
