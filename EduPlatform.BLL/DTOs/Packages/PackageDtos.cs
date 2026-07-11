namespace EduPlatform.BLL.DTOs.Packages;

public record PackageDto(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    int MaxCourses,
    int DailyChats,
    int DurationDays,
    bool IsActive);

public record PackageAdminDto(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    int MaxCourses,
    int DailyChats,
    int DurationDays,
    bool IsActive,
    DateTimeOffset CreatedAtUtc);

public record CreatePackageCommand(
    string Name,
    string Description,
    decimal Price,
    int MaxCourses,
    int DailyChats,
    int DurationDays,
    bool IsActive);

public record UpdatePackageCommand(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    int MaxCourses,
    int DailyChats,
    int DurationDays,
    bool IsActive);
