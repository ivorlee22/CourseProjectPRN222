using EduPlatform.BLL.DTOs.Reports;
using EduPlatform.BLL.Enums;
using EduPlatform.BLL.Services;
using EduPlatform.DAL.Repositories;

namespace EduPlatform.Tests.Services;

[TestClass]
public sealed class ReportServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 9, 12, 0, 0, TimeSpan.Zero);
    private readonly FakeReportRepository _repository = new();
    private readonly ReportService _service;

    public ReportServiceTests()
    {
        _service = new ReportService(_repository, new FixedTimeProvider(Now));
    }

    [TestMethod]
    public async Task GetRevenueReportAsync_GroupsDailyRevenueIntoMonths()
    {
        _repository.RevenueDaily.Add(new ReportDateAmount(new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero), 100m));
        _repository.RevenueDaily.Add(new ReportDateAmount(new DateTimeOffset(2026, 7, 2, 0, 0, 0, TimeSpan.Zero), 200m));
        _repository.RevenueDaily.Add(new ReportDateAmount(new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero), 50m));
        _repository.RevenueByPackage.Add(new ReportCategoryAmount("Plus", 250m));
        _repository.RevenueByMethod.Add(new ReportCategoryAmount("VNPay", 350m));

        var result = await _service.GetRevenueReportAsync(
            new ReportDateRange(
                new DateTimeOffset(2026, 7, 1, 7, 0, 0, TimeSpan.FromHours(7)),
                new DateTimeOffset(2026, 9, 1, 7, 0, 0, TimeSpan.FromHours(7))),
            ReportPeriodGrouping.Month,
            CancellationToken.None);

        Assert.AreEqual(TimeSpan.Zero, result.Range.StartUtc.Offset);
        Assert.AreEqual(350m, result.TotalRevenue);
        Assert.HasCount(2, result.RevenueByPeriod);
        Assert.AreEqual("07/2026", result.RevenueByPeriod[0].Label);
        Assert.AreEqual(300m, result.RevenueByPeriod[0].Value);
        Assert.AreEqual("08/2026", result.RevenueByPeriod[1].Label);
        Assert.AreEqual(50m, result.RevenueByPeriod[1].Value);
        Assert.HasCount(1, result.RevenueByPackage);
        Assert.HasCount(1, result.RevenueByPaymentMethod);
    }

    [TestMethod]
    public async Task GetAdminDashboardAsync_CombinesRequiredSnapshots()
    {
        _repository.Overview = new AdminOverviewSnapshot(20, 4, 999m, 3);
        _repository.CourseStats = new CourseStatsSnapshot(4, 3, 2, 2, 8, 5, 4, 6, 10);
        _repository.UserGrowthDaily.Add(new ReportDateCount(new DateTimeOffset(2026, 7, 8, 0, 0, 0, TimeSpan.Zero), 2));
        _repository.ChatUsageDaily.Add(new ReportDateCount(new DateTimeOffset(2026, 7, 8, 0, 0, 0, TimeSpan.Zero), 7));
        _repository.TopCourses.Add(new TopCourseSnapshot(Guid.NewGuid(), "Course A", "Teacher", 3, 2, 9));
        _repository.SubscriptionDistribution.Add(new ReportCategoryCount("Plus", 2));

        var result = await _service.GetAdminDashboardAsync(
            new ReportDateRange(Now.AddDays(-7), Now.AddDays(1)),
            ReportPeriodGrouping.Day,
            5,
            CancellationToken.None);

        Assert.AreEqual(20, result.TotalUsers);
        Assert.AreEqual(4, result.TotalCourses);
        Assert.AreEqual(999m, result.TotalRevenue);
        Assert.AreEqual(3, result.ActiveSubscriptions);
        Assert.AreEqual(10, result.CourseStats.TotalChatMessages);
        Assert.HasCount(1, result.UserGrowth);
        Assert.HasCount(1, result.ChatUsage);
        Assert.HasCount(1, result.TopCourses);
        Assert.HasCount(1, result.SubscriptionDistribution);
    }

    [TestMethod]
    public async Task GetTeacherStatisticsAsync_SumsCourseStats()
    {
        var teacherId = Guid.NewGuid();
        _repository.TeacherCourses.Add(new TeacherCourseStatsSnapshot(Guid.NewGuid(), "A", 2, 1, 1, 3, 4));
        _repository.TeacherCourses.Add(new TeacherCourseStatsSnapshot(Guid.NewGuid(), "B", 5, 2, 1, 1, 6));

        var result = await _service.GetTeacherStatisticsAsync(teacherId, CancellationToken.None);

        Assert.AreEqual(teacherId, result.TeacherId);
        Assert.AreEqual(2, result.TotalCourses);
        Assert.AreEqual(7, result.TotalEnrolledStudents);
        Assert.AreEqual(3, result.TotalDocuments);
        Assert.AreEqual(2, result.TotalReadyDocuments);
        Assert.AreEqual(4, result.TotalChatSessions);
        Assert.AreEqual(10, result.TotalChatMessages);
    }

    [TestMethod]
    public async Task GetStudentUsageAsync_UsesFreePackageLimitWhenNoActiveSubscription()
    {
        var studentId = Guid.NewGuid();
        _repository.StudentUsage = new StudentUsageSnapshot(
            2,
            3,
            9,
            4,
            null,
            new PackageLimitSnapshot(Guid.NewGuid(), "Free", 2, 10));

        var result = await _service.GetStudentUsageAsync(studentId, CancellationToken.None);

        Assert.AreEqual(studentId, result.StudentId);
        Assert.AreEqual(10, result.DailyChatLimit);
        Assert.AreEqual(4, result.ChatsUsedToday);
        Assert.AreEqual(6, result.ChatsRemainingToday);
        Assert.IsNull(result.ActiveSubscription);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class FakeReportRepository : IReportRepository
    {
        public AdminOverviewSnapshot Overview { get; set; } = new(0, 0, 0m, 0);
        public CourseStatsSnapshot CourseStats { get; set; } = new(0, 0, 0, 0, 0, 0, 0, 0, 0);
        public List<ReportDateAmount> RevenueDaily { get; } = [];
        public List<ReportCategoryAmount> RevenueByPackage { get; } = [];
        public List<ReportCategoryAmount> RevenueByMethod { get; } = [];
        public List<ReportDateCount> UserGrowthDaily { get; } = [];
        public List<ReportCategoryCount> UsersByRole { get; } = [];
        public List<ReportDateCount> ChatUsageDaily { get; } = [];
        public List<ReportCategoryCount> SubscriptionDistribution { get; } = [];
        public List<TopCourseSnapshot> TopCourses { get; } = [];
        public List<TeacherCourseStatsSnapshot> TeacherCourses { get; } = [];
        public StudentUsageSnapshot StudentUsage { get; set; } = new(0, 0, 0, 0, null, null);

        public Task<AdminOverviewSnapshot> GetAdminOverviewAsync(DateTimeOffset nowUtc, CancellationToken cancellationToken)
            => Task.FromResult(Overview);

        public Task<CourseStatsSnapshot> GetCourseStatsAsync(CancellationToken cancellationToken)
            => Task.FromResult(CourseStats);

        public Task<IReadOnlyList<ReportDateAmount>> GetSucceededRevenueDailyAsync(DateTimeOffset startUtc, DateTimeOffset endUtc, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<ReportDateAmount>>(RevenueDaily);

        public Task<IReadOnlyList<ReportCategoryAmount>> GetSucceededRevenueByPackageAsync(DateTimeOffset startUtc, DateTimeOffset endUtc, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<ReportCategoryAmount>>(RevenueByPackage);

        public Task<IReadOnlyList<ReportCategoryAmount>> GetSucceededRevenueByPaymentMethodAsync(DateTimeOffset startUtc, DateTimeOffset endUtc, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<ReportCategoryAmount>>(RevenueByMethod);

        public Task<IReadOnlyList<ReportDateCount>> GetUserGrowthDailyAsync(DateTimeOffset startUtc, DateTimeOffset endUtc, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<ReportDateCount>>(UserGrowthDaily);

        public Task<IReadOnlyList<ReportCategoryCount>> GetUsersByRoleAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<ReportCategoryCount>>(UsersByRole);

        public Task<IReadOnlyList<ReportDateCount>> GetChatUsageDailyAsync(DateTimeOffset startUtc, DateTimeOffset endUtc, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<ReportDateCount>>(ChatUsageDaily);

        public Task<IReadOnlyList<ReportCategoryCount>> GetSubscriptionDistributionAsync(DateTimeOffset nowUtc, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<ReportCategoryCount>>(SubscriptionDistribution);

        public Task<IReadOnlyList<TopCourseSnapshot>> GetTopCoursesAsync(int limit, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<TopCourseSnapshot>>(TopCourses.Take(limit).ToArray());

        public Task<IReadOnlyList<TeacherCourseStatsSnapshot>> GetTeacherCourseStatsAsync(Guid teacherId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<TeacherCourseStatsSnapshot>>(TeacherCourses);

        public Task<StudentUsageSnapshot> GetStudentUsageAsync(Guid studentId, DateTimeOffset startOfDayUtc, DateTimeOffset nowUtc, CancellationToken cancellationToken)
            => Task.FromResult(StudentUsage);
    }
}
