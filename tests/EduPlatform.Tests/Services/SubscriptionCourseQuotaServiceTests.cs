using EduPlatform.BLL.Exceptions;
using EduPlatform.BLL.Services;
using EduPlatform.DAL.Entities;
using EduPlatform.DAL.Repositories;

namespace EduPlatform.Tests.Services;

[TestClass]
public sealed class SubscriptionCourseQuotaServiceTests
{
    private static readonly Guid UserId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private readonly FakePackageRepository _packageRepository = new();
    private readonly FakeSubscriptionRepository _subscriptionRepository = new();
    private readonly SubscriptionCourseQuotaService _service;
    private readonly DateTimeOffset _now = new(2026, 7, 7, 0, 0, 0, TimeSpan.Zero);

    public SubscriptionCourseQuotaServiceTests()
    {
        _service = new SubscriptionCourseQuotaService(
            _subscriptionRepository,
            _packageRepository);
    }

    [TestMethod]
    public async Task EnsureCanCreateCourseAsync_ActiveSubscriptionUnderLimit_Allows()
    {
        AddActiveSubscription(maxCourses: 5);

        await _service.EnsureCanCreateCourseAsync(UserId, 4, CancellationToken.None);

        Assert.AreEqual(1, _subscriptionRepository.GetActiveSubscriptionCallCount);
        Assert.AreEqual(0, _packageRepository.GetFreePackageCallCount);
    }

    [TestMethod]
    public async Task EnsureCanCreateCourseAsync_ActiveSubscriptionAtExactLimit_Throws()
    {
        AddActiveSubscription(maxCourses: 5);

        await Assert.ThrowsExactlyAsync<CourseQuotaExceededException>(
            async () => await _service.EnsureCanCreateCourseAsync(
                UserId,
                5,
                CancellationToken.None));

        Assert.AreEqual(0, _packageRepository.GetFreePackageCallCount);
    }

    [TestMethod]
    public async Task EnsureCanCreateCourseAsync_ActiveSubscriptionOverLimit_Throws()
    {
        AddActiveSubscription(maxCourses: 5);

        await Assert.ThrowsExactlyAsync<CourseQuotaExceededException>(
            async () => await _service.EnsureCanCreateCourseAsync(
                UserId,
                6,
                CancellationToken.None));
    }

    [TestMethod]
    public async Task EnsureCanCreateCourseAsync_ExpiredSubscriptionFallsBackToFreeExactLimit_Throws()
    {
        AddSubscription(maxCourses: 5, startsAtUtc: _now.AddDays(-10), endsAtUtc: _now.AddDays(-1));
        AddFreePackage(maxCourses: 1);

        await Assert.ThrowsExactlyAsync<CourseQuotaExceededException>(
            async () => await _service.EnsureCanCreateCourseAsync(
                UserId,
                1,
                CancellationToken.None));

        Assert.AreEqual(1, _packageRepository.GetFreePackageCallCount);
    }

    [TestMethod]
    public async Task EnsureCanCreateCourseAsync_ExpiredSubscriptionFallsBackToFreeUnderLimit_Allows()
    {
        AddSubscription(maxCourses: 5, startsAtUtc: _now.AddDays(-10), endsAtUtc: _now.AddDays(-1));
        AddFreePackage(maxCourses: 2);

        await _service.EnsureCanCreateCourseAsync(UserId, 1, CancellationToken.None);
    }

    [TestMethod]
    public async Task EnsureCanCreateCourseAsync_MissingSubscriptionFallsBackToFreeUnderLimit_Allows()
    {
        AddFreePackage(maxCourses: 1);

        await _service.EnsureCanCreateCourseAsync(UserId, 0, CancellationToken.None);

        Assert.AreEqual(1, _subscriptionRepository.GetActiveSubscriptionCallCount);
        Assert.AreEqual(1, _packageRepository.GetFreePackageCallCount);
    }

    [TestMethod]
    public async Task EnsureCanCreateCourseAsync_MissingSubscriptionFallsBackToFreeExactLimit_Throws()
    {
        AddFreePackage(maxCourses: 1);

        await Assert.ThrowsExactlyAsync<CourseQuotaExceededException>(
            async () => await _service.EnsureCanCreateCourseAsync(
                UserId,
                1,
                CancellationToken.None));
    }

