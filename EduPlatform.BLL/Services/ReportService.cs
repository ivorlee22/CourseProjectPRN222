using EduPlatform.BLL.DTOs.Reports;
using EduPlatform.BLL.Enums;
using EduPlatform.BLL.Interfaces;
using EduPlatform.DAL.Repositories;

namespace EduPlatform.BLL.Services;

public sealed class ReportService(
    IReportRepository reportRepository,
    TimeProvider timeProvider) : IReportService
{
    public async Task<AdminDashboardReportDto> GetAdminDashboardAsync(
        ReportDateRange range,
        ReportPeriodGrouping grouping,
        int topCourseLimit,
        CancellationToken cancellationToken)
    {
        var normalizedRange = NormalizeRange(range);
        var now = GetUtcNow();
        var overview = await reportRepository.GetAdminOverviewAsync(now, cancellationToken);
        var revenue = await reportRepository.GetSucceededRevenueDailyAsync(
            normalizedRange.StartUtc,
            normalizedRange.EndUtc,
            cancellationToken);
        var userGrowth = await reportRepository.GetUserGrowthDailyAsync(
            normalizedRange.StartUtc,
            normalizedRange.EndUtc,
            cancellationToken);
        var courseStats = await reportRepository.GetCourseStatsAsync(cancellationToken);
        var chatUsage = await reportRepository.GetChatUsageDailyAsync(
            normalizedRange.StartUtc,
            normalizedRange.EndUtc,
            cancellationToken);
        var topCourses = await reportRepository.GetTopCoursesAsync(topCourseLimit, cancellationToken);
        var subscriptionDistribution = await reportRepository.GetSubscriptionDistributionAsync(now, cancellationToken);

        return new AdminDashboardReportDto(
            overview.TotalUsers,
            overview.TotalCourses,
            overview.TotalRevenue,
            overview.ActiveSubscriptions,
            BucketAmounts(revenue, grouping),
            BucketCounts(userGrowth, grouping),
            MapCourseStats(courseStats),
            BucketCounts(chatUsage, grouping),
            topCourses.Select(MapTopCourse).ToArray(),
            subscriptionDistribution.Select(MapCategoryCount).ToArray());
    }

    public async Task<RevenueReportDto> GetRevenueReportAsync(
        ReportDateRange range,
        ReportPeriodGrouping grouping,
        CancellationToken cancellationToken)
    {
        var normalizedRange = NormalizeRange(range);
        var revenue = await reportRepository.GetSucceededRevenueDailyAsync(
            normalizedRange.StartUtc,
            normalizedRange.EndUtc,
            cancellationToken);
        var revenueByPackage = await reportRepository.GetSucceededRevenueByPackageAsync(
            normalizedRange.StartUtc,
            normalizedRange.EndUtc,
            cancellationToken);
        var revenueByPaymentMethod = await reportRepository.GetSucceededRevenueByPaymentMethodAsync(
            normalizedRange.StartUtc,
            normalizedRange.EndUtc,
            cancellationToken);

        return new RevenueReportDto(
            normalizedRange,
            grouping,
            revenue.Sum(item => item.Amount),
            BucketAmounts(revenue, grouping),
            revenueByPackage.Select(MapCategoryValue).ToArray(),
            revenueByPaymentMethod.Select(MapCategoryValue).ToArray());
    }

    public async Task<UserAnalyticsReportDto> GetUserAnalyticsAsync(
        ReportDateRange range,
        ReportPeriodGrouping grouping,
        CancellationToken cancellationToken)
    {
        var normalizedRange = NormalizeRange(range);
        var overview = await reportRepository.GetAdminOverviewAsync(GetUtcNow(), cancellationToken);
        var userGrowth = await reportRepository.GetUserGrowthDailyAsync(
            normalizedRange.StartUtc,
            normalizedRange.EndUtc,
            cancellationToken);
        var usersByRole = await reportRepository.GetUsersByRoleAsync(cancellationToken);
        var subscriptions = await reportRepository.GetSubscriptionDistributionAsync(GetUtcNow(), cancellationToken);

        return new UserAnalyticsReportDto(
            normalizedRange,
            overview.TotalUsers,
            userGrowth.Sum(item => item.Count),
            BucketCounts(userGrowth, grouping),
            usersByRole.Select(MapCategoryCount).ToArray(),
            subscriptions.Select(MapCategoryCount).ToArray());
    }

    public async Task<TeacherStatisticsReportDto> GetTeacherStatisticsAsync(
        Guid teacherId,
        CancellationToken cancellationToken)
    {
        var courses = await reportRepository.GetTeacherCourseStatsAsync(teacherId, cancellationToken);
        var courseDtos = courses.Select(course => new TeacherCourseStatsDto(
            course.CourseId,
            course.Title,
            course.EnrolledStudents,
            course.DocumentCount,
            course.ReadyDocumentCount,
            course.ChatSessionCount,
            course.ChatMessageCount)).ToArray();

        return new TeacherStatisticsReportDto(
            teacherId,
            courseDtos.Length,
            courseDtos.Sum(course => course.EnrolledStudents),
            courseDtos.Sum(course => course.DocumentCount),
            courseDtos.Sum(course => course.ReadyDocumentCount),
            courseDtos.Sum(course => course.ChatSessionCount),
            courseDtos.Sum(course => course.ChatMessageCount),
            courseDtos);
    }

    public async Task<StudentUsageReportDto> GetStudentUsageAsync(
        Guid studentId,
        CancellationToken cancellationToken)
    {
        var now = GetUtcNow();
        var startOfDay = new DateTimeOffset(now.UtcDateTime.Date, TimeSpan.Zero);
        var usage = await reportRepository.GetStudentUsageAsync(
            studentId,
            startOfDay,
            now,
            cancellationToken);
        var activeSubscription = usage.ActiveSubscription is null
            ? null
            : new StudentSubscriptionReportDto(
                usage.ActiveSubscription.SubscriptionId,
                usage.ActiveSubscription.PackageId,
                usage.ActiveSubscription.PackageName,
                usage.ActiveSubscription.MaxCourses,
                usage.ActiveSubscription.DailyChats,
                usage.ActiveSubscription.StartsAtUtc,
                usage.ActiveSubscription.EndsAtUtc);
        var dailyChatLimit = usage.ActiveSubscription?.DailyChats
            ?? usage.FreePackage?.DailyChats
            ?? 0;

        return new StudentUsageReportDto(
            studentId,
            usage.EnrolledCourses,
            usage.ChatSessions,
            usage.ChatMessages,
            usage.ChatsUsedToday,
            dailyChatLimit,
            Math.Max(dailyChatLimit - usage.ChatsUsedToday, 0),
            activeSubscription);
    }

    private DateTimeOffset GetUtcNow()
    {
        return timeProvider.GetUtcNow().ToUniversalTime();
    }

    private static ReportDateRange NormalizeRange(ReportDateRange range)
    {
        var start = ToUtc(range.StartUtc);
        var end = ToUtc(range.EndUtc);
        if (end <= start)
        {
            throw new ArgumentException("Report end date must be after start date.");
        }

        return new ReportDateRange(start, end);
    }

    private static DateTimeOffset ToUtc(DateTimeOffset value)
    {
        return value.ToUniversalTime();
    }

    private static ReportTimeSeriesPointDto[] BucketAmounts(
        IReadOnlyList<ReportDateAmount> points,
        ReportPeriodGrouping grouping)
    {
        return points
            .GroupBy(point => BucketStart(point.DateUtc, grouping))
            .OrderBy(group => group.Key)
            .Select(group => new ReportTimeSeriesPointDto(
                group.Key,
                FormatLabel(group.Key, grouping),
                group.Sum(point => point.Amount)))
            .ToArray();
    }

    private static ReportCountSeriesPointDto[] BucketCounts(
        IReadOnlyList<ReportDateCount> points,
        ReportPeriodGrouping grouping)
    {
        return points
            .GroupBy(point => BucketStart(point.DateUtc, grouping))
            .OrderBy(group => group.Key)
            .Select(group => new ReportCountSeriesPointDto(
                group.Key,
                FormatLabel(group.Key, grouping),
                group.Sum(point => point.Count)))
            .ToArray();
    }

    private static DateTimeOffset BucketStart(DateTimeOffset value, ReportPeriodGrouping grouping)
    {
        var utcDate = value.UtcDateTime.Date;
        return grouping switch
        {
            ReportPeriodGrouping.Day => new DateTimeOffset(utcDate, TimeSpan.Zero),
            ReportPeriodGrouping.Week => new DateTimeOffset(
                utcDate.AddDays(-(((int)utcDate.DayOfWeek + 6) % 7)),
                TimeSpan.Zero),
            ReportPeriodGrouping.Month => new DateTimeOffset(
                new DateTime(utcDate.Year, utcDate.Month, 1),
                TimeSpan.Zero),
            _ => throw new ArgumentOutOfRangeException(nameof(grouping), grouping, "Unsupported report grouping.")
        };
    }

    private static string FormatLabel(DateTimeOffset periodStart, ReportPeriodGrouping grouping)
    {
        return grouping switch
        {
            ReportPeriodGrouping.Day => periodStart.ToString("dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture),
            ReportPeriodGrouping.Week => $"Tuần {periodStart:dd/MM/yyyy}",
            ReportPeriodGrouping.Month => periodStart.ToString("MM/yyyy", System.Globalization.CultureInfo.InvariantCulture),
            _ => periodStart.ToString("dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture)
        };
    }

    private static CourseStatsReportDto MapCourseStats(CourseStatsSnapshot item)
    {
        return new CourseStatsReportDto(
            item.TotalCourses,
            item.VisibleCourses,
            item.PublicCourses,
            item.PrivateCourses,
            item.TotalEnrollments,
            item.TotalDocuments,
            item.ReadyDocuments,
            item.TotalChatSessions,
            item.TotalChatMessages);
    }

    private static TopCourseReportDto MapTopCourse(TopCourseSnapshot item)
    {
        return new TopCourseReportDto(
            item.CourseId,
            item.Title,
            item.OwnerName,
            item.EnrollmentCount,
            item.DocumentCount,
            item.ChatMessageCount);
    }

    private static ReportCategoryValueDto MapCategoryValue(ReportCategoryAmount item)
    {
        return new ReportCategoryValueDto(item.Category, item.Amount);
    }

    private static ReportCategoryCountDto MapCategoryCount(ReportCategoryCount item)
    {
        return new ReportCategoryCountDto(item.Category, item.Count);
    }
}
