using System.Security.Claims;
using EduPlatform.BLL.DTOs.Reports;
using EduPlatform.BLL.DTOs.Users;
using EduPlatform.BLL.Enums;
using EduPlatform.BLL.Exceptions;
using EduPlatform.BLL.Interfaces;
using EduPlatform.BLL.Models;
using EduPlatform.Web.Security;
using EduPlatform.Web.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;

namespace EduPlatform.Web.Controllers;

[Authorize(Roles = "Admin")]
public sealed class AdminController(IUserService userService, IReportService reportService) : Controller
{
    private const string DefaultImportPassword = "EduPlatform@123";

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var nowUtc = DateTimeOffset.UtcNow;
        var startUtc = new DateTimeOffset(nowUtc.UtcDateTime.Date.AddDays(-29), TimeSpan.Zero);
        var range = new ReportDateRange(startUtc, nowUtc);
        var report = await reportService.GetAdminDashboardAsync(
            range,
            ReportPeriodGrouping.Day,
            topCourseLimit: 5,
            cancellationToken);

        return View(new AdminDashboardViewModel
        {
            Report = report,
            Range = range,
            Grouping = ReportPeriodGrouping.Day
        });
    }

    // ── User Management ───────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Users(
        string? keyword,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await userService.GetAllAsync(keyword, page, pageSize, cancellationToken);
        var viewModel = new UserListViewModel
        {
            Users = result,
            Keyword = keyword
        };
        return View(viewModel);
    }

    // ── Create User ───────────────────────────────────────────────────────────

    [HttpGet]
    public IActionResult CreateUser()
    {
        return View(new CreateUserViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser(CreateUserViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var actor = User.GetRequiredActor();
            await userService.CreateAsync(
                new CreateUserCommand(model.FullName, model.Email, model.Password, model.Role),
                actor,
                cancellationToken);

            TempData["SuccessMessage"] = $"Tạo người dùng {model.Email} thành công.";
            return RedirectToAction(nameof(Users));
        }
        catch (ResourceConflictException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
        catch (BusinessValidationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    // ── Edit Role ─────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> EditUser(Guid id, CancellationToken cancellationToken)
    {
        var user = await userService.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        return View(new EditUserRoleViewModel
        {
            UserId = user.Id,
            FullName = user.FullName,
            Role = user.Role
        });
    }

    [HttpPost]
    public async Task<IActionResult> EditUser(EditUserRoleViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var actor = User.GetRequiredActor();
            await userService.UpdateRoleAsync(
                new UpdateUserRoleCommand(model.UserId, model.Role),
                actor,
                cancellationToken);

            TempData["SuccessMessage"] = "Cập nhật vai trò thành công.";
            return RedirectToAction(nameof(Users));
        }
        catch (BusinessValidationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
        catch (ForbiddenOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
        catch (ResourceNotFoundException)
        {
            return NotFound();
        }
    }

    // ── Toggle Active ─────────────────────────────────────────────────────────

    [HttpPost]
    public async Task<IActionResult> ToggleUserActive(Guid id, bool isActive, CancellationToken cancellationToken)
    {
        try
        {
            var actor = User.GetRequiredActor();
            await userService.SetActiveAsync(id, isActive, actor, cancellationToken);

            TempData["SuccessMessage"] = isActive 
                ? "Đã mở khóa tài khoản." 
                : "Đã tạm khóa tài khoản.";
        }
        catch (BusinessValidationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (ForbiddenOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Users));
    }

    // ── Import Users (Excel) ──────────────────────────────────────────────────

    [HttpPost]
    public async Task<IActionResult> ImportUsers(ImportUsersViewModel model, CancellationToken cancellationToken)
    {
        if (model.ExcelFile is null || model.ExcelFile.Length == 0)
        {
            TempData["ErrorMessage"] = "Vui lòng chọn một file hợp lệ.";
            return RedirectToAction(nameof(Users));
        }

        // Đảm bảo license EPPlus
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        var importedCount = 0;
        var errorCount = 0;
        var actor = User.GetRequiredActor();

        using var stream = model.ExcelFile.OpenReadStream();
        using var package = new ExcelPackage(stream);

        var worksheet = package.Workbook.Worksheets.FirstOrDefault();
        if (worksheet is null)
        {
            TempData["ErrorMessage"] = "File Excel không có dữ liệu.";
            return RedirectToAction(nameof(Users));
        }

        var rowCount = worksheet.Dimension?.Rows ?? 0;

        // Giả sử: Cột 1 = Họ và tên, Cột 2 = Email, Cột 3 (Optional) = Role (Student/Teacher/Admin)
        // Bỏ qua dòng tiêu đề (row 1)
        for (int row = 2; row <= rowCount; row++)
        {
            var fullName = worksheet.Cells[row, 1].Text?.Trim();
            var email = worksheet.Cells[row, 2].Text?.Trim();
            var roleText = worksheet.Cells[row, 3].Text?.Trim();

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(fullName))
            {
                continue;
            }

            var role = UserRole.Student;
            if (!string.IsNullOrEmpty(roleText) && Enum.TryParse<UserRole>(roleText, true, out var parsedRole))
            {
                role = parsedRole;
            }

            try
            {
                // Create Async ném lỗi nếu email bị trùng
                await userService.CreateAsync(
                    new CreateUserCommand(fullName, email, DefaultImportPassword, role),
                    actor,
                    cancellationToken);
                
                importedCount++;
            }
            catch (Exception)
            {
                errorCount++;
            }
        }

        if (importedCount > 0)
        {
            TempData["SuccessMessage"] = $"Đã thêm thành công {importedCount} người dùng. Mật khẩu mặc định: {DefaultImportPassword} (Lỗi: {errorCount}).";
        }
        else
        {
            TempData["ErrorMessage"] = $"Không thể thêm người dùng nào. Có thể do email đã tồn tại (Lỗi: {errorCount}).";
        }

        return RedirectToAction(nameof(Users));
    }
}
