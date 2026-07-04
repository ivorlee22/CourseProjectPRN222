using EduPlatform.BLL.DTOs.Subscriptions;
using EduPlatform.BLL.Exceptions;
using EduPlatform.BLL.Interfaces;
using EduPlatform.DAL.Entities;
using EduPlatform.DAL.Repositories;

namespace EduPlatform.BLL.Services;

public sealed class SubscriptionService(
    ISubscriptionRepository subscriptionRepository,
    IPackageRepository packageRepository,
    TimeProvider timeProvider) : ISubscriptionService
{
    public async Task<SubscriptionSummaryDto?> GetActiveSubscriptionAsync(Guid userId, CancellationToken cancellationToken)
    {
        var subscription = await subscriptionRepository.GetActiveSubscriptionAsync(userId, cancellationToken);
        if (subscription is null)
        {
            return null;
        }

        return Map(subscription);
    }

    public async Task<IReadOnlyList<SubscriptionSummaryDto>> GetUserSubscriptionsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var subscriptions = await subscriptionRepository.GetByUserIdAsync(userId, cancellationToken);
        return subscriptions.Select(Map).ToArray();
    }

    public async Task<Guid> CreateSubscriptionAsync(CreateSubscriptionCommand command, CancellationToken cancellationToken)
    {
        var package = await packageRepository.GetByIdAsync(command.PackageId, cancellationToken)
            ?? throw new ResourceNotFoundException("Không tìm thấy gói cước.");

        if (!package.IsActive)
        {
            throw new BusinessValidationException("Gói cước này không còn khả dụng.");
        }

        // Logic check if user already has an active or pending subscription for the same package might go here, 
        // but often we allow creating a new one that starts after the current one, or just replacing it.
        // For simplicity, we just create a pending one that awaits payment.

        var now = timeProvider.GetUtcNow();
        
        var subscription = new Subscription
        {
            UserId = command.UserId,
            PackageId = package.Id,
            Status = SubscriptionStatus.Pending,
            StartsAtUtc = now,
            EndsAtUtc = now.AddDays(package.DurationDays) // Tentative, usually confirmed upon payment
        };

        await subscriptionRepository.AddAsync(subscription, cancellationToken);
        await subscriptionRepository.SaveChangesAsync(cancellationToken);

        return subscription.Id;
    }

    public async Task CancelSubscriptionAsync(Guid subscriptionId, Guid userId, CancellationToken cancellationToken)
    {
        var subscription = await subscriptionRepository.GetByIdAsync(subscriptionId, cancellationToken)
            ?? throw new ResourceNotFoundException("Không tìm thấy thông tin đăng ký.");

        if (subscription.UserId != userId)
        {
            throw new ForbiddenOperationException("Bạn không có quyền quản lý gói đăng ký này.");
        }

        if (subscription.Status == SubscriptionStatus.Cancelled)
        {
            throw new BusinessValidationException("Gói đăng ký đã được hủy trước đó.");
        }

        subscription.Status = SubscriptionStatus.Cancelled;
        subscription.CancelledAtUtc = timeProvider.GetUtcNow();

        subscriptionRepository.Update(subscription);
        await subscriptionRepository.SaveChangesAsync(cancellationToken);
    }

    private static SubscriptionSummaryDto Map(Subscription subscription)
    {
        return new SubscriptionSummaryDto(
            subscription.Id,
            subscription.PackageId,
            subscription.Package.Name,
            subscription.Package.MaxCourses,
            subscription.Package.DailyChats,
            subscription.Status.ToString(),
            subscription.StartsAtUtc,
            subscription.EndsAtUtc);
    }
}
