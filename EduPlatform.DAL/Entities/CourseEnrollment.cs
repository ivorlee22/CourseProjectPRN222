namespace EduPlatform.DAL.Entities;

public sealed class CourseEnrollment : BaseEntity
{
    public Guid CourseId { get; set; }

    public Course Course { get; set; } = null!;

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Pending;

    public DateTimeOffset? EnrolledAtUtc { get; set; }

    public Guid? InvitedById { get; set; }
}
