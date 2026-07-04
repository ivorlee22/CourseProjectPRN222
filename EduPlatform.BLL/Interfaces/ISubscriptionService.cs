using EduPlatform.BLL.DTOs.Subscriptions;

namespace EduPlatform.BLL.Interfaces;

public interface ISubscriptionService
{
    Task<SubscriptionSummaryDto?> GetActiveSubscriptionAsync(Guid userId, CancellationToken cancellationToken);

    Task<IReadOnlyList<SubscriptionSummaryDto>> GetUserSubscriptionsAsync(Guid userId, CancellationToken cancellationToken);

    Task<Guid> CreateSubscriptionAsync(CreateSubscriptionCommand command, CancellationToken cancellationToken);

    Task CancelSubscriptionAsync(Guid subscriptionId, Guid userId, CancellationToken cancellationToken);
}
