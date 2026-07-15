using System.Security.Claims;
using EduPlatform.BLL.DTOs.Payments;
using EduPlatform.BLL.DTOs.Subscriptions;
using EduPlatform.BLL.Enums;
using EduPlatform.BLL.Exceptions;
using EduPlatform.BLL.Interfaces;
using EduPlatform.Web.ViewModels.Packages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduPlatform.Web.Controllers;

[Authorize]
[Route("payment")]
public class PaymentController(
    IPaymentService paymentService,
    IPackageService packageService,
    ISubscriptionService subscriptionService) : Controller
{
    [HttpGet("checkout/{packageId:guid}")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> Checkout(Guid packageId, CancellationToken cancellationToken)
    {
        try
        {
            var package = await packageService.GetByIdAsync(packageId, cancellationToken);
            if (package.Price <= 0)
            {
                TempData["ErrorMessage"] = "Gói miễn phí đang là mặc định, không cần thanh toán.";
                return RedirectToAction("Index", "Package");
            }

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            SubscriptionSummaryDto? currentSubscription = null;
            if (Guid.TryParse(userIdString, out var userId))
            {
                currentSubscription = await subscriptionService.GetActiveSubscriptionAsync(
                    userId,
                    cancellationToken);
            }

            return View(new PaymentCheckoutViewModel(
                package,
                currentSubscription?.PackageId == package.Id,
                currentSubscription));
        }
        catch (ResourceNotFoundException exception)
        {
            TempData["ErrorMessage"] = exception.Message;
            return RedirectToAction("Index", "Package");
        }
    }

    [HttpPost("create")]
    [Authorize(Roles = "Student")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePayment(Guid packageId, CancellationToken cancellationToken)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return Unauthorized();
        }

        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

        try
        {
            var package = await packageService.GetByIdAsync(packageId, cancellationToken);
            if (package.Price <= 0)
            {
                TempData["ErrorMessage"] = "Gói miễn phí không cần thanh toán.";
                return RedirectToAction("Index", "Package");
            }

            var command = new CreatePaymentCommand(userId, packageId, PaymentMethod.VNPay, clientIp);
            var response = await paymentService.CreatePaymentAsync(command, cancellationToken);
            return Redirect(response.Url);
        }
        catch (ResourceNotFoundException exception)
        {
            TempData["ErrorMessage"] = exception.Message;
            return RedirectToAction("Index", "Package");
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Không thể tạo thanh toán VNPay: {ex.Message}";
            return RedirectToAction(nameof(Checkout), new { packageId });
        }
    }

    [HttpGet("vnpay-return")]
    public async Task<IActionResult> VNPayReturn(CancellationToken cancellationToken)
    {
        var queryData = Request.Query.ToDictionary(k => k.Key, v => v.Value.ToString());
        var command = new PaymentCallbackCommand(PaymentMethod.VNPay, queryData);

        var isSuccess = await paymentService.ProcessCallbackAsync(command, cancellationToken);

        if (isSuccess)
        {
            TempData["SuccessMessage"] = "Thanh toán thành công. Gói của bạn đã được kích hoạt.";
        }
        else
        {
            TempData["ErrorMessage"] = "Thanh toán chưa thành công. Vui lòng thử lại hoặc chọn gói khác.";
        }

        return RedirectToAction(nameof(History));
    }

    [AllowAnonymous]
    [HttpGet("vnpay-ipn")]
    public async Task<IActionResult> VNPayIpn(CancellationToken cancellationToken)
    {
        var queryData = Request.Query.ToDictionary(k => k.Key, v => v.Value.ToString());
        var command = new PaymentCallbackCommand(PaymentMethod.VNPay, queryData);

        var isSuccess = await paymentService.ProcessCallbackAsync(command, cancellationToken);

        return Ok(isSuccess
            ? new { RspCode = "00", Message = "Confirm Success" }
            : new { RspCode = "97", Message = "Invalid signature or payment state" });
    }

    [HttpGet("history")]
    public async Task<IActionResult> History(CancellationToken cancellationToken)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return Unauthorized();
        }

        var payments = await paymentService.GetUserPaymentsAsync(userId, cancellationToken);
        return View(payments);
    }

    [AllowAnonymous]
    [HttpGet("packages")]
    public IActionResult Packages()
    {
        return RedirectToAction("Index", "Package");
    }

    [HttpGet("detail/{id:guid}")]
    public async Task<IActionResult> Detail(Guid id, CancellationToken cancellationToken)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return Unauthorized();
        }

        var payment = await paymentService.GetPaymentDetailAsync(id, userId, cancellationToken);
        if (payment == null)
        {
            return NotFound();
        }

        return View(payment);
    }
}
