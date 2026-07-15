namespace EduPlatform.BLL.Interfaces;

public interface ICourseQuotaService
{
    Task EnsureCanJoinCourseAsync(
        Guid userId,
        int currentActiveCourseCount,
        CancellationToken cancellationToken);
}
