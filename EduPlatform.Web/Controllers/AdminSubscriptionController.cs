using EduPlatform.BLL.Interfaces;
using EduPlatform.Web.ViewModels.AdminSubscription;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduPlatform.Web.Controllers;

[Authorize(Roles = "Admin")]
public sealed class AdminSubscriptionController(ISubscriptionService subscriptionService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await subscriptionService.GetAllSubscriptionsPagedAsync(page, pageSize, cancellationToken);
        var viewModel = new AdminSubscriptionListViewModel { Subscriptions = result };
        return View(viewModel);
    }
}
