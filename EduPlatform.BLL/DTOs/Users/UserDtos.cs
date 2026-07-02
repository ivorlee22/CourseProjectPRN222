using EduPlatform.BLL.Enums;

namespace EduPlatform.BLL.DTOs.Users;

public sealed record UserSummaryDto(
    Guid Id,
    string FullName,
    string Email,
    UserRole Role,
    bool IsActive,
    DateTimeOffset CreatedAtUtc);

public sealed record LoginCommand(
    string Email,
    string Password);

public sealed record RegisterCommand(
    string FullName,
    string Email,
    string Password);

public sealed record CreateUserCommand(
    string FullName,
    string Email,
    string Password,
    UserRole Role);

public sealed record UpdateUserRoleCommand(
    Guid UserId,
    UserRole NewRole);

public sealed record ChangePasswordCommand(
    Guid UserId,
    string CurrentPassword,
    string NewPassword);
