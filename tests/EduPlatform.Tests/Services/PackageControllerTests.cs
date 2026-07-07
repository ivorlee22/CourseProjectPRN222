using System.Security.Claims;
using EduPlatform.BLL.DTOs.Packages;
using EduPlatform.BLL.DTOs.Subscriptions;
using EduPlatform.BLL.Interfaces;
using EduPlatform.Web.Controllers;
using EduPlatform.Web.ViewModels.Packages;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace EduPlatform.Tests.Services;

[TestClass]
public sealed class PackageControllerTests
{
    private static readonly Guid UserId = Guid.Parse("10000000-0000-0000-0000-000000000003");
    private readonly FakePackageService _packageService = new();
    private readonly FakeSubscriptionService _subscriptionService = new();

    [TestMethod]
    public async Task Index_AnonymousUser_ReturnsAllPackagesWithoutCurrentSubscription()
    {
        AddDefaultPackages();
        using var controller = CreateController();

        var result = await controller.Index(CancellationToken.None);

        var view = Assert.IsInstanceOfType<ViewResult>(result);
        var model = Assert.IsInstanceOfType<PackagePricingViewModel>(view.Model);
        Assert.HasCount(4, model.Packages);
        Assert.IsNull(model.CurrentSubscription);
        Assert.IsFalse(model.Packages.Any(x => x.IsCurrent));
        Assert.AreEqual(0, _subscriptionService.GetActiveSubscriptionCallCount);
    }

    [TestMethod]
    public async Task Index_StudentUser_HighlightsCurrentPackage()
    {
        AddDefaultPackages();
        using var controller = CreateController();
        controller.ControllerContext = CreateControllerContext(UserId, "Student");
        _subscriptionService.ActiveSubscription = new SubscriptionSummaryDto(
            Guid.NewGuid(),
            _packageService.Packages[1].Id,
            "Plus",
            10,
            50,
            "Active",
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(29));

        var result = await controller.Index(CancellationToken.None);

        var view = Assert.IsInstanceOfType<ViewResult>(result);
        var model = Assert.IsInstanceOfType<PackagePricingViewModel>(view.Model);
        Assert.AreEqual("Plus", model.Packages.Single(x => x.IsCurrent).Package.Name);
        Assert.AreEqual(1, _subscriptionService.GetActiveSubscriptionCallCount);
        Assert.AreEqual(UserId, _subscriptionService.LastUserId);
    }

    [TestMethod]
    public async Task Index_TeacherUser_DoesNotLoadCurrentSubscription()
    {
        AddDefaultPackages();
        using var controller = CreateController();
        controller.ControllerContext = CreateControllerContext(UserId, "Teacher");

        var result = await controller.Index(CancellationToken.None);

        var view = Assert.IsInstanceOfType<ViewResult>(result);
        var model = Assert.IsInstanceOfType<PackagePricingViewModel>(view.Model);
        Assert.IsNull(model.CurrentSubscription);
        Assert.IsFalse(model.Packages.Any(x => x.IsCurrent));
        Assert.AreEqual(0, _subscriptionService.GetActiveSubscriptionCallCount);
    }

    [TestMethod]
    public async Task Index_AdminUser_ReturnsPackagesWithoutCurrentSubscription()
    {
        AddDefaultPackages();
        using var controller = CreateController();
        controller.ControllerContext = CreateControllerContext(UserId, "Admin");

        var result = await controller.Index(CancellationToken.None);

        var view = Assert.IsInstanceOfType<ViewResult>(result);
        var model = Assert.IsInstanceOfType<PackagePricingViewModel>(view.Model);
        Assert.HasCount(4, model.Packages);
        Assert.IsNull(model.CurrentSubscription);
        Assert.IsFalse(model.Packages.Any(x => x.IsCurrent));
        Assert.AreEqual(0, _subscriptionService.GetActiveSubscriptionCallCount);
    }

