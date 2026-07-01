namespace EduPlatform.DAL.Entities;

public sealed class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;

    public string NormalizedEmail { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.Student;

    public bool IsActive { get; set; } = true;

    public ICollection<Course> CreatedCourses { get; set; } = [];

    public ICollection<CourseEnrollment> CourseEnrollments { get; set; } = [];

    public ICollection<ChatSession> ChatSessions { get; set; } = [];

    public ICollection<Subscription> Subscriptions { get; set; } = [];

    public ICollection<Payment> Payments { get; set; } = [];
}
