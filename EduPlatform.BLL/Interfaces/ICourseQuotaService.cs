namespace EduPlatform.BLL.Interfaces;

public interface ICourseQuotaService
{
    Task EnsureCanCreateCourseAsync(
        Guid userId,
        int currentCourseCount,
        CancellationToken cancellationToken);
}
