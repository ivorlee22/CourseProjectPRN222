using EduPlatform.BLL.DTOs.Subscriptions;
using EduPlatform.BLL.Exceptions;
using EduPlatform.BLL.Interfaces;
using EduPlatform.Web.Security;
using EduPlatform.Web.ViewModels.Subscriptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduPlatform.Web.Controllers;

[Authorize(Roles = "Student")]
public sealed class SubscriptionController(ISubscriptionService subscriptionService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var actor = User.GetRequiredActor();
        var currentSubscription = await subscriptionService.GetActiveSubscriptionAsync(
            actor.UserId,
            cancellationToken);
        var subscriptions = await subscriptionService.GetUserSubscriptionsAsync(
            actor.UserId,
            cancellationToken);

        var model = new SubscriptionManagementViewModel(
            currentSubscription,
            subscriptions.Select(MapHistoryItem).ToArray());

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Renew(Guid packageId, CancellationToken cancellationToken)
    {
        var actor = User.GetRequiredActor();

        try
        {
            await subscriptionService.CreateSubscriptionAsync(
                new CreateSubscriptionCommand(actor.UserId, packageId),
                cancellationToken);
            TempData["SuccessMessage"] =
                "Đã tạo yêu cầu gia hạn. Thanh toán sẽ được xử lý khi cổng thanh toán sẵn sàng.";
        }
        catch (ResourceNotFoundException exception)
        {
            TempData["ErrorMessage"] = exception.Message;
        }
        catch (BusinessValidationException exception)
        {
            TempData["ErrorMessage"] = exception.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid subscriptionId, CancellationToken cancellationToken)
    {
        var actor = User.GetRequiredActor();

        try
        {
            await subscriptionService.CancelSubscriptionAsync(
                subscriptionId,
                actor.UserId,
                cancellationToken);
            TempData["SuccessMessage"] = "Đã hủy gói đăng ký.";
        }
        catch (ResourceNotFoundException exception)
        {
            TempData["ErrorMessage"] = exception.Message;
        }
        catch (ForbiddenOperationException exception)
        {
            TempData["ErrorMessage"] = exception.Message;
        }
        catch (BusinessValidationException exception)
        {
            TempData["ErrorMessage"] = exception.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    private static SubscriptionHistoryItemViewModel MapHistoryItem(SubscriptionSummaryDto subscription)
    {
        var normalizedStatus = subscription.Status.Trim();
        var isActive = string.Equals(normalizedStatus, "Active", StringComparison.OrdinalIgnoreCase);
        var isPending = string.Equals(normalizedStatus, "Pending", StringComparison.OrdinalIgnoreCase);
        var isCancelled = string.Equals(normalizedStatus, "Cancelled", StringComparison.OrdinalIgnoreCase);

        return new SubscriptionHistoryItemViewModel(
            subscription,
            GetStatusLabel(normalizedStatus),
            GetStatusBadgeClass(isActive, isPending, isCancelled),
            isActive || isPending,
            true);
    }

    private static string GetStatusLabel(string status)
    {
        return status.ToUpperInvariant() switch
        {
            "ACTIVE" => "Đang hiệu lực",
            "PENDING" => "Chờ thanh toán",
            "CANCELLED" => "Đã hủy",
            "EXPIRED" => "Hết hạn",
            _ => status
        };
    }

    private static string GetStatusBadgeClass(bool isActive, bool isPending, bool isCancelled)
    {
        if (isActive)
        {
            return "text-bg-success";
        }

        if (isPending)
        {
            return "text-bg-warning";
        }

        if (isCancelled)
        {
            return "text-bg-secondary";
        }

        return "text-bg-dark";
    }
}
