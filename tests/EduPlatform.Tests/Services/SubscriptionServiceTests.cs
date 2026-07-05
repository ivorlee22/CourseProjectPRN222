using EduPlatform.BLL.DTOs.Subscriptions;
using EduPlatform.BLL.Exceptions;
using EduPlatform.BLL.Services;
using EduPlatform.DAL.Entities;
using EduPlatform.DAL.Repositories;

namespace EduPlatform.Tests.Services;

[TestClass]
public sealed class SubscriptionServiceTests
{
    private static readonly Guid UserId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    
    private readonly FakePackageRepository _packageRepository = new();
    private readonly FakeSubscriptionRepository _subscriptionRepository = new();
    private readonly SubscriptionService _service;
    private readonly DateTimeOffset _fixedTime = new(2026, 7, 4, 12, 0, 0, TimeSpan.Zero);

    public SubscriptionServiceTests()
    {
        _service = new SubscriptionService(
            _subscriptionRepository,
            _packageRepository,
            new FixedTimeProvider(_fixedTime));
    }

    [TestMethod]
    public async Task CreateSubscriptionAsync_PackageNotExists_ThrowsNotFound()
    {
        var command = new CreateSubscriptionCommand(UserId, Guid.NewGuid());

        var exception = await Assert.ThrowsExactlyAsync<ResourceNotFoundException>(
            async () => await _service.CreateSubscriptionAsync(command, CancellationToken.None));

        Assert.AreEqual("Không tìm thấy gói cước.", exception.Message);
    }

    [TestMethod]
    public async Task CreateSubscriptionAsync_PackageInactive_ThrowsValidation()
    {
        var packageId = Guid.NewGuid();
        _packageRepository.Packages.Add(new Package { Id = packageId, Name = "Legacy", IsActive = false });
        var command = new CreateSubscriptionCommand(UserId, packageId);

        var exception = await Assert.ThrowsExactlyAsync<BusinessValidationException>(
            async () => await _service.CreateSubscriptionAsync(command, CancellationToken.None));

        Assert.AreEqual("Gói cước này không còn khả dụng.", exception.Message);
    }

    [TestMethod]
    public async Task CreateSubscriptionAsync_ValidPackage_CreatesPendingSubscription()
    {
        var packageId = Guid.NewGuid();
        _packageRepository.Packages.Add(new Package { Id = packageId, Name = "Plus", IsActive = true, DurationDays = 30 });
        var command = new CreateSubscriptionCommand(UserId, packageId);

        var id = await _service.CreateSubscriptionAsync(command, CancellationToken.None);

        var sub = Assert.ContainsSingle(_subscriptionRepository.Subscriptions);
        Assert.AreEqual(id, sub.Id);
        Assert.AreEqual(UserId, sub.UserId);
        Assert.AreEqual(packageId, sub.PackageId);
        Assert.AreEqual(SubscriptionStatus.Pending, sub.Status);
        Assert.AreEqual(_fixedTime, sub.StartsAtUtc);
        Assert.AreEqual(_fixedTime.AddDays(30), sub.EndsAtUtc);
        Assert.AreEqual(1, _subscriptionRepository.SaveChangesCallCount);
    }

    [TestMethod]
    public async Task CancelSubscriptionAsync_NonOwner_ThrowsForbidden()
    {
        var subId = Guid.NewGuid();
        _subscriptionRepository.Subscriptions.Add(new Subscription 
        { 
            Id = subId, 
            UserId = Guid.NewGuid(), // Different user
            Status = SubscriptionStatus.Active 
        });

        var exception = await Assert.ThrowsExactlyAsync<ForbiddenOperationException>(
            async () => await _service.CancelSubscriptionAsync(subId, UserId, CancellationToken.None));

        Assert.AreEqual("Bạn không có quyền quản lý gói đăng ký này.", exception.Message);
    }

    [TestMethod]
    public async Task CancelSubscriptionAsync_AlreadyCancelled_ThrowsValidation()
    {
        var subId = Guid.NewGuid();
        _subscriptionRepository.Subscriptions.Add(new Subscription 
        { 
            Id = subId, 
            UserId = UserId, 
            Status = SubscriptionStatus.Cancelled 
        });

        var exception = await Assert.ThrowsExactlyAsync<BusinessValidationException>(
            async () => await _service.CancelSubscriptionAsync(subId, UserId, CancellationToken.None));

        Assert.AreEqual("Gói đăng ký đã được hủy trước đó.", exception.Message);
    }

    [TestMethod]
    public async Task CancelSubscriptionAsync_Valid_CancelsSubscription()
    {
        var subId = Guid.NewGuid();
        var sub = new Subscription 
        { 
            Id = subId, 
            UserId = UserId, 
            Status = SubscriptionStatus.Active 
        };
        _subscriptionRepository.Subscriptions.Add(sub);

        await _service.CancelSubscriptionAsync(subId, UserId, CancellationToken.None);

        Assert.AreEqual(SubscriptionStatus.Cancelled, sub.Status);
        Assert.AreEqual(_fixedTime, sub.CancelledAtUtc);
        Assert.AreEqual(1, _subscriptionRepository.UpdateCallCount);
        Assert.AreEqual(1, _subscriptionRepository.SaveChangesCallCount);
    }

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;
    }

    private sealed class FakePackageRepository : IPackageRepository
    {
        public List<Package> Packages { get; } = [];

        public Task<IReadOnlyList<Package>> GetActivePackagesAsync(CancellationToken cancellationToken)
        {
            var active = Packages.Where(x => x.IsActive).OrderBy(x => x.Price).ToArray();
            return Task.FromResult<IReadOnlyList<Package>>(active);
        }

        public Task<Package?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(Packages.SingleOrDefault(x => x.Id == id));
        }
    }

    private sealed class FakeSubscriptionRepository : ISubscriptionRepository
    {
        public List<Subscription> Subscriptions { get; } = [];
        public int SaveChangesCallCount { get; private set; }
        public int UpdateCallCount { get; private set; }

        public Task<Subscription?> GetActiveSubscriptionAsync(Guid userId, CancellationToken cancellationToken)
        {
            var now = DateTimeOffset.UtcNow;
            return Task.FromResult(Subscriptions
                .Where(x => x.UserId == userId && x.Status == SubscriptionStatus.Active && x.StartsAtUtc <= now && x.EndsAtUtc > now)
                .OrderByDescending(x => x.EndsAtUtc)
                .FirstOrDefault());
        }

        public Task<IReadOnlyList<Subscription>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
        {
            var items = Subscriptions.Where(x => x.UserId == userId).ToArray();
            return Task.FromResult<IReadOnlyList<Subscription>>(items);
        }

        public Task<Subscription?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(Subscriptions.SingleOrDefault(x => x.Id == id));
        }

        public Task AddAsync(Subscription subscription, CancellationToken cancellationToken)
        {
            if (subscription.Id == Guid.Empty) subscription.Id = Guid.NewGuid();
            Subscriptions.Add(subscription);
            return Task.CompletedTask;
        }

        public void Update(Subscription subscription)
        {
            UpdateCallCount++;
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCallCount++;
            return Task.FromResult(1);
        }
    }
}
