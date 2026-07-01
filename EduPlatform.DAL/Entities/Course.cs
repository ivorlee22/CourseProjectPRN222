namespace EduPlatform.DAL.Entities;

public sealed class Course : BaseEntity
{
    public Guid OwnerId { get; set; }

    public User Owner { get; set; } = null!;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public CourseType Type { get; set; } = CourseType.Public;

    public bool IsVisible { get; set; } = true;

    public string? EnrollmentPasswordHash { get; set; }

    public ICollection<CourseEnrollment> Enrollments { get; set; } = [];

    public ICollection<Document> Documents { get; set; } = [];

    public ICollection<ChatSession> ChatSessions { get; set; } = [];
}
