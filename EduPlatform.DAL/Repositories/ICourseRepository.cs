using EduPlatform.DAL.Entities;

namespace EduPlatform.DAL.Repositories;

public interface ICourseRepository
{
    Task<Course?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<(IReadOnlyList<Course> Items, int TotalCount)> SearchAsync(
        string? keyword,
        int pageNumber,
        int pageSize,
        bool visibleOnly,
        Guid? ownerId,
        CancellationToken cancellationToken);

    Task<int> CountByOwnerAsync(Guid ownerId, CancellationToken cancellationToken);

    Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken);

    Task<CourseEnrollment?> GetEnrollmentAsync(
        Guid courseId,
        Guid userId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<CourseEnrollment>> GetStudentsAsync(
        Guid courseId,
        CancellationToken cancellationToken);

    Task AddAsync(Course course, CancellationToken cancellationToken);

    Task AddEnrollmentAsync(CourseEnrollment enrollment, CancellationToken cancellationToken);

    void Remove(Course course);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
