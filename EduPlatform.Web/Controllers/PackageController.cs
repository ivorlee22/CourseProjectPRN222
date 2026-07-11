using EduPlatform.BLL.DTOs.Packages;
using EduPlatform.BLL.DTOs.Subscriptions;
using EduPlatform.BLL.Enums;
using EduPlatform.BLL.Interfaces;
using EduPlatform.DAL.Entities;
using EduPlatform.Web.Security;
using EduPlatform.Web.ViewModels.Packages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BllUserRole = EduPlatform.BLL.Enums.UserRole;

namespace EduPlatform.Web.Controllers;

public sealed class PackageController(
    IPackageService packageService,
    ISubscriptionService subscriptionService) : Controller
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var packages = await packageService.GetActivePackagesAsync(cancellationToken);
        var currentSubscription = await GetCurrentSubscriptionAsync(cancellationToken);
        var model = new PackagePricingViewModel(
            packages.Select(package => Map(package, currentSubscription?.PackageId)).ToArray(),
            currentSubscription);

        return View(model);
    }

    [AllowAnonymous]
    [HttpGet("/pricing")]
    [HttpGet("/bang-gia")]
    public IActionResult Pricing()
    {
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Student")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Buy(Guid packageId)
    {
        var actor = User.GetRequiredActor();
        if (actor.Role != BllUserRole.Student)
        {
            TempData["ErrorMessage"] = "Chỉ học viên mới có thể mua gói.";
            return RedirectToAction(nameof(Index));
        }

        return RedirectToAction(
            "Checkout",
            "Payment",
            new { packageId });
    }

    private async Task<SubscriptionSummaryDto?> GetCurrentSubscriptionAsync(
        CancellationToken cancellationToken)
    {
        var actor = User.GetActorOrDefault();
        if (actor is null || actor.Role != BllUserRole.Student)
        {
            return null;
        }

        return await subscriptionService.GetActiveSubscriptionAsync(
            actor.UserId,
            cancellationToken);
    }

    private static PackagePricingItemViewModel Map(PackageDto package, Guid? currentPackageId)
    {
        return new PackagePricingItemViewModel(
            package,
            currentPackageId == package.Id,
            IsFeatured(package.Name),
            GetTagline(package.Name),
            GetAccentLabel(package.Name),
            GetHighlights(package));
    }

    private static bool IsFeatured(string packageName)
    {
        return string.Equals(packageName, "Plus", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetTagline(string packageName)
    {
        return packageName switch
        {
            "Free" => "Bắt đầu học với tài liệu và hỏi đáp có trích dẫn.",
            "Plus" => "Mở rộng lớp học và tần suất hỏi đáp mỗi ngày.",
            "Pro" => "Dành cho người học và nhóm lớp cần nhiều không gian hơn.",
            "Max" => "Dung lượng lớn cho học tập chuyên sâu và nhiều khóa học.",
            _ => "Gói học tập trên EduPlatform."
        };
    }

    private static string GetAccentLabel(string packageName)
    {
        return packageName switch
        {
            "Free" => "Miễn phí",
            "Plus" => "Phổ biến",
            "Pro" => "Nâng cao",
            "Max" => "Tối đa",
            _ => "Gói"
        };
    }

    private static bool IsFreePackage(PackageDto package)
    {
        return package.Price <= 0;
    }

    private static IReadOnlyList<string> GetHighlights(PackageDto package)
    {
        var usageText = IsFreePackage(package)
            ? "Sử dụng không giới hạn thời gian"
            : $"{package.DurationDays} ngày sử dụng";

        return
        [
            $"{package.MaxCourses} khóa học",
            $"{package.DailyChats} lượt chat mỗi ngày",
            usageText
        ];
    }
}
