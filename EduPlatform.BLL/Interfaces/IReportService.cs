using EduPlatform.BLL.DTOs.Reports;
using EduPlatform.BLL.Enums;

namespace EduPlatform.BLL.Interfaces;

public interface IReportService
{
    Task<AdminDashboardReportDto> GetAdminDashboardAsync(
        ReportDateRange range,
        ReportPeriodGrouping grouping,
        int topCourseLimit,
        CancellationToken cancellationToken);

    Task<RevenueReportDto> GetRevenueReportAsync(
        ReportDateRange range,
        ReportPeriodGrouping grouping,
        CancellationToken cancellationToken);

    Task<UserAnalyticsReportDto> GetUserAnalyticsAsync(
        ReportDateRange range,
        ReportPeriodGrouping grouping,
        CancellationToken cancellationToken);

    Task<TeacherStatisticsReportDto> GetTeacherStatisticsAsync(
        Guid teacherId,
        CancellationToken cancellationToken);

    Task<StudentUsageReportDto> GetStudentUsageAsync(
        Guid studentId,
        CancellationToken cancellationToken);
}
