using EduPlatform.DAL.Entities;
using EduPlatform.DAL.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EduPlatform.DAL.Repositories;

public sealed class CourseRepository(AppDbContext dbContext) : ICourseRepository
{
    public Task<Course?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.Courses
            .Include(x => x.Owner)
            .Include(x => x.Enrollments)
            .ThenInclude(x => x.User)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<(IReadOnlyList<Course> Items, int TotalCount)> SearchAsync(
        string? keyword,
        int pageNumber,
        int pageSize,
        bool visibleOnly,
        Guid? ownerId,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Courses
            .AsNoTracking()
            .Include(x => x.Owner)
            .Include(x => x.Enrollments)
            .AsSplitQuery();

        if (visibleOnly)
        {
            query = query.Where(x => x.IsVisible);
        }

        if (ownerId.HasValue)
        {
            query = query.Where(x => x.OwnerId == ownerId.Value);
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var pattern = $"%{keyword.Trim()}%";
            query = query.Where(x =>
                EF.Functions.ILike(x.Title, pattern)
                || EF.Functions.ILike(x.Description, pattern));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<int> CountByOwnerAsync(Guid ownerId, CancellationToken cancellationToken)
    {
        return dbContext.Courses.CountAsync(x => x.OwnerId == ownerId, cancellationToken);
    }

    public Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken)
    {
        return dbContext.Users.AnyAsync(x => x.Id == userId && x.IsActive, cancellationToken);
    }

    public Task<CourseEnrollment?> GetEnrollmentAsync(
        Guid courseId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return dbContext.CourseEnrollments
            .SingleOrDefaultAsync(
                x => x.CourseId == courseId && x.UserId == userId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<CourseEnrollment>> GetStudentsAsync(
        Guid courseId,
        CancellationToken cancellationToken)
    {
        return await dbContext.CourseEnrollments
            .AsNoTracking()
            .Include(x => x.User)
            .Where(x => x.CourseId == courseId)
            .OrderBy(x => x.User.FullName)
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(Course course, CancellationToken cancellationToken)
    {
        return dbContext.Courses.AddAsync(course, cancellationToken).AsTask();
    }

    public Task AddEnrollmentAsync(
        CourseEnrollment enrollment,
        CancellationToken cancellationToken)
    {
        return dbContext.CourseEnrollments.AddAsync(enrollment, cancellationToken).AsTask();
    }

    public void Remove(Course course)
    {
        dbContext.Courses.Remove(course);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
