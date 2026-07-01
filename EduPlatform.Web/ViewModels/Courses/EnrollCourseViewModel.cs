using System.ComponentModel.DataAnnotations;

namespace EduPlatform.Web.ViewModels.Courses;

public sealed class EnrollCourseViewModel
{
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu tham gia")]
    public string? EnrollmentPassword { get; init; }
}
