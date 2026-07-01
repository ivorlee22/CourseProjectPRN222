using EduPlatform.BLL.Enums;

namespace EduPlatform.BLL.DTOs.Courses;

public sealed record CourseSummaryDto(
    Guid Id,
    string Title,
    string Description,
    CourseType Type,
    bool IsVisible,
    Guid OwnerId,
    string OwnerName,
    int EnrollmentCount,
    DateTimeOffset CreatedAtUtc);

public sealed record CourseDetailsDto(
    Guid Id,
    string Title,
    string Description,
    CourseType Type,
    bool IsVisible,
    bool RequiresEnrollmentPassword,
    Guid OwnerId,
    string OwnerName,
    int EnrollmentCount,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record CourseStudentDto(
    Guid UserId,
    string FullName,
    string Email,
    EnrollmentStatus Status,
    DateTimeOffset? EnrolledAtUtc);

public sealed record CreateCourseCommand(
    string Title,
    string Description,
    CourseType Type,
    bool IsVisible,
    string? EnrollmentPassword);

public sealed record UpdateCourseCommand(
    string Title,
    string Description,
    CourseType Type,
    bool IsVisible,
    string? EnrollmentPassword,
    bool RemoveEnrollmentPassword);

public sealed record CourseSearchQuery(
    string? Keyword,
    int PageNumber = 1,
    int PageSize = 12,
    bool MineOnly = false,
    bool IncludeHidden = false);
