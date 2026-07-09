using System.ComponentModel.DataAnnotations;

namespace EduPlatform.Web.ViewModels.Courses;

public sealed class InviteCourseViewModel
{
    public Guid CourseId { get; init; }

    public string CourseTitle { get; init; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập email hoặc tên học viên.")]
    [StringLength(320, ErrorMessage = "Email hoặc tên không được vượt quá 320 ký tự.")]
    [Display(Name = "Email hoặc tên học viên")]
    public string StudentLookup { get; init; } = string.Empty;
}
