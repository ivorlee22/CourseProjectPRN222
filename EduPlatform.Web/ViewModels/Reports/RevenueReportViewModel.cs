using EduPlatform.BLL.DTOs.Reports;
using EduPlatform.BLL.Enums;

namespace EduPlatform.Web.ViewModels.Reports;

public sealed class RevenueReportViewModel
{
    public RevenueReportDto Report { get; set; } = null!;

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public ReportPeriodGrouping Grouping { get; set; } = ReportPeriodGrouping.Day;
}
