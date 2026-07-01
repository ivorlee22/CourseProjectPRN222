namespace EduPlatform.DAL.Entities;

public sealed class ChatSession : BaseEntity
{
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public Guid CourseId { get; set; }

    public Course Course { get; set; } = null!;

    public string Title { get; set; } = string.Empty;

    public DateTimeOffset? LastMessageAtUtc { get; set; }

    public ICollection<Message> Messages { get; set; } = [];
}