    [TestMethod]
    public async Task EnsureCanCreateCourseAsync_ConcurrentExactLimitRequests_Throw()
    {
        AddActiveSubscription(maxCourses: 5);

        var tasks = Enumerable
            .Range(0, 2)
            .Select(_ => Assert.ThrowsExactlyAsync<CourseQuotaExceededException>(
                async () => await _service.EnsureCanCreateCourseAsync(
                    UserId,
                    5,
                    CancellationToken.None)));

        await Task.WhenAll(tasks);
    }

    private void AddFreePackage(int maxCourses)
    {
        _packageRepository.Packages.Add(new Package
        {
            Id = Guid.NewGuid(),
            Name = "Free",
            Price = 0m,
            IsActive = true,
            MaxCourses = maxCourses
        });
    }

    private void AddActiveSubscription(int maxCourses)
    {
        AddSubscription(maxCourses, _now.AddDays(-1), _now.AddDays(1));
    }

    private void AddSubscription(
        int maxCourses,
        DateTimeOffset startsAtUtc,
        DateTimeOffset endsAtUtc)
    {
        var package = new Package
        {
            Id = Guid.NewGuid(),
            Name = "Plus",
            MaxCourses = maxCourses,
            IsActive = true
        };
        _subscriptionRepository.Subscriptions.Add(new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            PackageId = package.Id,
            Package = package,
            Status = SubscriptionStatus.Active,
            StartsAtUtc = startsAtUtc,
            EndsAtUtc = endsAtUtc
        });
    }

    private sealed class FakePackageRepository : IPackageRepository
    {

        public Task<IReadOnlyList<Package>> GetAllAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<Package>>(Packages);
        public Task AddAsync(Package package, CancellationToken cancellationToken) { Packages.Add(package); return Task.CompletedTask; }
        public void Update(Package package) { }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(0);

        public List<Package> Packages { get; } = [];
        public int GetFreePackageCallCount { get; private set; }

        public Task<IReadOnlyList<Package>> GetActivePackagesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<Package>>(
                Packages.Where(x => x.IsActive).OrderBy(x => x.Price).ToArray());
        }

        public Task<Package?> GetFreePackageAsync(CancellationToken cancellationToken)
        {
            GetFreePackageCallCount++;
            return Task.FromResult(Packages.SingleOrDefault(
                x => x.IsActive && x.Name == "Free" && x.Price == 0m));
        }

        public Task<Package?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(Packages.SingleOrDefault(x => x.Id == id));
        }
    }

    private sealed class FakeSubscriptionRepository : ISubscriptionRepository
    {

        public Task<(IReadOnlyList<Subscription> Items, int TotalCount)> GetAllPagedAsync(int page, int pageSize, CancellationToken cancellationToken) => Task.FromResult<(IReadOnlyList<Subscription>, int)>((Subscriptions, Subscriptions.Count));

        public List<Subscription> Subscriptions { get; } = [];
        public int GetActiveSubscriptionCallCount { get; private set; }

        public Task<Subscription?> GetActiveSubscriptionAsync(Guid userId, CancellationToken cancellationToken)
        {
            GetActiveSubscriptionCallCount++;
            return Task.FromResult(Subscriptions
                .Where(x => x.UserId == userId
                    && x.Status == SubscriptionStatus.Active
                    && x.StartsAtUtc <= new DateTimeOffset(2026, 7, 7, 0, 0, 0, TimeSpan.Zero)
                    && x.EndsAtUtc > new DateTimeOffset(2026, 7, 7, 0, 0, 0, TimeSpan.Zero))
                .OrderByDescending(x => x.EndsAtUtc)
                .FirstOrDefault());
        }

        public Task<IReadOnlyList<Subscription>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<Subscription>>(
                Subscriptions.Where(x => x.UserId == userId).ToArray());
        }

        public Task<Subscription?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(Subscriptions.SingleOrDefault(x => x.Id == id));
        }

        public Task AddAsync(Subscription subscription, CancellationToken cancellationToken)
        {
            Subscriptions.Add(subscription);
            return Task.CompletedTask;
        }

        public void Update(Subscription subscription)
        {
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(1);
        }
    }
}
