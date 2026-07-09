namespace EduPlatform.DAL.Repositories;

public sealed record ReportDateAmount(DateTimeOffset DateUtc, decimal Amount);

public sealed record ReportDateCount(DateTimeOffset DateUtc, int Count);

public sealed record ReportCategoryAmount(string Category, decimal Amount);

public sealed record ReportCategoryCount(string Category, int Count);

public sealed record AdminOverviewSnapshot(
    int TotalUsers,
    int TotalCourses,
    decimal TotalRevenue,
    int ActiveSubscriptions);

public sealed record CourseStatsSnapshot(
    int TotalCourses,
    int VisibleCourses,
    int PublicCourses,
    int PrivateCourses,
    int TotalEnrollments,
    int TotalDocuments,
    int ReadyDocuments,
    int TotalChatSessions,
    int TotalChatMessages);

public sealed record TopCourseSnapshot(
    Guid CourseId,
    string Title,
    string OwnerName,
    int EnrollmentCount,
    int DocumentCount,
    int ChatMessageCount);

public sealed record TeacherCourseStatsSnapshot(
    Guid CourseId,
    string Title,
    int EnrolledStudents,
    int DocumentCount,
    int ReadyDocumentCount,
    int ChatSessionCount,
    int ChatMessageCount);

public sealed record StudentSubscriptionSnapshot(
    Guid SubscriptionId,
    Guid PackageId,
    string PackageName,
    int MaxCourses,
    int DailyChats,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc);

public sealed record PackageLimitSnapshot(
    Guid PackageId,
    string PackageName,
    int MaxCourses,
    int DailyChats);

public sealed record StudentUsageSnapshot(
    int EnrolledCourses,
    int ChatSessions,
    int ChatMessages,
    int ChatsUsedToday,
    StudentSubscriptionSnapshot? ActiveSubscription,
    PackageLimitSnapshot? FreePackage);

public interface IReportRepository
{
    Task<AdminOverviewSnapshot> GetAdminOverviewAsync(
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken);

    Task<CourseStatsSnapshot> GetCourseStatsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<ReportDateAmount>> GetSucceededRevenueDailyAsync(
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ReportCategoryAmount>> GetSucceededRevenueByPackageAsync(
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ReportCategoryAmount>> GetSucceededRevenueByPaymentMethodAsync(
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ReportDateCount>> GetUserGrowthDailyAsync(
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ReportCategoryCount>> GetUsersByRoleAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<ReportDateCount>> GetChatUsageDailyAsync(
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ReportCategoryCount>> GetSubscriptionDistributionAsync(
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<TopCourseSnapshot>> GetTopCoursesAsync(
        int limit,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<TeacherCourseStatsSnapshot>> GetTeacherCourseStatsAsync(
        Guid teacherId,
        CancellationToken cancellationToken);

    Task<StudentUsageSnapshot> GetStudentUsageAsync(
        Guid studentId,
        DateTimeOffset startOfDayUtc,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken);
}
