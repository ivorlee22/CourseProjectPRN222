namespace EduPlatform.DAL.Entities;

public sealed class Payment : BaseEntity
{
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public Guid PackageId { get; set; }

    public Package Package { get; set; } = null!;

    public Guid? SubscriptionId { get; set; }

    public Subscription? Subscription { get; set; }

    public decimal Amount { get; set; }

    public PaymentMethod Method { get; set; }

    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    public string InternalReference { get; set; } = string.Empty;

    public string? GatewayTransactionId { get; set; }

    public string? GatewayResponseCode { get; set; }

    public string? RawResponseJson { get; set; }

    public DateTimeOffset? ProcessedAtUtc { get; set; }
}
