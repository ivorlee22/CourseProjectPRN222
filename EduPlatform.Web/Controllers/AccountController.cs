using System.Security.Claims;
using EduPlatform.BLL.DTOs.Users;
using EduPlatform.BLL.Exceptions;
using EduPlatform.BLL.Interfaces;
using EduPlatform.Web.Security;
using EduPlatform.Web.ViewModels.Account;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduPlatform.Web.Controllers;

public sealed class AccountController(IUserService userService) : Controller
{
    // ── Login ─────────────────────────────────────────────────────────────────

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var redirect = RedirectToLocal(returnUrl);
            if (redirect is not null) return redirect;
            return RedirectToAction("Index", "Home");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Login(
        LoginViewModel model,
        string? returnUrl,
        CancellationToken cancellationToken)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var user = await userService.AuthenticateAsync(
                new LoginCommand(model.Email, model.Password),
                cancellationToken);

            var claims = new List<Claim>
            {
                // ClaimTypes.NameIdentifier → Guid string → đọc bởi ClaimsPrincipalExtensions
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.FullName),
                new(ClaimTypes.Email, user.Email),
                // ClaimTypes.Role → "Admin" / "Teacher" / "Student" → Enum.TryParse trong ClaimsPrincipalExtensions
                new(ClaimTypes.Role, user.Role.ToString())
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            var properties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = model.RememberMe
                    ? DateTimeOffset.UtcNow.AddDays(14)
                    : null
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                properties);

            var postLoginRedirect = RedirectToLocal(returnUrl);
            if (postLoginRedirect is not null) return postLoginRedirect;
            return RedirectToAction("Index", "Home");
        }
        catch (BusinessValidationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    // ── Logout ────────────────────────────────────────────────────────────────

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    // ── Register ──────────────────────────────────────────────────────────────

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        return View(new RegisterViewModel());
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Register(
        RegisterViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await userService.RegisterAsync(
                new RegisterCommand(model.FullName, model.Email, model.Password),
                cancellationToken);

            TempData["SuccessMessage"] = "Đăng ký thành công. Vui lòng đăng nhập.";
            return RedirectToAction(nameof(Login));
        }
        catch (ResourceConflictException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
        catch (BusinessValidationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    // ── Profile ───────────────────────────────────────────────────────────────

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Profile(CancellationToken cancellationToken)
    {
        var actor = User.GetRequiredActor();
        var user = await userService.GetByIdAsync(actor.UserId, cancellationToken);

        if (user is null)
        {
            // Tài khoản không còn tồn tại hoặc bị xóa — đăng xuất
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        return View(new ProfileViewModel
        {
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            IsActive = user.IsActive,
            CreatedAtUtc = user.CreatedAtUtc
        });
    }

    // ── Change Password ───────────────────────────────────────────────────────

    [Authorize]
    [HttpGet]
    public IActionResult ChangePassword()
    {
        return View(new ChangePasswordViewModel());
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> ChangePassword(
        ChangePasswordViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var actor = User.GetRequiredActor();

            await userService.ChangePasswordAsync(
                new ChangePasswordCommand(actor.UserId, model.CurrentPassword, model.NewPassword),
                cancellationToken);

            TempData["SuccessMessage"] = "Đổi mật khẩu thành công.";
            return RedirectToAction(nameof(Profile));
        }
        catch (BusinessValidationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private RedirectResult? RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return null;
    }
}
