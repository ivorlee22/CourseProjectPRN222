using System.Security.Claims;
using EduPlatform.BLL.DTOs.Payments;
using EduPlatform.BLL.Interfaces;
using EduPlatform.DAL.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduPlatform.Web.Controllers;

[Authorize]
[Route("payment")]
public class PaymentController(IPaymentService paymentService) : Controller
{
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

        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

        try
        {
            var command = new CreatePaymentCommand(userId, packageId, method, clientIp);
            var response = await paymentService.CreatePaymentAsync(command, cancellationToken);
            return Redirect(response.Url);
        }
        catch (Exception ex)
        {
            return Content($"Error: {ex.GetType().Name} - {ex.Message}\n\nStack Trace:\n{ex.StackTrace}");
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

    [HttpGet("momo-return")]
    public async Task<IActionResult> MoMoReturn(CancellationToken cancellationToken)
    {
        var queryData = Request.Query.ToDictionary(k => k.Key, v => v.Value.ToString());
        var command = new PaymentCallbackCommand(PaymentMethod.MoMo, queryData);

        var isSuccess = await paymentService.ProcessCallbackAsync(command, cancellationToken);

        if (isSuccess)
        {
            TempData["SuccessMessage"] = "Thanh toán MoMo thành công. Gói cước của bạn đã được kích hoạt.";
        }
        else
        {
            TempData["ErrorMessage"] = "Thanh toán MoMo thất bại hoặc chữ ký không hợp lệ.";
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

    [AllowAnonymous]
    [HttpPost("momo-ipn")]
    public async Task<IActionResult> MoMoIpn(CancellationToken cancellationToken)
    {
        var queryData = Request.Query.ToDictionary(k => k.Key, v => v.Value.ToString());
        var command = new PaymentCallbackCommand(PaymentMethod.MoMo, queryData);

        var isSuccess = await paymentService.ProcessCallbackAsync(command, cancellationToken);

        if (isSuccess)
        {
            return NoContent();
        }

        return BadRequest();
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
    public async Task<IActionResult> Packages([FromServices] IPackageService packageService, CancellationToken cancellationToken)
    {
        var packages = await packageService.GetActivePackagesAsync(cancellationToken);
        return View(packages);
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