    [TestMethod]
    public async Task Buy_ValidPackage_RedirectsToPricingWithPendingPaymentMessage()
    {
        AddDefaultPackages();
        using var controller = CreateController();
        controller.ControllerContext = CreateControllerContext(UserId, "Student");
        controller.TempData = new TempDataDictionary(
            new DefaultHttpContext(),
            new FakeTempDataProvider());

        var result = await controller.Buy(_packageService.Packages[2].Id, CancellationToken.None);

        var redirect = Assert.IsInstanceOfType<RedirectToActionResult>(result);
        Assert.AreEqual(nameof(PackageController.Index), redirect.ActionName);
        Assert.AreEqual(1, _packageService.GetByIdCallCount);
        Assert.IsTrue(controller.TempData.ContainsKey("ErrorMessage"));
    }

    [TestMethod]
    public async Task Buy_TeacherUser_RedirectsWithoutBuyingPackage()
    {
        AddDefaultPackages();
        using var controller = CreateController();
        controller.ControllerContext = CreateControllerContext(UserId, "Teacher");
        controller.TempData = new TempDataDictionary(
            controller.HttpContext,
            new FakeTempDataProvider());

        var result = await controller.Buy(_packageService.Packages[2].Id, CancellationToken.None);

        var redirect = Assert.IsInstanceOfType<RedirectToActionResult>(result);
        Assert.AreEqual(nameof(PackageController.Index), redirect.ActionName);
        Assert.AreEqual(0, _packageService.GetByIdCallCount);
        Assert.IsTrue(controller.TempData.ContainsKey("ErrorMessage"));
    }

    [TestMethod]
    public async Task Buy_AdminUser_RedirectsWithoutBuyingPackage()
    {
        AddDefaultPackages();
        using var controller = CreateController();
        controller.ControllerContext = CreateControllerContext(UserId, "Admin");
        controller.TempData = new TempDataDictionary(
            controller.HttpContext,
            new FakeTempDataProvider());

        var result = await controller.Buy(_packageService.Packages[2].Id, CancellationToken.None);

        var redirect = Assert.IsInstanceOfType<RedirectToActionResult>(result);
        Assert.AreEqual(nameof(PackageController.Index), redirect.ActionName);
        Assert.AreEqual(0, _packageService.GetByIdCallCount);
        Assert.IsTrue(controller.TempData.ContainsKey("ErrorMessage"));
    }

    private PackageController CreateController()
    {
        return new PackageController(_packageService, _subscriptionService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity())
                }
            }
        };
    }

    private void AddDefaultPackages()
    {
        _packageService.Packages.AddRange(
        [
            new PackageDto(Guid.NewGuid(), "Free", "Free", 0m, 2, 10, 36500),
            new PackageDto(Guid.NewGuid(), "Plus", "Plus", 99000m, 10, 50, 30),
            new PackageDto(Guid.NewGuid(), "Pro", "Pro", 199000m, 20, 100, 30),
            new PackageDto(Guid.NewGuid(), "Max", "Max", 399000m, 200, 200, 30)
        ]);
    }

    private static ControllerContext CreateControllerContext(Guid userId, string role)
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role)
        ], "Test");

        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };
    }

    private sealed class FakePackageService : IPackageService
    {
        public List<PackageDto> Packages { get; } = [];
        public int GetByIdCallCount { get; private set; }

        public Task<IReadOnlyList<PackageDto>> GetActivePackagesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<PackageDto>>(Packages);
        }

        public Task<PackageDto> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            GetByIdCallCount++;
            return Task.FromResult(Packages.Single(x => x.Id == id));
        }
    }

    private sealed class FakeSubscriptionService : ISubscriptionService
    {
        public SubscriptionSummaryDto? ActiveSubscription { get; set; }
        public int GetActiveSubscriptionCallCount { get; private set; }
        public Guid? LastUserId { get; private set; }

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
            return Task.FromResult<IReadOnlyList<SubscriptionSummaryDto>>([]);
        }

        public Task<Guid> CreateSubscriptionAsync(
            CreateSubscriptionCommand command,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task CancelSubscriptionAsync(
            Guid subscriptionId,
            Guid userId,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
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
