using System.Security.Claims;
using EduPlatform.BLL.DTOs.Subscriptions;
using EduPlatform.BLL.Interfaces;
using EduPlatform.Web.Controllers;
using EduPlatform.Web.ViewModels.Subscriptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace EduPlatform.Tests.Services;

[TestClass]
public sealed class SubscriptionControllerTests
{
    private static readonly Guid UserId = Guid.Parse("20000000-0000-0000-0000-000000000001");
    private static readonly Guid PackageId = Guid.Parse("20000000-0000-0000-0000-000000000002");
    private readonly FakeSubscriptionService _subscriptionService = new();

    [TestMethod]
    public async Task Index_StudentUser_ReturnsCurrentSubscriptionAndHistory()
    {
        var currentSubscription = CreateSubscription("Plus", "Active");
        _subscriptionService.ActiveSubscription = currentSubscription;
        _subscriptionService.Subscriptions.Add(currentSubscription);
        _subscriptionService.Subscriptions.Add(CreateSubscription("Free", "Cancelled"));
        using var controller = CreateController();

        var result = await controller.Index(CancellationToken.None);

        var view = Assert.IsInstanceOfType<ViewResult>(result);
        var model = Assert.IsInstanceOfType<SubscriptionManagementViewModel>(view.Model);
        Assert.AreEqual(currentSubscription.Id, model.CurrentSubscription?.Id);
        Assert.HasCount(2, model.History);
        Assert.AreEqual(1, _subscriptionService.GetActiveSubscriptionCallCount);
        Assert.AreEqual(1, _subscriptionService.GetUserSubscriptionsCallCount);
        Assert.AreEqual(UserId, _subscriptionService.LastUserId);
    }

    [TestMethod]
    public async Task Index_NoActiveSubscription_ReturnsHistoryOnly()
    {
        _subscriptionService.Subscriptions.Add(CreateSubscription("Free", "Expired"));
        using var controller = CreateController();

        var result = await controller.Index(CancellationToken.None);

        var view = Assert.IsInstanceOfType<ViewResult>(result);
        var model = Assert.IsInstanceOfType<SubscriptionManagementViewModel>(view.Model);
        Assert.IsNull(model.CurrentSubscription);
        Assert.HasCount(1, model.History);
        Assert.AreEqual("Hết hạn", model.History[0].StatusLabel);
    }

    [TestMethod]
    public async Task Renew_CreatesPendingSubscriptionForStudentPackage()
    {
        using var controller = CreateController();

        var result = await controller.Renew(PackageId, CancellationToken.None);

        var redirect = Assert.IsInstanceOfType<RedirectToActionResult>(result);
        Assert.AreEqual(nameof(SubscriptionController.Index), redirect.ActionName);
        Assert.AreEqual(1, _subscriptionService.CreateSubscriptionCallCount);
        Assert.AreEqual(UserId, _subscriptionService.LastCreateCommand?.UserId);
        Assert.AreEqual(PackageId, _subscriptionService.LastCreateCommand?.PackageId);
        Assert.IsTrue(controller.TempData.ContainsKey("SuccessMessage"));
    }

    [TestMethod]
    public async Task Cancel_CancelsOwnedSubscription()
    {
        var subscriptionId = Guid.Parse("20000000-0000-0000-0000-000000000003");
        using var controller = CreateController();

        var result = await controller.Cancel(subscriptionId, CancellationToken.None);

        var redirect = Assert.IsInstanceOfType<RedirectToActionResult>(result);
        Assert.AreEqual(nameof(SubscriptionController.Index), redirect.ActionName);
        Assert.AreEqual(1, _subscriptionService.CancelSubscriptionCallCount);
        Assert.AreEqual(subscriptionId, _subscriptionService.LastCancelledSubscriptionId);
        Assert.AreEqual(UserId, _subscriptionService.LastCancelledUserId);
        Assert.IsTrue(controller.TempData.ContainsKey("SuccessMessage"));
    }

    private SubscriptionController CreateController()
    {
        return new SubscriptionController(_subscriptionService)
        {
            ControllerContext = CreateControllerContext(),
            TempData = new TempDataDictionary(
                new DefaultHttpContext(),
                new FakeTempDataProvider())
        };
    }

    private static ControllerContext CreateControllerContext()
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, UserId.ToString()),
            new Claim(ClaimTypes.Role, "Student")
        ], "Test");

        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };
    }

    private static SubscriptionSummaryDto CreateSubscription(string packageName, string status)
    {
        return new SubscriptionSummaryDto(
            Guid.NewGuid(),
            PackageId,
            packageName,
            10,
            50,
            status,
            DateTimeOffset.UtcNow.AddDays(-10),
            DateTimeOffset.UtcNow.AddDays(20));
    }

    private sealed class FakeSubscriptionService : ISubscriptionService
    {
        public SubscriptionSummaryDto? ActiveSubscription { get; set; }
        public List<SubscriptionSummaryDto> Subscriptions { get; } = [];
        public int GetActiveSubscriptionCallCount { get; private set; }
        public int GetUserSubscriptionsCallCount { get; private set; }
        public int CreateSubscriptionCallCount { get; private set; }
        public int CancelSubscriptionCallCount { get; private set; }
        public Guid? LastUserId { get; private set; }
        public CreateSubscriptionCommand? LastCreateCommand { get; private set; }
        public Guid? LastCancelledSubscriptionId { get; private set; }
        public Guid? LastCancelledUserId { get; private set; }

        public Task<SubscriptionSummaryDto?> GetActiveSubscriptionAsync(
            Guid userId,
            CancellationToken cancellationToken)
        {
            GetActiveSubscriptionCallCount++;
            LastUserId = userId;
            return Task.FromResult(ActiveSubscription);
        }

        public Task<IReadOnlyList<SubscriptionSummaryDto>> GetUserSubscriptionsAsync(
            Guid userId,
            CancellationToken cancellationToken)
        {
            GetUserSubscriptionsCallCount++;
            LastUserId = userId;
            return Task.FromResult<IReadOnlyList<SubscriptionSummaryDto>>(Subscriptions);
        }

        public Task<Guid> CreateSubscriptionAsync(
            CreateSubscriptionCommand command,
            CancellationToken cancellationToken)
        {
            CreateSubscriptionCallCount++;
            LastCreateCommand = command;
            return Task.FromResult(Guid.NewGuid());
        }

        public Task CancelSubscriptionAsync(
            Guid subscriptionId,
            Guid userId,
            CancellationToken cancellationToken)
        {
            CancelSubscriptionCallCount++;
            LastCancelledSubscriptionId = subscriptionId;
            LastCancelledUserId = userId;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeTempDataProvider : ITempDataProvider
    {
        private readonly Dictionary<string, object> _values = [];

        public IDictionary<string, object> LoadTempData(HttpContext context)
        {
            return _values;
        }

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
            _values.Clear();
            foreach (var value in values)
            {
                _values[value.Key] = value.Value;
            }
        }
    }
}
