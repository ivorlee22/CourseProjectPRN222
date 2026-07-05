using System.Runtime.CompilerServices;
using EduPlatform.BLL.DTOs.Chats;
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
        await foreach (var item in chatService.StreamMessageAsync(
                           sessionId,
                           new SendChatMessageCommand(question),
                           actor,
                           cancellationToken))
        {
            yield return item;
        }
    }
}
