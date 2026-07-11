using System.Security.Claims;
using EduPlatform.BLL.DTOs.Payments;
using EduPlatform.BLL.Exceptions;
using EduPlatform.BLL.Interfaces;
using EduPlatform.DAL.Entities;
using EduPlatform.Web.ViewModels.Packages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduPlatform.Web.Controllers;

[Authorize]
[Route("payment")]
public class PaymentController(
    IPaymentService paymentService,
    IPackageService packageService) : Controller
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

            return View(new PaymentCheckoutViewModel(package, false));
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
    public async Task<IActionResult> CreatePayment(Guid packageId, PaymentMethod method, CancellationToken cancellationToken)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return Unauthorized();
        }

        if (method != PaymentMethod.VNPay)
        {
            TempData["ErrorMessage"] =
                "Phương thức thanh toán chưa được hỗ trợ. Vui lòng chọn VNPay.";
            return RedirectToAction("Index", "Package");
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

            var command = new CreatePaymentCommand(userId, packageId, method, clientIp);
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
            TempData["ErrorMessage"] =
                $"Không thể tạo yêu cầu thanh toán: {ex.Message}";
            return RedirectToAction("Index", "Package");
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
            TempData["SuccessMessage"] = "Thanh toán VNPay thành công. Gói cước của bạn đã được kích hoạt.";
        }
        else
        {
            TempData["ErrorMessage"] = "Thanh toán VNPay thất bại hoặc chữ ký không hợp lệ.";
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

        return Ok(new { RspCode = "00", Message = "Confirm Success" });
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