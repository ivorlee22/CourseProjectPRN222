using EduPlatform.BLL.DTOs.Courses;

namespace EduPlatform.Web.ViewModels.Courses;

public sealed record CourseDetailsViewModel(
    CourseDetailsDto Course,
    bool CanAdminister,
    bool CanTeach,
    bool IsAuthenticated,
    bool CanViewDocuments);
