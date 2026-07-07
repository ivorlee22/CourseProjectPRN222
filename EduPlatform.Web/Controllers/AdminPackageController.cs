using EduPlatform.BLL.DTOs.Packages;
using EduPlatform.BLL.Exceptions;
using EduPlatform.BLL.Interfaces;
using EduPlatform.Web.ViewModels.AdminPackage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduPlatform.Web.Controllers;

[Authorize(Roles = "Admin")]
public sealed class AdminPackageController(IPackageService packageService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var packages = await packageService.GetAllPackagesAsync(cancellationToken);
        return View(packages);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreatePackageViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreatePackageViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var command = new CreatePackageCommand(
                model.Name,
                model.Description,
                model.Price,
                model.MaxCourses,
                model.DailyChats,
                model.DurationDays,
                model.IsActive);

            await packageService.CreatePackageAsync(command, cancellationToken);
            TempData["SuccessMessage"] = $"Đã tạo gói {model.Name} thành công.";
            return RedirectToAction(nameof(Index));
        }
        catch (BusinessValidationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var package = await packageService.GetByIdAsync(id, cancellationToken);
            var model = new EditPackageViewModel
            {
                Id = package.Id,
                Name = package.Name,
                Description = package.Description,
                Price = package.Price,
                MaxCourses = package.MaxCourses,
                DailyChats = package.DailyChats,
                DurationDays = package.DurationDays,
                IsActive = true // We need GetByIdAsync to return IsActive or we use GetAllPackagesAsync to find it.
            };
            
            // Temporary workaround since GetByIdAsync returns PackageDto without IsActive.
            // Let's get it from GetAllPackagesAsync
            var all = await packageService.GetAllPackagesAsync(cancellationToken);
            var target = all.FirstOrDefault(x => x.Id == id);
            if (target != null)
            {
                model.IsActive = target.IsActive;
            }

            return View(model);
        }
        catch (ResourceNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost]
    public async Task<IActionResult> Edit(EditPackageViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var command = new UpdatePackageCommand(
                model.Id,
                model.Name,
                model.Description,
                model.Price,
                model.MaxCourses,
                model.DailyChats,
                model.DurationDays);

            await packageService.UpdatePackageAsync(command, cancellationToken);
            
            // Update IsActive status separately if needed, or we just handle it via ToggleStatus.
            // Wait, the Edit view might not include IsActive, or if it does, we should update it. 
            // In our BLL, UpdatePackageAsync doesn't take IsActive. TogglePackageStatusAsync handles it.
            // We can call TogglePackageStatusAsync here.
            await packageService.TogglePackageStatusAsync(model.Id, model.IsActive, cancellationToken);

            TempData["SuccessMessage"] = "Cập nhật gói thành công.";
            return RedirectToAction(nameof(Index));
        }
        catch (ResourceNotFoundException)
        {
            return NotFound();
        }
        catch (BusinessValidationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpPost]
    public async Task<IActionResult> ToggleStatus(Guid id, bool isActive, CancellationToken cancellationToken)
    {
        try
        {
            await packageService.TogglePackageStatusAsync(id, isActive, cancellationToken);
            TempData["SuccessMessage"] = isActive ? "Đã hiện gói cước." : "Đã ẩn gói cước.";
        }
        catch (ResourceNotFoundException)
        {
            TempData["ErrorMessage"] = "Không tìm thấy gói cước.";
        }
        catch (BusinessValidationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
