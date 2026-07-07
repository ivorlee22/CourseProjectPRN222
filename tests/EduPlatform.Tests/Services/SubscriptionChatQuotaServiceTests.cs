using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EduPlatform.BLL.Exceptions;
using EduPlatform.BLL.Services;
using EduPlatform.DAL.Entities;
using EduPlatform.DAL.Repositories;

namespace EduPlatform.Tests.Services;

[TestClass]
public sealed class SubscriptionChatQuotaServiceTests
{
    private readonly FakeSubscriptionRepository _subscriptionRepository = new();
    private readonly FakePackageRepository _packageRepository = new();
    private readonly FakeChatQuotaRepository _chatQuotaRepository = new();
    private readonly FakeUserRepository _userRepository = new();
    private readonly SubscriptionChatQuotaService _sut;

    public SubscriptionChatQuotaServiceTests()
    {
        _sut = new SubscriptionChatQuotaService(
            _subscriptionRepository,
            _packageRepository,
            _chatQuotaRepository,
            _userRepository,
            TimeProvider.System);
    }

    [TestMethod]
    public async Task EnsureCanSendMessageAsync_AdminBypassesQuota()
    {
        var userId = Guid.NewGuid();
        _userRepository.Users.Add(new User { Id = userId, Role = EduPlatform.DAL.Entities.UserRole.Admin });

        await _sut.EnsureCanSendMessageAsync(userId, CancellationToken.None);

        Assert.AreEqual(0, _chatQuotaRepository.LockCallCount);
    }

    [TestMethod]
    public async Task EnsureCanSendMessageAsync_BelowLimit_Succeeds()
    {
        var userId = Guid.NewGuid();
        var package = new Package { DailyChats = 10 };
        _userRepository.Users.Add(new User { Id = userId, Role = EduPlatform.DAL.Entities.UserRole.Student });
        _subscriptionRepository.Subscriptions.Add(new Subscription { UserId = userId, Package = package });
        _chatQuotaRepository.MessageCount = 5;

        await _sut.EnsureCanSendMessageAsync(userId, CancellationToken.None);

        Assert.AreEqual(1, _chatQuotaRepository.LockCallCount);
    }

    [TestMethod]
    public async Task EnsureCanSendMessageAsync_AboveLimit_ThrowsException()
    {
        var userId = Guid.NewGuid();
        var package = new Package { DailyChats = 10 };
        _userRepository.Users.Add(new User { Id = userId, Role = EduPlatform.DAL.Entities.UserRole.Student });
        _subscriptionRepository.Subscriptions.Add(new Subscription { UserId = userId, Package = package });
        _chatQuotaRepository.MessageCount = 10;

        var exception = await Assert.ThrowsExactlyAsync<ChatQuotaExceededException>(
            async () => await _sut.EnsureCanSendMessageAsync(userId, CancellationToken.None));

        Assert.Contains("10 tin nhắn trong ngày", exception.Message);
    }

    [TestMethod]
    public async Task EnsureCanSendMessageAsync_NoActiveSubscription_UsesFreePackageLimit()
    {
        var userId = Guid.NewGuid();
        _userRepository.Users.Add(new User { Id = userId, Role = EduPlatform.DAL.Entities.UserRole.Student });
        _packageRepository.Packages.Add(new Package { Name = "Free", Price = 0, IsActive = true, DailyChats = 5 });
        _chatQuotaRepository.MessageCount = 4;

        await _sut.EnsureCanSendMessageAsync(userId, CancellationToken.None);

        Assert.AreEqual(1, _chatQuotaRepository.LockCallCount);
    }

    private sealed class FakeSubscriptionRepository : ISubscriptionRepository
    {
        public List<Subscription> Subscriptions { get; } = [];

        public Task<Subscription?> GetActiveSubscriptionAsync(Guid userId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Subscriptions.FirstOrDefault(x => x.UserId == userId));
        }

        public Task AddAsync(Subscription subscription, CancellationToken cancellationToken) => throw new NotImplementedException();
        public void Update(Subscription subscription) => throw new NotImplementedException();
        public Task<Subscription?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<int> GetActiveSubscriptionCountAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<(IReadOnlyList<Subscription> Items, int TotalCount)> GetAllPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<IReadOnlyList<Subscription>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
    }

    private sealed class FakePackageRepository : IPackageRepository
    {
        public List<Package> Packages { get; } = [];

        public Task<Package?> GetFreePackageAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(Packages.SingleOrDefault(x => x.IsActive && x.Name == "Free" && x.Price == 0m));
        }
        
        public Task<IReadOnlyList<Package>> GetActivePackagesAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<Package?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<IReadOnlyList<Package>> GetAllAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task AddAsync(Package package, CancellationToken cancellationToken) => throw new NotImplementedException();
        public void Update(Package package) => throw new NotImplementedException();
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
    }

    private sealed class FakeChatQuotaRepository : IChatQuotaRepository
    {
        public int LockCallCount { get; private set; }
        public int MessageCount { get; set; }

        public Task<int> CountMessagesTodayAsync(Guid userId, DateTimeOffset startOfDay, CancellationToken cancellationToken)
        {
            return Task.FromResult(MessageCount);
        }

        public Task LockUserRowAsync(Guid userId, CancellationToken cancellationToken)
        {
            LockCallCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public List<User> Users { get; } = [];

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(Users.FirstOrDefault(x => x.Id == id));
        }

        public Task AddAsync(User user, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<User?> GetByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<(IReadOnlyList<User> Items, int TotalCount)> GetAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<IReadOnlyList<User>> GetByRoleAsync(EduPlatform.DAL.Entities.UserRole role, CancellationToken cancellationToken) => throw new NotImplementedException();
        public void Remove(User user) => throw new NotImplementedException();
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
    }
}
