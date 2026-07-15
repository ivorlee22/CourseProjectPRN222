using EduPlatform.BLL.DTOs.Packages;
using EduPlatform.BLL.DTOs.Subscriptions;

namespace EduPlatform.Web.ViewModels.Packages;

public sealed record PackagePricingViewModel(
    IReadOnlyList<PackagePricingItemViewModel> Packages,
    SubscriptionSummaryDto? CurrentSubscription);

public sealed record PackagePricingItemViewModel(
    PackageDto Package,
    bool IsCurrent,
    bool IsFeatured,
    string Tagline,
    string AccentLabel,
    IReadOnlyList<string> Highlights);

public sealed record PaymentCheckoutViewModel(
    PackageDto Package,
    bool IsCurrent,
    SubscriptionSummaryDto? CurrentSubscription);
