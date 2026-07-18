using EduPlatform.BLL.DTOs.Documents;
using EduPlatform.BLL.Exceptions;
using EduPlatform.BLL.Interfaces;
using EduPlatform.BLL.Models;
using EduPlatform.Web.Security;
using EduPlatform.Web.ViewModels.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduPlatform.Web.Controllers;

public sealed class DocumentController(
    IDocumentService documentService,
    ICourseService courseService) : Controller
{
    private const long MaxFileSizeBytes = 25L * 1024L * 1024;

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Index(
        Guid courseId,
        CancellationToken cancellationToken)
    {
        var actor = User.GetRequiredActor();
        var course = await courseService.GetByIdAsync(courseId, actor, cancellationToken);

        var documents = await documentService.ListByCourseAsync(
            courseId,
            actor,
            cancellationToken);

        return View(new DocumentIndexViewModel(
            course.Id,
            course.Title,
            documents,
            CanManageCourse(course.OwnerId, actor)));
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Details(
        Guid id,
        CancellationToken cancellationToken)
    {
        var actor = User.GetRequiredActor();
        var document = await documentService.GetByIdAsync(id, actor, cancellationToken);
        var course = await courseService.GetByIdAsync(
            document.CourseId,
            actor,
            cancellationToken);

        var canManage = CanManageCourse(course.OwnerId, actor);
        var chunks = canManage || actor.IsAdmin
            ? await documentService.ListChunksAsync(id, actor, cancellationToken)
            : Array.Empty<DocumentChunkDto>();

        return View(new DocumentDetailsViewModel(
            document,
            chunks,
            canManage,
            canManage || actor.IsAdmin));
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Upload(
        Guid courseId,
        CancellationToken cancellationToken)
    {
        var actor = User.GetRequiredActor();
        var course = await courseService.GetByIdAsync(courseId, actor, cancellationToken);

        if (!CanManageCourse(course.OwnerId, actor))
        {
            throw new ForbiddenOperationException(
                "Bạn không có quyền tải tài liệu cho khóa học này.");
        }

        return View(new UploadDocumentViewModel
        {
            CourseId = course.Id,
            CourseTitle = course.Title
        });
    }

    [Authorize]
    [HttpPost]
    [RequestSizeLimit(MaxFileSizeBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxFileSizeBytes)]
    public async Task<IActionResult> Upload(
        UploadDocumentViewModel model,
        CancellationToken cancellationToken)
    {
        var actor = User.GetRequiredActor();

        if (!ModelState.IsValid || model.File is null)
        {
            return View(model);
        }

        if (model.File.Length == 0)
        {
            ModelState.AddModelError(
                nameof(model.File),
                "Tệp tải lên trống.");
            return View(model);
        }

        try
        {
            await using var stream = model.File.OpenReadStream();
            var id = await documentService.UploadAsync(
                new UploadDocumentCommand(
                    model.CourseId,
                    model.File.FileName,
                    model.File.ContentType,
                    model.File.Length,
                    stream),
                actor,
                cancellationToken);

            TempData["SuccessMessage"] = "Đã tải lên và xử lý tài liệu.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception exception) when (IsBusinessException(exception))
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return View(model);
        }
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var actor = User.GetRequiredActor();
        var document = await documentService.GetByIdAsync(id, actor, cancellationToken);
        await documentService.DeleteAsync(id, actor, cancellationToken);

        TempData["SuccessMessage"] = "Đã xóa tài liệu.";
        return RedirectToAction(nameof(Index), new { courseId = document.CourseId });
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> ChunkEmbedding(
        Guid id,
        Guid chunkId,
        CancellationToken cancellationToken)
    {
        var actor = User.GetRequiredActor();
        var embedding = await documentService.GetChunkEmbeddingAsync(id, chunkId, actor, cancellationToken);

        if (embedding == null)
        {
            return NotFound(new { message = "Không tìm thấy vector nhúng hoặc bạn không có quyền truy cập." });
        }

        return Json(new { vector = embedding });
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Download(
        Guid id,
        CancellationToken cancellationToken)
    {
        var actor = User.GetRequiredActor();

        try
        {
            var url = await documentService.GetDownloadUrlAsync(id, actor, cancellationToken);
            return Redirect(url);
        }
        catch (Exception exception) when (IsBusinessException(exception) || exception is ResourceNotFoundException)
        {
            TempData["ErrorMessage"] = exception.Message;
            return RedirectToAction(nameof(Index), new { courseId = Guid.Empty }); // Or back to where they were
        }
    }

    private static bool CanManageCourse(Guid ownerId, ActorContext actor)
    {
        return actor.IsAdmin || actor.UserId == ownerId;
    }

    private static bool IsBusinessException(Exception exception)
    {
        return exception is BusinessValidationException
            or ResourceConflictException
            or DocumentProcessingException;
    }
}