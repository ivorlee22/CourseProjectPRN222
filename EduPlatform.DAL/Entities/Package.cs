namespace EduPlatform.DAL.Entities;

public sealed class Package : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public int MaxCourses { get; set; }

    public int DailyChats { get; set; }

    public int DurationDays { get; set; } = 30;

    public bool IsActive { get; set; } = true;

    public ICollection<Subscription> Subscriptions { get; set; } = [];

    public ICollection<Payment> Payments { get; set; } = [];
}
