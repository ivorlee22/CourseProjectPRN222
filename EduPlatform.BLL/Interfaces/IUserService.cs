using EduPlatform.BLL.DTOs.Users;
using EduPlatform.BLL.Models;

namespace EduPlatform.BLL.Interfaces;

public interface IUserService
{
    /// <summary>Xác thực đăng nhập. Ném <see cref="Exceptions.BusinessValidationException"/> nếu sai.</summary>
    Task<UserSummaryDto> AuthenticateAsync(LoginCommand command, CancellationToken cancellationToken);

    /// <summary>Đăng ký tài khoản mới (role mặc định: Student).</summary>
    Task<Guid> RegisterAsync(RegisterCommand command, CancellationToken cancellationToken);

    /// <summary>Admin tạo tài khoản với role tùy chọn.</summary>
    Task<Guid> CreateAsync(CreateUserCommand command, ActorContext actor, CancellationToken cancellationToken);

    Task<UserSummaryDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<UserSummaryDto>> GetAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken);

    Task<IReadOnlyList<UserSummaryDto>> GetByRoleAsync(EduPlatform.BLL.Enums.UserRole role, CancellationToken cancellationToken);

    /// <summary>Admin thay đổi role của người dùng khác.</summary>
    Task UpdateRoleAsync(UpdateUserRoleCommand command, ActorContext actor, CancellationToken cancellationToken);

    /// <summary>Người dùng đổi mật khẩu của chính mình.</summary>
    Task ChangePasswordAsync(ChangePasswordCommand command, CancellationToken cancellationToken);

    /// <summary>Admin bật/tắt tài khoản.</summary>
    Task SetActiveAsync(Guid userId, bool isActive, ActorContext actor, CancellationToken cancellationToken);
}
