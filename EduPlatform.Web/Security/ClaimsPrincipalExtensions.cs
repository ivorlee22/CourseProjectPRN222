using System.Security.Claims;
using EduPlatform.BLL.Enums;
using EduPlatform.BLL.Exceptions;
using EduPlatform.BLL.Models;

namespace EduPlatform.Web.Security;

public static class ClaimsPrincipalExtensions
{
    public static ActorContext? GetActorOrDefault(this ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var userIdValue = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var roleValue = principal.FindFirstValue(ClaimTypes.Role);

        if (!Guid.TryParse(userIdValue, out var userId)
            || !Enum.TryParse<UserRole>(roleValue, ignoreCase: true, out var role))
        {
            return null;
        }

        return new ActorContext(userId, role);
    }

    public static ActorContext GetRequiredActor(this ClaimsPrincipal principal)
    {
        return principal.GetActorOrDefault()
            ?? throw new ForbiddenOperationException(
                "Phiên đăng nhập không hợp lệ. Vui lòng đăng nhập lại.");
    }
}
