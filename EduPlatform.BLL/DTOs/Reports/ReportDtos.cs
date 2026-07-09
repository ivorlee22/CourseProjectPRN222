using EduPlatform.BLL.Enums;

namespace EduPlatform.BLL.DTOs.Reports;

public sealed record ReportDateRange(DateTimeOffset StartUtc, DateTimeOffset EndUtc);

public sealed record ReportTimeSeriesPointDto(
    DateTimeOffset PeriodStartUtc,
    string Label,
    decimal Value);

public sealed record ReportCountSeriesPointDto(
    DateTimeOffset PeriodStartUtc,
    string Label,
    int Count);

public sealed record ReportCategoryValueDto(
    string Category,
    decimal Value);

public sealed record ReportCategoryCountDto(
    string Category,
    int Count);

public sealed record CourseStatsReportDto(
    int TotalCourses,
    int VisibleCourses,
    int PublicCourses,
    int PrivateCourses,
    int TotalEnrollments,
    int TotalDocuments,
    int ReadyDocuments,
    int TotalChatSessions,
    int TotalChatMessages);

public sealed record TopCourseReportDto(
    Guid CourseId,
    string Title,
    string OwnerName,
    int EnrollmentCount,
    int DocumentCount,
    int ChatMessageCount);

public sealed record AdminDashboardReportDto(
    int TotalUsers,
    int TotalCourses,
    decimal TotalRevenue,
    int ActiveSubscriptions,
    IReadOnlyList<ReportTimeSeriesPointDto> Revenue,
    IReadOnlyList<ReportCountSeriesPointDto> UserGrowth,
    CourseStatsReportDto CourseStats,
    IReadOnlyList<ReportCountSeriesPointDto> ChatUsage,
    IReadOnlyList<TopCourseReportDto> TopCourses,
    IReadOnlyList<ReportCategoryCountDto> SubscriptionDistribution);

public sealed record RevenueReportDto(
    ReportDateRange Range,
    ReportPeriodGrouping Grouping,
    decimal TotalRevenue,
    IReadOnlyList<ReportTimeSeriesPointDto> RevenueByPeriod,
    IReadOnlyList<ReportCategoryValueDto> RevenueByPackage,
    IReadOnlyList<ReportCategoryValueDto> RevenueByPaymentMethod);

public sealed record UserAnalyticsReportDto(
    ReportDateRange Range,
    int TotalUsers,
    int NewUsers,
    IReadOnlyList<ReportCountSeriesPointDto> UserGrowth,
    IReadOnlyList<ReportCategoryCountDto> UsersByRole,
    IReadOnlyList<ReportCategoryCountDto> SubscriptionDistribution);

public sealed record TeacherCourseStatsDto(
    Guid CourseId,
    string Title,
    int EnrolledStudents,
    int DocumentCount,
    int ReadyDocumentCount,
    int ChatSessionCount,
    int ChatMessageCount);

public sealed record TeacherStatisticsReportDto(
    Guid TeacherId,
    int TotalCourses,
    int TotalEnrolledStudents,
    int TotalDocuments,
    int TotalReadyDocuments,
    int TotalChatSessions,
    int TotalChatMessages,
    IReadOnlyList<TeacherCourseStatsDto> Courses);

public sealed record StudentSubscriptionReportDto(
    Guid SubscriptionId,
    Guid PackageId,
    string PackageName,
    int MaxCourses,
    int DailyChats,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc);

public sealed record StudentUsageReportDto(
    Guid StudentId,
    int EnrolledCourses,
    int ChatSessions,
    int ChatMessages,
    int ChatsUsedToday,
    int DailyChatLimit,
    int ChatsRemainingToday,
    StudentSubscriptionReportDto? ActiveSubscription);
