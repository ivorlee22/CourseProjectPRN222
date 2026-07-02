using EduPlatform.BLL.DTOs.Users;
using EduPlatform.BLL.Exceptions;
using EduPlatform.BLL.Interfaces;
using EduPlatform.BLL.Models;
using EduPlatform.DAL.Entities;
using EduPlatform.DAL.Repositories;
using BllUserRole = EduPlatform.BLL.Enums.UserRole;
using DalUserRole = EduPlatform.DAL.Entities.UserRole;

namespace EduPlatform.BLL.Services;

public sealed class UserService(IUserRepository userRepository) : IUserService
{
    private const int MaximumPageSize = 50;

    // ── Authentication ────────────────────────────────────────────────────────

    public async Task<UserSummaryDto> AuthenticateAsync(
        LoginCommand command,
        CancellationToken cancellationToken)
    {
        ValidateEmail(command.Email);

        var normalizedEmail = command.Email.Trim().ToUpperInvariant();
        var user = await userRepository.GetByNormalizedEmailAsync(normalizedEmail, cancellationToken);

        // Kiểm tra sai password và không tồn tại trả về cùng 1 thông báo để tránh user enumeration
        if (user is null || !BCrypt.Net.BCrypt.Verify(command.Password, user.PasswordHash))
        {
            throw new BusinessValidationException("Email hoặc mật khẩu không đúng.");
        }

        if (!user.IsActive)
        {
            throw new BusinessValidationException(
                "Tài khoản đã bị vô hiệu hóa. Vui lòng liên hệ quản trị viên.");
        }

        return MapSummary(user);
    }

    // ── Registration ──────────────────────────────────────────────────────────

    public async Task<Guid> RegisterAsync(RegisterCommand command, CancellationToken cancellationToken)
    {
        ValidateFullName(command.FullName);
        ValidateEmail(command.Email);
        ValidatePassword(command.Password);

        var normalizedEmail = command.Email.Trim().ToUpperInvariant();
        var existing = await userRepository.GetByNormalizedEmailAsync(normalizedEmail, cancellationToken);

        if (existing is not null)
        {
            throw new ResourceConflictException("Email này đã được đăng ký.");
        }

        var user = new User
        {
            FullName = command.FullName.Trim(),
            Email = command.Email.Trim(),
            NormalizedEmail = normalizedEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(command.Password),
            Role = DalUserRole.Student,
            IsActive = true
        };

        await userRepository.AddAsync(user, cancellationToken);
        await userRepository.SaveChangesAsync(cancellationToken);

        return user.Id;
    }

    // ── Admin — Create User ───────────────────────────────────────────────────

    public async Task<Guid> CreateAsync(
        CreateUserCommand command,
        ActorContext actor,
        CancellationToken cancellationToken)
    {
        EnsureIsAdmin(actor);
        ValidateFullName(command.FullName);
        ValidateEmail(command.Email);
        ValidatePassword(command.Password);

        var normalizedEmail = command.Email.Trim().ToUpperInvariant();
        var existing = await userRepository.GetByNormalizedEmailAsync(normalizedEmail, cancellationToken);

        if (existing is not null)
        {
            throw new ResourceConflictException("Email này đã được sử dụng.");
        }

        var user = new User
        {
            FullName = command.FullName.Trim(),
            Email = command.Email.Trim(),
            NormalizedEmail = normalizedEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(command.Password),
            Role = ToDal(command.Role),
            IsActive = true
        };

        await userRepository.AddAsync(user, cancellationToken);
        await userRepository.SaveChangesAsync(cancellationToken);

        return user.Id;
    }

    // ── Queries ───────────────────────────────────────────────────────────────

