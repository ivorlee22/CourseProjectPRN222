using EduPlatform.BLL.Exceptions;
using EduPlatform.BLL.Interfaces;
using EduPlatform.DAL.Entities;
using EduPlatform.DAL.Repositories;

namespace EduPlatform.BLL.Services;

public sealed class SubscriptionCourseQuotaService(
    ISubscriptionRepository subscriptionRepository,
    IPackageRepository packageRepository,
    IUserRepository userRepository) : ICourseQuotaService
{
    public async Task EnsureCanJoinCourseAsync(
        Guid userId,
        int currentActiveCourseCount,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new ResourceNotFoundException("Không tìm thấy người dùng.");

        if (user.Role == UserRole.Admin || user.Role == UserRole.Teacher)
        {
            return;
        }

        var subscription = await subscriptionRepository.GetActiveSubscriptionAsync(
            userId,
            cancellationToken);

        var maxCourses = subscription is null
            ? (await GetFreePackageAsync(cancellationToken)).MaxCourses
            : subscription.Package.MaxCourses;

        if (currentActiveCourseCount >= maxCourses)
        {
            throw new CourseQuotaExceededException(
                "Bạn đã hết quota tham gia khóa học. Hãy nâng cấp gói để tham gia thêm khóa học.");
        }
    }

    private async Task<Package> GetFreePackageAsync(CancellationToken cancellationToken)
    {
        return await packageRepository.GetFreePackageAsync(cancellationToken)
            ?? throw new BusinessValidationException(
                "Không tìm thấy gói Free để áp dụng quota mặc định.");
    }
}
