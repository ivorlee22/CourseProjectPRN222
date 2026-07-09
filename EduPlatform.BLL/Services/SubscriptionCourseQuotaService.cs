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
    public async Task EnsureCanCreateCourseAsync(
        Guid userId,
        int currentCourseCount,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new ResourceNotFoundException("Không tìm thấy người dùng.");

        if (user.Role == UserRole.Admin)
        {
            return;
        }

        var subscription = await subscriptionRepository.GetActiveSubscriptionAsync(
            userId,
            cancellationToken);
        var package = subscription?.Package
            ?? await GetFreePackageAsync(cancellationToken);

        if (currentCourseCount >= package.MaxCourses)
        {
            throw new CourseQuotaExceededException(
                "Bạn đã hết quota tạo khóa học. Hãy nâng cấp gói để tạo thêm khóa học.");
        }
    }

    private async Task<Package> GetFreePackageAsync(CancellationToken cancellationToken)
    {
        return await packageRepository.GetFreePackageAsync(cancellationToken)
            ?? throw new BusinessValidationException(
                "Khong tim thay goi Free de ap dung quota mac dinh.");
    }
}