    public async Task<UserSummaryDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(id, cancellationToken);
        return user is null ? null : MapSummary(user);
    }

    public async Task<PagedResult<UserSummaryDto>> GetAllAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(1, pageNumber);
        var size = Math.Clamp(pageSize, 1, MaximumPageSize);

        var result = await userRepository.GetAllAsync(page, size, cancellationToken);

        return new PagedResult<UserSummaryDto>(
            result.Items.Select(MapSummary).ToArray(),
            page,
            size,
            result.TotalCount);
    }

    // ── Admin — Role & Status ─────────────────────────────────────────────────

    public async Task UpdateRoleAsync(
        UpdateUserRoleCommand command,
        ActorContext actor,
        CancellationToken cancellationToken)
    {
        EnsureIsAdmin(actor);

        if (command.UserId == actor.UserId)
        {
            throw new ForbiddenOperationException(
                "Bạn không thể thay đổi vai trò của chính mình.");
        }

        var user = await GetUserAsync(command.UserId, cancellationToken);
        user.Role = ToDal(command.NewRole);

        await userRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task SetActiveAsync(
        Guid userId,
        bool isActive,
        ActorContext actor,
        CancellationToken cancellationToken)
    {
        EnsureIsAdmin(actor);

        if (userId == actor.UserId)
        {
            throw new ForbiddenOperationException(
                "Bạn không thể vô hiệu hóa tài khoản của chính mình.");
        }

        var user = await GetUserAsync(userId, cancellationToken);
        user.IsActive = isActive;

        await userRepository.SaveChangesAsync(cancellationToken);
    }

    // ── Password ──────────────────────────────────────────────────────────────

    public async Task ChangePasswordAsync(
        ChangePasswordCommand command,
        CancellationToken cancellationToken)
    {
        ValidatePassword(command.NewPassword);

        var user = await GetUserAsync(command.UserId, cancellationToken);

        if (!BCrypt.Net.BCrypt.Verify(command.CurrentPassword, user.PasswordHash))
        {
            throw new BusinessValidationException("Mật khẩu hiện tại không đúng.");
        }

        if (command.CurrentPassword == command.NewPassword)
        {
            throw new BusinessValidationException("Mật khẩu mới phải khác mật khẩu hiện tại.");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(command.NewPassword);
        await userRepository.SaveChangesAsync(cancellationToken);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task<User> GetUserAsync(Guid id, CancellationToken cancellationToken)
    {
        return await userRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new ResourceNotFoundException("Không tìm thấy người dùng.");
    }

    private static UserSummaryDto MapSummary(User user)
    {
        return new UserSummaryDto(
            user.Id,
            user.FullName,
            user.Email,
            ToBll(user.Role),
            user.IsActive,
            user.CreatedAtUtc);
    }

    private static void EnsureIsAdmin(ActorContext actor)
    {
        if (!actor.IsAdmin)
        {
            throw new ForbiddenOperationException("Chỉ quản trị viên mới có thể thực hiện thao tác này.");
        }
    }

    private static void ValidateFullName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName) || fullName.Trim().Length > 160)
        {
            throw new BusinessValidationException("Họ và tên phải có từ 1 đến 160 ký tự.");
        }
    }

    private static void ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || email.Trim().Length > 320)
        {
            throw new BusinessValidationException("Email không hợp lệ.");
        }

        var trimmed = email.Trim();
        var atIndex = trimmed.IndexOf('@');

        if (atIndex <= 0
            || atIndex == trimmed.Length - 1
            || trimmed.IndexOf('.', atIndex) < 0)
        {
            throw new BusinessValidationException("Định dạng email không hợp lệ.");
        }
    }

    private static void ValidatePassword(string password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < 8)
        {
            throw new BusinessValidationException("Mật khẩu phải có ít nhất 8 ký tự.");
        }

        if (!password.Any(char.IsUpper))
        {
            throw new BusinessValidationException("Mật khẩu phải có ít nhất một chữ hoa.");
        }

        if (!password.Any(char.IsLower))
        {
            throw new BusinessValidationException("Mật khẩu phải có ít nhất một chữ thường.");
        }

        if (!password.Any(char.IsDigit))
        {
            throw new BusinessValidationException("Mật khẩu phải có ít nhất một chữ số.");
        }
    }

    private static DalUserRole ToDal(BllUserRole role)
    {
        return role switch
        {
            BllUserRole.Student => DalUserRole.Student,
            BllUserRole.Teacher => DalUserRole.Teacher,
            BllUserRole.Admin   => DalUserRole.Admin,
            _ => throw new BusinessValidationException("Vai trò người dùng không hợp lệ.")
        };
    }

    private static BllUserRole ToBll(DalUserRole role)
    {
        return role switch
        {
            DalUserRole.Student => BllUserRole.Student,
            DalUserRole.Teacher => BllUserRole.Teacher,
            DalUserRole.Admin   => BllUserRole.Admin,
            _ => throw new InvalidOperationException("Unsupported persisted user role.")
        };
    }
}
