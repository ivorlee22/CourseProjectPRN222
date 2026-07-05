namespace EduPlatform.BLL.DTOs.Subscriptions;

public record SubscriptionSummaryDto(
    Guid Id,
    Guid PackageId,
    string PackageName,
    int MaxCourses,
    int DailyChats,
    string Status,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc);

public record CreateSubscriptionCommand(Guid UserId, Guid PackageId);
