using EduPlatform.BLL.DTOs.Courses;
using EduPlatform.BLL.Models;

namespace EduPlatform.BLL.Interfaces;

public interface ICourseService
{
    Task<PagedResult<CourseSummaryDto>> SearchAsync(
        CourseSearchQuery query,
        ActorContext? actor,
        CancellationToken cancellationToken);

    Task<CourseDetailsDto> GetByIdAsync(
        Guid id,
        ActorContext? actor,
        CancellationToken cancellationToken);

    Task<Guid> CreateAsync(
        CreateCourseCommand command,
        ActorContext actor,
        CancellationToken cancellationToken);

    Task UpdateAsync(
        Guid id,
        UpdateCourseCommand command,
        ActorContext actor,
        CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, ActorContext actor, CancellationToken cancellationToken);

    Task SetVisibilityAsync(
        Guid id,
        bool isVisible,
        ActorContext actor,
        CancellationToken cancellationToken);

    Task EnrollAsync(
        Guid id,
        string? enrollmentPassword,
        ActorContext actor,
        CancellationToken cancellationToken);

    Task InviteAsync(
        Guid id,
        Guid userId,
        ActorContext actor,
        CancellationToken cancellationToken);

    Task RespondToInvitationAsync(
        Guid id,
        bool accept,
        ActorContext actor,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<CourseStudentDto>> GetStudentsAsync(
        Guid id,
        ActorContext actor,
        CancellationToken cancellationToken);
}
