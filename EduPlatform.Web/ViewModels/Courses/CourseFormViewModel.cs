using System.ComponentModel.DataAnnotations;
using EduPlatform.BLL.Enums;

namespace EduPlatform.Web.ViewModels.Courses;

public sealed class CourseFormViewModel
{
    public Guid? Id { get; init; }

    [Required(ErrorMessage = "Vui lòng nhập tên khóa học.")]
    [StringLength(200, ErrorMessage = "Tên khóa học không vượt quá 200 ký tự.")]
    [Display(Name = "Tên khóa học")]
    public string Title { get; init; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mô tả.")]
    [StringLength(4000, ErrorMessage = "Mô tả không vượt quá 4000 ký tự.")]
    [Display(Name = "Mô tả")]
    public string Description { get; init; } = string.Empty;

    [Display(Name = "Loại khóa học")]
    public CourseType Type { get; init; } = CourseType.Public;

    [Display(Name = "Hiển thị khóa học")]
    public bool IsVisible { get; init; } = true;

    [StringLength(
        72,
        MinimumLength = 6,
        ErrorMessage = "Mật khẩu phải có từ 6 đến 72 ký tự.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu tham gia")]
    public string? EnrollmentPassword { get; init; }

    [Display(Name = "Xóa mật khẩu hiện tại")]
    public bool RemoveEnrollmentPassword { get; init; }

    public bool HasExistingPassword { get; init; }
}
