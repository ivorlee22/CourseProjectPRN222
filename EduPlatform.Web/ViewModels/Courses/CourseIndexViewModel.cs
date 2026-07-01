using EduPlatform.BLL.DTOs.Courses;

namespace EduPlatform.Web.ViewModels.Courses;

public sealed record CourseIndexViewModel(
    IReadOnlyList<CourseSummaryDto> Courses,
    string? Keyword,
    int PageNumber,
    int TotalPages,
    int TotalCount,
    bool MineOnly);
