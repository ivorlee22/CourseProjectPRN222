namespace EduPlatform.DAL.Repositories;

/// <summary>
/// Projection result for a pending course invitation query.
/// Not an entity — this is a read-only query result produced by a JOIN.
/// </summary>
public sealed record PendingCourseInvitation(
    Guid CourseId,
    string CourseTitle,
    string InviterName);
