using EduPlatform.BLL.DTOs.Chats;
using EduPlatform.BLL.Exceptions;
using EduPlatform.BLL.Interfaces;
using EduPlatform.Web.Security;
using EduPlatform.Web.ViewModels.Chats;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduPlatform.Web.Controllers;

[Authorize]
public sealed class ChatController(
    IChatService chatService,
    ICourseService courseService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(
        Guid courseId,
        Guid? sessionId,
        CancellationToken cancellationToken)
    {
        if (courseId == Guid.Empty)
        {
            return BadRequest();
        }

        var actor = User.GetRequiredActor();
        var sessions = await chatService.GetSessionsAsync(
            courseId,
            actor,
            cancellationToken);
        var course = await courseService.GetByIdAsync(
            courseId,
            actor,
            cancellationToken);

        ChatSessionDto? activeSession = null;
        IReadOnlyList<ChatMessageDto> messages = [];
        var activeSessionId = sessionId
            ?? (sessions.Count > 0 ? sessions[0].Id : null);
        if (activeSessionId.HasValue)
        {
            activeSession = sessions.SingleOrDefault(item => item.Id == activeSessionId.Value);
            if (activeSession is null)
            {
                return NotFound();
            }

            messages = await chatService.GetMessagesAsync(
                activeSession.Id,
                actor,
                cancellationToken);
        }

        return View(new ChatPageViewModel(
            course.Id,
            course.Title,
            sessions,
            activeSession,
            messages,
            new ChatMessageInputViewModel()));
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        Guid courseId,
        CancellationToken cancellationToken)
    {
        try
        {
            var sessionId = await chatService.CreateSessionAsync(
                new CreateChatSessionCommand(courseId, null),
                User.GetRequiredActor(),
                cancellationToken);
            return RedirectToAction(nameof(Index), new { courseId, sessionId });
        }
        catch (Exception exception) when (IsUserFacingException(exception))
        {
            TempData["ErrorMessage"] = exception.Message;
            return RedirectToAction(nameof(Index), new { courseId });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Send(
        Guid sessionId,
        ChatMessageInputViewModel input,
        CancellationToken cancellationToken)
    {
        var session = await chatService.GetSessionAsync(
            sessionId,
            User.GetRequiredActor(),
            cancellationToken);
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = ModelState.Values
                .SelectMany(value => value.Errors)
                .Select(error => error.ErrorMessage)
                .FirstOrDefault() ?? "Câu hỏi chưa hợp lệ.";
            return RedirectToAction(nameof(Index), new { courseId = session.CourseId, sessionId });
        }

        try
        {
            await chatService.SendMessageAsync(
                sessionId,
                new SendChatMessageCommand(input.Question),
                User.GetRequiredActor(),
                cancellationToken);
            return RedirectToAction(
                nameof(Index),
                new { courseId = session.CourseId, sessionId, sent = true });
        }
        catch (Exception exception) when (IsUserFacingException(exception))
        {
            TempData["ErrorMessage"] = exception.Message;
            return RedirectToAction(nameof(Index), new { courseId = session.CourseId, sessionId });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Delete(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var session = await chatService.GetSessionAsync(
            sessionId,
            User.GetRequiredActor(),
            cancellationToken);
        await chatService.DeleteSessionAsync(
            sessionId,
            User.GetRequiredActor(),
            cancellationToken);
        TempData["SuccessMessage"] = "Đã xóa cuộc trò chuyện.";
        return RedirectToAction(nameof(Index), new { courseId = session.CourseId });
    }

    private static bool IsUserFacingException(Exception exception)
    {
        return exception is BusinessValidationException
            or ResourceConflictException;
    }
}
