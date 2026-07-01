using System.ComponentModel.DataAnnotations;

namespace EduPlatform.Web.ViewModels.Courses;

public sealed class InviteCourseViewModel
{
    public Guid CourseId { get; init; }

    public string CourseTitle { get; init; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mã người dùng.")]
    [Display(Name = "Mã người dùng")]
    public Guid UserId { get; init; }
}
