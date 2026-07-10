using EduPlatform.BLL.DTOs.Courses;
using EduPlatform.BLL.Exceptions;
using EduPlatform.BLL.Interfaces;
using EduPlatform.Web.Hubs;
using EduPlatform.Web.Security;
using EduPlatform.Web.ViewModels.Courses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;

namespace EduPlatform.Web.Controllers;

public sealed class CourseController(
    ICourseService courseService,
    IUserService userService,
    IHubContext<CourseHub> courseHubContext) : Controller
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Index(
        string? keyword,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        var actor = User.GetActorOrDefault();
        if (actor?.Role == EduPlatform.BLL.Enums.UserRole.Teacher)
        {
            return RedirectToAction(nameof(Mine), new { page });
        }

        var result = await courseService.SearchAsync(
            new CourseSearchQuery(keyword, page),
            actor,
            cancellationToken);

        IReadOnlyList<CourseInvitationDto>? pendingInvitations = null;
        if (actor is not null && actor.Role == EduPlatform.BLL.Enums.UserRole.Student)
        {
            pendingInvitations = await courseService.GetPendingInvitationsAsync(actor, cancellationToken);
        }

        return View(new CourseIndexViewModel(
            result.Items,
            keyword,
            result.PageNumber,
            result.TotalPages,
            result.TotalCount,
            MineOnly: false,
            pendingInvitations));
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Mine(
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        var actor = User.GetRequiredActor();
        var result = await courseService.SearchAsync(
            new CourseSearchQuery(null, page, MineOnly: true),
            actor,
            cancellationToken);

        IReadOnlyList<CourseInvitationDto>? pendingInvitations = null;
        if (actor.Role == EduPlatform.BLL.Enums.UserRole.Student)
        {
            pendingInvitations = await courseService.GetPendingInvitationsAsync(actor, cancellationToken);
        }

        return View("Index", new CourseIndexViewModel(
            result.Items,
            null,
            result.PageNumber,
            result.TotalPages,
            result.TotalCount,
            MineOnly: true,
            pendingInvitations));
    }

    [HttpGet]
    public async Task<IActionResult> ListFragment(
        string? keyword,
        bool mineOnly,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        var actor = User.GetActorOrDefault();
        var result = await courseService.SearchAsync(
            new CourseSearchQuery(keyword, page, MineOnly: mineOnly),
            actor,
            cancellationToken);

        IReadOnlyList<CourseInvitationDto>? pendingInvitations = null;
        if (actor is not null && actor.Role == EduPlatform.BLL.Enums.UserRole.Student)
        {
            pendingInvitations = await courseService.GetPendingInvitationsAsync(actor, cancellationToken);
        }

        return PartialView("_CourseList", new CourseIndexViewModel(
            result.Items,
            keyword,
            result.PageNumber,
            result.TotalPages,
            result.TotalCount,
            mineOnly,
            pendingInvitations));
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Details(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var course = await courseService.GetByIdAsync(
                id,
                User.GetActorOrDefault(),
                cancellationToken);
            var actor = User.GetActorOrDefault();
            var canAdminister = actor?.IsAdmin == true;
            var canTeach = actor is not null
                && (actor.IsAdmin || actor.UserId == course.OwnerId);
            var canViewDocuments = canTeach
                || course.IsEnrolled
                || (actor is not null && course.IsVisible && course.Type == EduPlatform.BLL.Enums.CourseType.Public);

            return View(new CourseDetailsViewModel(
                course,
                canAdminister,
                canTeach,
                User.Identity?.IsAuthenticated == true,
                canViewDocuments));
        }
        catch (ResourceNotFoundException)
        {
            return NotFound();
        }
    }

    [Authorize(Policy = AuthorizationPolicies.CanCreateCourse)]
    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        await PopulateTeachersViewBagAsync(cancellationToken);
        return View(new CourseFormViewModel());
    }

    private async Task PopulateTeachersViewBagAsync(CancellationToken cancellationToken)
    {
        var teachers = await userService.GetByRoleAsync(EduPlatform.BLL.Enums.UserRole.Teacher, cancellationToken);
        ViewBag.Teachers = teachers.Select(t => new SelectListItem
        {
            Value = t.Id.ToString(),
            Text = $"{t.FullName} ({t.Email})"
        });
    }

    [Authorize(Policy = AuthorizationPolicies.CanCreateCourse)]
    [HttpPost]
    public async Task<IActionResult> Create(
        CourseFormViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await PopulateTeachersViewBagAsync(cancellationToken);
            return View(model);
        }

        try
        {
            var id = await courseService.CreateAsync(
                new CreateCourseCommand(
                    model.Title,
                    model.Description,
                    model.Type,
                    model.IsVisible,
                    model.EnrollmentPassword,
                    model.OwnerId),
                User.GetRequiredActor(),
                cancellationToken);

            TempData["SuccessMessage"] = "Đã tạo khóa học.";
            await NotifyCourseChangedAsync(cancellationToken);
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception exception) when (AddBusinessError(exception))
        {
            await PopulateTeachersViewBagAsync(cancellationToken);
            return View(model);
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> Edit(
        Guid id,
        CancellationToken cancellationToken)
    {
        var course = await courseService.GetByIdAsync(
            id,
            User.GetRequiredActor(),
            cancellationToken);
        EnsureCanAdminister(course);
        await PopulateTeachersViewBagAsync(cancellationToken);

        return View(new CourseFormViewModel
        {
            Id = course.Id,
            OwnerId = course.OwnerId,
            Title = course.Title,
            Description = course.Description,
            Type = course.Type,
            IsVisible = course.IsVisible,
            HasExistingPassword = course.RequiresEnrollmentPassword
        });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Edit(
        Guid id,
        CourseFormViewModel model,
        CancellationToken cancellationToken)
    {
        if (model.Id != id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            await PopulateTeachersViewBagAsync(cancellationToken);
            return View(model);
        }

        try
        {
            await courseService.UpdateAsync(
                id,
                new UpdateCourseCommand(
                    model.Title,
                    model.Description,
                    model.Type,
                    model.IsVisible,
                    model.EnrollmentPassword,
                    model.RemoveEnrollmentPassword,
                    model.OwnerId),
                User.GetRequiredActor(),
                cancellationToken);

            TempData["SuccessMessage"] = "Đã cập nhật khóa học.";
            await NotifyCourseChangedAsync(cancellationToken);
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception exception) when (AddBusinessError(exception))
        {
            await PopulateTeachersViewBagAsync(cancellationToken);
            return View(model);
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        await courseService.DeleteAsync(id, User.GetRequiredActor(), cancellationToken);
        await NotifyCourseChangedAsync(cancellationToken);
        TempData["SuccessMessage"] = "Đã xóa khóa học.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> SetVisibility(
        Guid id,
        bool isVisible,
        CancellationToken cancellationToken)
    {
        await courseService.SetVisibilityAsync(
            id,
            isVisible,
            User.GetRequiredActor(),
            cancellationToken);
        await NotifyCourseChangedAsync(cancellationToken);

        TempData["SuccessMessage"] = isVisible
            ? "Khóa học đã được hiển thị."
            : "Khóa học đã được ẩn.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Enroll(
        Guid id,
        EnrollCourseViewModel model,
        CancellationToken cancellationToken)
    {
        try
        {
            await courseService.EnrollAsync(
                id,
                model.EnrollmentPassword,
                User.GetRequiredActor(),
                cancellationToken);
            TempData["SuccessMessage"] = "Bạn đã tham gia khóa học.";
        }
        catch (Exception exception) when (IsBusinessException(exception))
        {
            TempData["ErrorMessage"] = exception.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Invite(
        Guid id,
        CancellationToken cancellationToken)
    {
        var course = await courseService.GetByIdAsync(
            id,
            User.GetRequiredActor(),
            cancellationToken);
        EnsureCanManage(course);

        return View(new InviteCourseViewModel
        {
            CourseId = course.Id,
            CourseTitle = course.Title
        });
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Invite(
        Guid id,
        InviteCourseViewModel model,
        CancellationToken cancellationToken)
    {
        if (id != model.CourseId)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var studentId = await courseService.InviteAsync(
                id,
                model.StudentLookup,
                User.GetRequiredActor(),
                cancellationToken);

            var course = await courseService.GetByIdAsync(id, User.GetRequiredActor(), cancellationToken);
            var inviterName = User.Identity?.Name ?? "Admin";

            // Realtime push
            await courseHubContext.Clients.User(studentId.ToString()).SendAsync(
                "ReceiveInvitation",
                new
                {
                    courseId = id,
                    courseTitle = course.Title,
                    inviterName = inviterName
                },
                cancellationToken);

            TempData["SuccessMessage"] = "Đã gửi lời mời tham gia khóa học.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception exception) when (AddBusinessError(exception))
        {
            return View(model);
        }
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CancelInvitation(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken)
    {
        try
        {
            await courseService.CancelInvitationAsync(
                id,
                userId,
                User.GetRequiredActor(),
                cancellationToken);

            // Realtime push
            await courseHubContext.Clients.User(userId.ToString()).SendAsync(
                "CancelInvitation",
                id,
                cancellationToken);

            TempData["SuccessMessage"] = "Đã hủy lời mời tham gia khóa học.";
        }
        catch (Exception exception)
        {
            TempData["ErrorMessage"] = exception.Message;
        }

        return RedirectToAction(nameof(Students), new { id });
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> RespondInvitation(
        Guid id,
        bool accept,
        CancellationToken cancellationToken)
    {
        await courseService.RespondToInvitationAsync(
            id,
            accept,
            User.GetRequiredActor(),
            cancellationToken);

        TempData["SuccessMessage"] = accept
            ? "Đã chấp nhận lời mời."
            : "Đã từ chối lời mời.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Students(
        Guid id,
        CancellationToken cancellationToken)
    {
        var students = await courseService.GetStudentsAsync(
            id,
            User.GetRequiredActor(),
            cancellationToken);
        var course = await courseService.GetByIdAsync(
            id,
            User.GetRequiredActor(),
            cancellationToken);

        ViewData["CourseTitle"] = course.Title;
        ViewData["CourseId"] = id;
        return View(students);
    }

    private void EnsureCanManage(CourseDetailsDto course)
    {
        var actor = User.GetRequiredActor();

        if (!actor.IsAdmin && actor.UserId != course.OwnerId)
        {
            throw new ForbiddenOperationException(
                "Bạn không có quyền quản lý khóa học này.");
        }
    }

    private void EnsureCanAdminister(CourseDetailsDto course)
    {
        var actor = User.GetRequiredActor();

        if (!actor.IsAdmin)
        {
            throw new ForbiddenOperationException(
                "Chỉ quản trị viên mới có quyền chỉnh sửa, ẩn hoặc xóa khóa học.");
        }
    }

    private bool AddBusinessError(Exception exception)
    {
        if (!IsBusinessException(exception))
        {
            return false;
        }

        ModelState.AddModelError(string.Empty, exception.Message);
        return true;
    }

    private static bool IsBusinessException(Exception exception)
    {
        return exception is BusinessValidationException
            or ResourceConflictException
            or ResourceNotFoundException
            or CourseQuotaExceededException;
    }

    private Task NotifyCourseChangedAsync(CancellationToken cancellationToken)
    {
        return courseHubContext.Clients.All.SendAsync(
            "CourseChanged",
            cancellationToken);
    }
}
