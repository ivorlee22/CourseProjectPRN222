using EduPlatform.DAL.Entities;
using EduPlatform.DAL.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EduPlatform.DAL.Repositories;

public sealed class ReportRepository(AppDbContext dbContext) : IReportRepository
{
    public async Task<AdminOverviewSnapshot> GetAdminOverviewAsync(
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        var totalRevenue = await SucceededPayments()
            .SumAsync(payment => (decimal?)payment.Amount, cancellationToken) ?? 0m;

        var activeSubscriptions = await ActiveSubscriptions(nowUtc)
            .CountAsync(cancellationToken);

        return new AdminOverviewSnapshot(
            await dbContext.Users.AsNoTracking().CountAsync(cancellationToken),
            await dbContext.Courses.AsNoTracking().CountAsync(cancellationToken),
            totalRevenue,
            activeSubscriptions);
    }

    public async Task<CourseStatsSnapshot> GetCourseStatsAsync(CancellationToken cancellationToken)
    {
        return new CourseStatsSnapshot(
            await dbContext.Courses.AsNoTracking().CountAsync(cancellationToken),
            await dbContext.Courses.AsNoTracking().CountAsync(course => course.IsVisible, cancellationToken),
            await dbContext.Courses.AsNoTracking().CountAsync(course => course.Type == CourseType.Public, cancellationToken),
            await dbContext.Courses.AsNoTracking().CountAsync(course => course.Type == CourseType.Private, cancellationToken),
            await dbContext.CourseEnrollments.AsNoTracking().CountAsync(enrollment => enrollment.Status == EnrollmentStatus.Active, cancellationToken),
            await dbContext.Documents.AsNoTracking().CountAsync(cancellationToken),
            await dbContext.Documents.AsNoTracking().CountAsync(document => document.Status == DocumentStatus.Ready, cancellationToken),
            await dbContext.ChatSessions.AsNoTracking().CountAsync(cancellationToken),
            await dbContext.Messages.AsNoTracking().CountAsync(message => message.Role == MessageRole.User, cancellationToken));
    }

    public async Task<IReadOnlyList<ReportDateAmount>> GetSucceededRevenueDailyAsync(
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        CancellationToken cancellationToken)
    {
        var payments = await SucceededPaymentsInRange(startUtc, endUtc)
            .Select(payment => new
            {
                payment.Amount,
                ProcessedAtUtc = payment.ProcessedAtUtc!.Value
            })
            .ToListAsync(cancellationToken);

        return payments
            .GroupBy(payment => StartOfUtcDay(payment.ProcessedAtUtc))
            .OrderBy(group => group.Key)
            .Select(group => new ReportDateAmount(group.Key, group.Sum(payment => payment.Amount)))
            .ToArray();
    }

