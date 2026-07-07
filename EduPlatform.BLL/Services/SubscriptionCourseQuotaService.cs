using EduPlatform.BLL.Exceptions;
using EduPlatform.BLL.Interfaces;
using EduPlatform.DAL.Entities;
using EduPlatform.DAL.Repositories;

namespace EduPlatform.BLL.Services;

public sealed class SubscriptionCourseQuotaService(
    ISubscriptionRepository subscriptionRepository,
    IPackageRepository packageRepository) : ICourseQuotaService
{
    public async Task EnsureCanCreateCourseAsync(
        Guid userId,
        int currentCourseCount,
        CancellationToken cancellationToken)
    {
        var subscription = await subscriptionRepository.GetActiveSubscriptionAsync(
            userId,
            cancellationToken);
        var package = subscription?.Package
            ?? await GetFreePackageAsync(cancellationToken);

        if (currentCourseCount >= package.MaxCourses)
        {
            throw new CourseQuotaExceededException(
                "Ban da het quota tao khoa hoc. Hay nang cap goi de tao them khoa hoc.");
        }
    }

    private async Task<Package> GetFreePackageAsync(CancellationToken cancellationToken)
    {
        return await packageRepository.GetFreePackageAsync(cancellationToken)
            ?? throw new BusinessValidationException(
                "Khong tim thay goi Free de ap dung quota mac dinh.");
    }
}
