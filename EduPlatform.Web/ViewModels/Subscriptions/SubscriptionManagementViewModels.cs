using EduPlatform.BLL.DTOs.Subscriptions;

namespace EduPlatform.Web.ViewModels.Subscriptions;

public sealed record SubscriptionManagementViewModel(
    SubscriptionSummaryDto? CurrentSubscription,
    IReadOnlyList<SubscriptionHistoryItemViewModel> History)
{
    public bool HasCurrentSubscription => CurrentSubscription is not null;
}

public sealed record SubscriptionHistoryItemViewModel(
    SubscriptionSummaryDto Subscription,
    string StatusLabel,
    string StatusBadgeClass,
    bool CanCancel,
    bool CanRenew);
