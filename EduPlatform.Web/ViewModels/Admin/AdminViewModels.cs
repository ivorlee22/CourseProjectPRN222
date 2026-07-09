using System.ComponentModel.DataAnnotations;
using EduPlatform.BLL.DTOs.Reports;
using EduPlatform.BLL.DTOs.Users;
using EduPlatform.BLL.Enums;
using EduPlatform.BLL.Models;
using Microsoft.AspNetCore.Http;

namespace EduPlatform.Web.ViewModels.Admin;

public sealed class AdminDashboardViewModel
{
    public AdminDashboardReportDto Report { get; set; } = null!;

    public ReportDateRange Range { get; set; } = null!;

    public ReportPeriodGrouping Grouping { get; set; } = ReportPeriodGrouping.Day;
}

public sealed class UserListViewModel
{
    public PagedResult<UserSummaryDto> Users { get; set; } = null!;

    public string? Keyword { get; set; }
}

public sealed class CreateUserViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
    [StringLength(160, MinimumLength = 1, ErrorMessage = "Họ và tên phải có từ 1 đến 160 ký tự.")]
    [Display(Name = "Họ và tên")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập email.")]
    [EmailAddress(ErrorMessage = "Định dạng email không hợp lệ.")]
    [StringLength(320, ErrorMessage = "Email không được vượt quá 320 ký tự.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Mật khẩu phải có ít nhất 8 ký tự.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn vai trò.")]
    [Display(Name = "Vai trò")]
    public UserRole Role { get; set; } = UserRole.Student;
}

public sealed class EditUserRoleViewModel
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Vui lòng chọn vai trò.")]
    [Display(Name = "Vai trò")]
    public UserRole Role { get; set; }
}

public sealed class ImportUsersViewModel
{
    [Required(ErrorMessage = "Vui lòng chọn file Excel.")]
    [Display(Name = "File Excel (.xlsx)")]
    public IFormFile? ExcelFile { get; set; }
}