    public async Task<IReadOnlyList<ReportCategoryAmount>> GetSucceededRevenueByPackageAsync(
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        CancellationToken cancellationToken)
    {
        return await SucceededPaymentsInRange(startUtc, endUtc)
            .GroupBy(payment => payment.Package.Name)
            .Select(group => new ReportCategoryAmount(group.Key, group.Sum(payment => payment.Amount)))
            .OrderByDescending(item => item.Amount)
            .ThenBy(item => item.Category)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ReportCategoryAmount>> GetSucceededRevenueByPaymentMethodAsync(
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        CancellationToken cancellationToken)
    {
        return await SucceededPaymentsInRange(startUtc, endUtc)
            .GroupBy(payment => payment.Method)
            .Select(group => new ReportCategoryAmount(group.Key.ToString(), group.Sum(payment => payment.Amount)))
            .OrderByDescending(item => item.Amount)
            .ThenBy(item => item.Category)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ReportDateCount>> GetUserGrowthDailyAsync(
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        CancellationToken cancellationToken)
    {
        var users = await dbContext.Users
            .AsNoTracking()
            .Where(user => user.CreatedAtUtc >= startUtc && user.CreatedAtUtc < endUtc)
            .Select(user => user.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return users
            .GroupBy(StartOfUtcDay)
            .OrderBy(group => group.Key)
            .Select(group => new ReportDateCount(group.Key, group.Count()))
            .ToArray();
    }

    public async Task<IReadOnlyList<ReportCategoryCount>> GetUsersByRoleAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Users
            .AsNoTracking()
            .GroupBy(user => user.Role)
            .Select(group => new ReportCategoryCount(group.Key.ToString(), group.Count()))
            .OrderBy(item => item.Category)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ReportDateCount>> GetChatUsageDailyAsync(
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        CancellationToken cancellationToken)
    {
        var messages = await dbContext.Messages
            .AsNoTracking()
            .Where(message =>
                message.Role == MessageRole.User
                && message.CreatedAtUtc >= startUtc
                && message.CreatedAtUtc < endUtc)
            .Select(message => message.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return messages
            .GroupBy(StartOfUtcDay)
            .OrderBy(group => group.Key)
            .Select(group => new ReportDateCount(group.Key, group.Count()))
            .ToArray();
    }

    public async Task<IReadOnlyList<ReportCategoryCount>> GetSubscriptionDistributionAsync(
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        return await ActiveSubscriptions(nowUtc)
            .GroupBy(subscription => subscription.Package.Name)
            .Select(group => new ReportCategoryCount(group.Key, group.Count()))
            .OrderByDescending(item => item.Count)
            .ThenBy(item => item.Category)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TopCourseSnapshot>> GetTopCoursesAsync(
        int limit,
        CancellationToken cancellationToken)
    {
        return await dbContext.Courses
            .AsNoTracking()
            .Select(course => new TopCourseSnapshot(
                course.Id,
                course.Title,
                course.Owner.FullName,
                course.Enrollments.Count(enrollment => enrollment.Status == EnrollmentStatus.Active),
                course.Documents.Count,
                course.ChatSessions
                    .SelectMany(session => session.Messages)
                    .Count(message => message.Role == MessageRole.User)))
            .OrderByDescending(course => course.ChatMessageCount)
            .ThenByDescending(course => course.EnrollmentCount)
            .ThenBy(course => course.Title)
            .Take(Math.Max(limit, 1))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TeacherCourseStatsSnapshot>> GetTeacherCourseStatsAsync(
        Guid teacherId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Courses
            .AsNoTracking()
            .Where(course => course.OwnerId == teacherId)
            .OrderBy(course => course.Title)
            .Select(course => new TeacherCourseStatsSnapshot(
                course.Id,
                course.Title,
                course.Enrollments.Count(enrollment => enrollment.Status == EnrollmentStatus.Active),
                course.Documents.Count,
                course.Documents.Count(document => document.Status == DocumentStatus.Ready),
                course.ChatSessions.Count,
                course.ChatSessions
                    .SelectMany(session => session.Messages)
                    .Count(message => message.Role == MessageRole.User)))
            .ToListAsync(cancellationToken);
    }

    public async Task<StudentUsageSnapshot> GetStudentUsageAsync(
        Guid studentId,
        DateTimeOffset startOfDayUtc,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        var activeSubscription = await ActiveSubscriptions(nowUtc)
            .Where(subscription => subscription.UserId == studentId)
            .OrderByDescending(subscription => subscription.EndsAtUtc)
            .Select(subscription => new StudentSubscriptionSnapshot(
                subscription.Id,
                subscription.PackageId,
                subscription.Package.Name,
                subscription.Package.MaxCourses,
                subscription.Package.DailyChats,
                subscription.StartsAtUtc,
                subscription.EndsAtUtc))
            .FirstOrDefaultAsync(cancellationToken);

        var freePackage = activeSubscription is null
            ? await dbContext.Packages
                .AsNoTracking()
                .Where(package =>
                    package.IsActive
                    && package.Price == 0m
                    && package.Name == "Free")
                .Select(package => new PackageLimitSnapshot(
                    package.Id,
                    package.Name,
                    package.MaxCourses,
                    package.DailyChats))
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        var userMessages = dbContext.Messages
            .AsNoTracking()
            .Where(message =>
                message.Role == MessageRole.User
                && message.ChatSession.UserId == studentId);

        return new StudentUsageSnapshot(
            await dbContext.CourseEnrollments.AsNoTracking().CountAsync(
                enrollment => enrollment.UserId == studentId && enrollment.Status == EnrollmentStatus.Active,
                cancellationToken),
            await dbContext.ChatSessions.AsNoTracking().CountAsync(session => session.UserId == studentId, cancellationToken),
            await userMessages.CountAsync(cancellationToken),
            await userMessages.CountAsync(message => message.CreatedAtUtc >= startOfDayUtc, cancellationToken),
            activeSubscription,
            freePackage);
    }

    private IQueryable<Payment> SucceededPayments()
    {
        return dbContext.Payments
            .AsNoTracking()
            .Where(payment => payment.Status == PaymentStatus.Succeeded);
    }

    private IQueryable<Payment> SucceededPaymentsInRange(DateTimeOffset startUtc, DateTimeOffset endUtc)
    {
        return SucceededPayments()
            .Where(payment =>
                payment.ProcessedAtUtc.HasValue
                && payment.ProcessedAtUtc.Value >= startUtc
                && payment.ProcessedAtUtc.Value < endUtc);
    }

    private IQueryable<Subscription> ActiveSubscriptions(DateTimeOffset nowUtc)
    {
        return dbContext.Subscriptions
            .AsNoTracking()
            .Where(subscription =>
                subscription.Status == SubscriptionStatus.Active
                && subscription.StartsAtUtc <= nowUtc
                && subscription.EndsAtUtc > nowUtc);
    }

    private static DateTimeOffset StartOfUtcDay(DateTimeOffset value)
    {
        return new DateTimeOffset(value.UtcDateTime.Date, TimeSpan.Zero);
    }
}
