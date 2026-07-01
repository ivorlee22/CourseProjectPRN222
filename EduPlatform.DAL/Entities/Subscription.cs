namespace EduPlatform.DAL.Entities;

public sealed class Subscription : BaseEntity
{
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public Guid PackageId { get; set; }

    public Package Package { get; set; } = null!;

    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Pending;

    public DateTimeOffset StartsAtUtc { get; set; }

    public DateTimeOffset EndsAtUtc { get; set; }

    public DateTimeOffset? CancelledAtUtc { get; set; }

    public ICollection<Payment> Payments { get; set; } = [];
}
