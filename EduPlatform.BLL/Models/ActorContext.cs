using EduPlatform.BLL.Enums;

namespace EduPlatform.BLL.Models;

public sealed record ActorContext(Guid UserId, UserRole Role)
{
    public bool IsAdmin => Role == UserRole.Admin;
}
