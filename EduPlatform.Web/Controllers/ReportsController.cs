using System.Globalization;
using EduPlatform.BLL.DTOs.Reports;
using EduPlatform.BLL.Enums;
using EduPlatform.BLL.Interfaces;
using EduPlatform.Web.ViewModels.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;

namespace EduPlatform.Web.Controllers;

[Authorize(Roles = "Admin")]
public sealed class ReportsController(IReportService reportService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Revenue(
        DateOnly? startDate,
        DateOnly? endDate,
        ReportPeriodGrouping grouping = ReportPeriodGrouping.Day,
        CancellationToken cancellationToken = default)
    {
        var filter = BuildFilter(startDate, endDate, grouping);
        var report = await reportService.GetRevenueReportAsync(
            filter.Range,
            filter.Grouping,
            cancellationToken);

        return View(new RevenueReportViewModel
        {
            Report = report,
            StartDate = filter.StartDate,
            EndDate = filter.EndDate,
            Grouping = filter.Grouping
        });
    }

    [HttpGet]
    public async Task<IActionResult> ExportRevenue(
        DateOnly? startDate,
        DateOnly? endDate,
        ReportPeriodGrouping grouping = ReportPeriodGrouping.Day,
        CancellationToken cancellationToken = default)
    {
        var filter = BuildFilter(startDate, endDate, grouping);
        var report = await reportService.GetRevenueReportAsync(
            filter.Range,
            filter.Grouping,
            cancellationToken);

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using var package = new ExcelPackage();
        AddSummarySheet(package, report, filter);
        AddPeriodSheet(package, report);
        AddCategorySheet(package, "Theo goi", "Gói cước", report.RevenueByPackage);
        AddCategorySheet(package, "Theo phuong thuc", "Phương thức", report.RevenueByPaymentMethod);

        var bytes = await package.GetAsByteArrayAsync(cancellationToken);
        var fileName = $"revenue-report-{filter.StartDate:yyyyMMdd}-{filter.EndDate:yyyyMMdd}.xlsx";
        return File(
            bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    private static RevenueFilter BuildFilter(
        DateOnly? startDate,
        DateOnly? endDate,
        ReportPeriodGrouping grouping)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var start = startDate ?? today.AddDays(-29);
        var end = endDate ?? today;

        if (end < start)
        {
            (start, end) = (end, start);
        }

        if (!Enum.IsDefined(grouping))
        {
            grouping = ReportPeriodGrouping.Day;
        }

        var startUtc = new DateTimeOffset(start.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var endUtc = new DateTimeOffset(end.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        return new RevenueFilter(
            start,
            end,
            grouping,
            new ReportDateRange(startUtc, endUtc));
    }

    private static void AddSummarySheet(ExcelPackage package, RevenueReportDto report, RevenueFilter filter)
    {
        var culture = CultureInfo.GetCultureInfo("vi-VN");
        var sheet = package.Workbook.Worksheets.Add("Tong quan");
        sheet.Cells[1, 1].Value = "Báo cáo doanh thu";
        sheet.Cells[1, 1, 1, 2].Merge = true;
        sheet.Cells[1, 1].Style.Font.Bold = true;
        sheet.Cells[1, 1].Style.Font.Size = 16;

        sheet.Cells[3, 1].Value = "Từ ngày";
        sheet.Cells[3, 2].Value = filter.StartDate.ToString("dd/MM/yyyy", culture);
        sheet.Cells[4, 1].Value = "Đến ngày";
        sheet.Cells[4, 2].Value = filter.EndDate.ToString("dd/MM/yyyy", culture);
        sheet.Cells[5, 1].Value = "Nhóm theo";
        sheet.Cells[5, 2].Value = filter.Grouping.ToString();
        sheet.Cells[6, 1].Value = "Tổng doanh thu";
        sheet.Cells[6, 2].Value = report.TotalRevenue;
        sheet.Cells[6, 2].Style.Numberformat.Format = "#,##0";
        sheet.Cells[3, 1, 6, 1].Style.Font.Bold = true;
        sheet.Cells.AutoFitColumns();
    }

    private static void AddPeriodSheet(ExcelPackage package, RevenueReportDto report)
    {
        var sheet = package.Workbook.Worksheets.Add("Theo thoi gian");
        sheet.Cells[1, 1].Value = "Kỳ";
        sheet.Cells[1, 2].Value = "Doanh thu";
        sheet.Cells[1, 1, 1, 2].Style.Font.Bold = true;

        for (var i = 0; i < report.RevenueByPeriod.Count; i++)
        {
            var row = i + 2;
            sheet.Cells[row, 1].Value = report.RevenueByPeriod[i].Label;
            sheet.Cells[row, 2].Value = report.RevenueByPeriod[i].Value;
            sheet.Cells[row, 2].Style.Numberformat.Format = "#,##0";
        }

        sheet.Cells.AutoFitColumns();
    }

    private static void AddCategorySheet(
        ExcelPackage package,
        string sheetName,
        string categoryHeader,
        IReadOnlyList<ReportCategoryValueDto> rows)
    {
        var sheet = package.Workbook.Worksheets.Add(sheetName);
        sheet.Cells[1, 1].Value = categoryHeader;
        sheet.Cells[1, 2].Value = "Doanh thu";
        sheet.Cells[1, 1, 1, 2].Style.Font.Bold = true;

        for (var i = 0; i < rows.Count; i++)
        {
            var row = i + 2;
            sheet.Cells[row, 1].Value = rows[i].Category;
            sheet.Cells[row, 2].Value = rows[i].Value;
            sheet.Cells[row, 2].Style.Numberformat.Format = "#,##0";
        }

        sheet.Cells.AutoFitColumns();
    }

    private sealed record RevenueFilter(
        DateOnly StartDate,
        DateOnly EndDate,
        ReportPeriodGrouping Grouping,
        ReportDateRange Range);
}
