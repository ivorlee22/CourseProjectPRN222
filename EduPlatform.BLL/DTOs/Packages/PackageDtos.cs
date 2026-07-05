namespace EduPlatform.BLL.DTOs.Packages;

public record PackageDto(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    int MaxCourses,
    int DailyChats,
    int DurationDays);
