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
        Guid? enrolledUserId,
        CancellationToken cancellationToken)
    {
        var baseQuery = dbContext.Courses.AsNoTracking();

        if (visibleOnly)
        {
            baseQuery = baseQuery.Where(x => x.IsVisible);
        }

        if (ownerId.HasValue)
        {
            baseQuery = baseQuery.Where(x => x.OwnerId == ownerId.Value);
        }

        if (enrolledUserId.HasValue)
        {
            // Filter courses where the user has an Active enrollment.
            // Uses FK via Enrollments collection — no nav property needed on enrollment.
            baseQuery = baseQuery.Where(x => x.Enrollments.Any(
                e => e.UserId == enrolledUserId.Value && e.Status == EnrollmentStatus.Active));
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var pattern = $"%{keyword.Trim()}%";
            baseQuery = baseQuery.Where(x =>
                EF.Functions.ILike(x.Title, pattern)
                || EF.Functions.ILike(x.Description, pattern));
        }

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var items = await baseQuery
            .Include(x => x.Owner)
            .Include(x => x.Enrollments)
            .AsSplitQuery()
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

    public async Task<IReadOnlyList<User>> FindActiveStudentsByEmailOrNameAsync(
        string lookup,
        CancellationToken cancellationToken)
    {
        var normalizedLookup = lookup.Trim().ToUpperInvariant();
        var nameLookup = lookup.Trim();

        return await dbContext.Users
            .AsNoTracking()
            .Where(x =>
                x.IsActive
                && x.Role == UserRole.Student
                && (x.NormalizedEmail == normalizedLookup || x.FullName == nameLookup))
            .OrderBy(x => x.Email)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PendingCourseInvitation>> GetPendingInvitationsAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        // Use a LINQ join to retrieve inviter name without a navigation property on
        // CourseEnrollment. InvitedById is a raw FK — we join Users directly.
        var results = await (
            from enrollment in dbContext.CourseEnrollments.AsNoTracking()
            join course in dbContext.Courses on enrollment.CourseId equals course.Id
            join inviter in dbContext.Users on enrollment.InvitedById equals inviter.Id into inviterGroup
            from inviter in inviterGroup.DefaultIfEmpty()
            where enrollment.UserId == userId
                && enrollment.Status == EnrollmentStatus.Pending
                && enrollment.InvitedById != null
            orderby course.CreatedAtUtc descending
            select new PendingCourseInvitation(
                enrollment.CourseId,
                course.Title,
                inviter != null ? inviter.FullName : "Quan tri vien")
        ).ToListAsync(cancellationToken);

        return results;
    }

    public Task<int> CountPendingInvitationsAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return dbContext.CourseEnrollments.CountAsync(
            x => x.UserId == userId
                 && x.Status == EnrollmentStatus.Pending
                 && x.InvitedById != null,
            cancellationToken);
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

    public void RemoveEnrollment(CourseEnrollment enrollment)
    {
        dbContext.CourseEnrollments.Remove(enrollment);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
