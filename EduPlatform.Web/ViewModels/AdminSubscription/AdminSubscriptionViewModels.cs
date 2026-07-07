using EduPlatform.BLL.DTOs.Subscriptions;
using EduPlatform.BLL.Models;

namespace EduPlatform.Web.ViewModels.AdminSubscription;

public class AdminSubscriptionListViewModel
{
    public PagedResult<SubscriptionAdminDto> Subscriptions { get; set; } = null!;
}
