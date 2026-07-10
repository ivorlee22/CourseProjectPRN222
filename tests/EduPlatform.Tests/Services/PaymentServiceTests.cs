using EduPlatform.BLL.DTOs.Payments;
using EduPlatform.BLL.Interfaces;
using EduPlatform.BLL.Services;
using EduPlatform.DAL.Entities;
using EduPlatform.DAL.Repositories;
using Microsoft.Extensions.Logging.Abstractions;

namespace EduPlatform.Tests.Services;

[TestClass]
public sealed class PaymentServiceTests
{
    private readonly FakePaymentRepository _paymentRepository = new();
    private readonly FakePackageRepository _packageRepository = new();
    private readonly FakeSubscriptionRepository _subscriptionRepository = new();
    private readonly FakeUserRepository _userRepository = new();
    private readonly FakeVNPayService _vnPayService = new();
    private readonly FakeEmailService _emailService = new();
    private readonly PaymentService _service;

    public PaymentServiceTests()
    {
        _service = new PaymentService(
            _paymentRepository,
            _packageRepository,
            _subscriptionRepository,
            _userRepository,
            _vnPayService,
            _emailService,
            NullLogger<PaymentService>.Instance);
    }

    [TestMethod]
    public async Task ProcessCallbackAsync_CancelledPayment_SetsStatusToFailedAndGatewayTxnIdToNull()
    {
        // Arrange
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            PackageId = Guid.NewGuid(),
            Amount = 100000,
            Method = PaymentMethod.VNPay,
            Status = PaymentStatus.Pending,
            InternalReference = "PAY-CANCEL-TEST"
        };
        _paymentRepository.Payments.Add(payment);

        _vnPayService.VerifySignatureResult = true;

        var queryData = new Dictionary<string, string>
        {
            { "vnp_TxnRef", "PAY-CANCEL-TEST" },
            { "vnp_TransactionNo", "0" },
            { "vnp_ResponseCode", "24" } // User cancelled
        };
        var command = new PaymentCallbackCommand(PaymentMethod.VNPay, queryData);

        // Act
        var result = await _service.ProcessCallbackAsync(command, CancellationToken.None);

        // Assert
        Assert.IsFalse(result);
        Assert.AreEqual(PaymentStatus.Failed, payment.Status);
        Assert.IsNull(payment.GatewayTransactionId);
        Assert.AreEqual("24", payment.GatewayResponseCode);
        Assert.AreEqual(1, _paymentRepository.UpdateCallCount);
        Assert.AreEqual(1, _paymentRepository.SaveChangesCallCount);
    }

    [TestMethod]
    public async Task ProcessCallbackAsync_SuccessfulPayment_SetsStatusToSucceededAndGatewayTxnId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var packageId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "test@eduplatform.com", FullName = "Test User" };
        var package = new Package { Id = packageId, Name = "Pro Gói", Price = 199000, DurationDays = 30 };
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PackageId = packageId,
            Amount = package.Price,
            Method = PaymentMethod.VNPay,
            Status = PaymentStatus.Pending,
            InternalReference = "PAY-SUCCESS-TEST",
            Package = package,
            User = user
        };
        _paymentRepository.Payments.Add(payment);
        _packageRepository.Packages.Add(package);
        _userRepository.Users.Add(user);

        _vnPayService.VerifySignatureResult = true;

        var queryData = new Dictionary<string, string>
        {
            { "vnp_TxnRef", "PAY-SUCCESS-TEST" },
            { "vnp_TransactionNo", "12345678" },
            { "vnp_ResponseCode", "00" } // Success
        };
        var command = new PaymentCallbackCommand(PaymentMethod.VNPay, queryData);

        // Act
        var result = await _service.ProcessCallbackAsync(command, CancellationToken.None);

        // Assert
        Assert.IsTrue(result);
        Assert.AreEqual(PaymentStatus.Succeeded, payment.Status);
        Assert.AreEqual("12345678", payment.GatewayTransactionId);
        Assert.AreEqual("00", payment.GatewayResponseCode);
        
        var sub = Assert.ContainsSingle(_subscriptionRepository.Subscriptions);
        Assert.AreEqual(userId, sub.UserId);
        Assert.AreEqual(packageId, sub.PackageId);
        Assert.AreEqual(SubscriptionStatus.Active, sub.Status);
        Assert.AreEqual(sub, payment.Subscription);
        Assert.HasCount(1, _emailService.SentConfirmationEmails);
        Assert.AreEqual(1, _paymentRepository.UpdateCallCount);
        Assert.AreEqual(1, _paymentRepository.SaveChangesCallCount);
    }

    private sealed class FakePaymentRepository : IPaymentRepository
    {
        public List<Payment> Payments { get; } = [];
        public int UpdateCallCount { get; private set; }
        public int SaveChangesCallCount { get; private set; }

        public Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Payments.SingleOrDefault(p => p.Id == id));

        public Task<Payment?> GetByInternalReferenceAsync(string internalReference, CancellationToken cancellationToken = default) =>
            Task.FromResult(Payments.SingleOrDefault(p => p.InternalReference == internalReference));

        public Task<Payment?> GetByGatewayTransactionIdAsync(PaymentMethod method, string gatewayTransactionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Payments.SingleOrDefault(p => p.Method == method && p.GatewayTransactionId == gatewayTransactionId));

        public Task<List<Payment>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Payments.Where(p => p.UserId == userId).ToList());

        public void Add(Payment payment) => Payments.Add(payment);

        public void Update(Payment payment) => UpdateCallCount++;

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCallCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakePackageRepository : IPackageRepository
    {
        public List<Package> Packages { get; } = [];
        public Task<IReadOnlyList<Package>> GetAllAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<Package>>(Packages);
        public Task AddAsync(Package package, CancellationToken cancellationToken) { Packages.Add(package); return Task.CompletedTask; }
        public void Update(Package package) { }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(0);
        public Task<IReadOnlyList<Package>> GetActivePackagesAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<Package>>(Packages);
        public Task<Package?> GetFreePackageAsync(CancellationToken cancellationToken) => Task.FromResult(Packages.FirstOrDefault(p => p.Price == 0));
        public Task<Package?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(Packages.FirstOrDefault(p => p.Id == id));
    }

    private sealed class FakeSubscriptionRepository : ISubscriptionRepository
    {
        public List<Subscription> Subscriptions { get; } = [];
        public int SaveChangesCallCount { get; private set; }
        public int UpdateCallCount { get; private set; }

        public Task<(IReadOnlyList<Subscription> Items, int TotalCount)> GetAllPagedAsync(int page, int pageSize, CancellationToken cancellationToken) =>
            Task.FromResult<(IReadOnlyList<Subscription>, int)>((Subscriptions, Subscriptions.Count));

        public Task<Subscription?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Subscriptions.FirstOrDefault(s => s.Id == id));

        public Task<Subscription?> GetActiveSubscriptionAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var now = DateTimeOffset.UtcNow;
            return Task.FromResult(Subscriptions
                .Where(x => x.UserId == userId && x.Status == SubscriptionStatus.Active && x.StartsAtUtc <= now && x.EndsAtUtc > now)
                .OrderByDescending(x => x.EndsAtUtc)
                .FirstOrDefault());
        }

        public Task<IReadOnlyList<Subscription>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Subscription>>(Subscriptions.Where(s => s.UserId == userId).ToList());

        public Task AddAsync(Subscription subscription, CancellationToken cancellationToken = default)
        {
            if (subscription.Id == Guid.Empty) subscription.Id = Guid.NewGuid();
            Subscriptions.Add(subscription);
            return Task.CompletedTask;
        }

        public void Update(Subscription subscription) => UpdateCallCount++;

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCallCount++;
            return Task.FromResult(1);
        }
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public List<User> Users { get; } = [];
        public int SaveChangesCallCount { get; private set; }

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Users.FirstOrDefault(u => u.Id == id));

        public Task AddAsync(User user, CancellationToken cancellationToken)
        {
            Users.Add(user);
            return Task.CompletedTask;
        }

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
            Task.FromResult(Users.FirstOrDefault(u => u.Email == email));

        public Task<User?> GetByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
            Task.FromResult(Users.FirstOrDefault(u => u.Email.Equals(normalizedEmail, StringComparison.OrdinalIgnoreCase)));

        public Task<(IReadOnlyList<User> Items, int TotalCount)> GetAllAsync(string? keyword, int pageNumber, int pageSize, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<IReadOnlyList<User>> GetByRoleAsync(UserRole role, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<User>>(Users.Where(u => u.Role == role).ToList());

        public void Remove(User user) => Users.Remove(user);

        public int UpdateCallCount { get; private set; }
        public void Update(User user) { UpdateCallCount++; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCallCount++;
            return Task.FromResult(1);
        }
    }

    private sealed class FakeVNPayService : IVNPayService
    {
        public bool VerifySignatureResult { get; set; }
        public string CreatePaymentUrl(Payment payment, string clientIpAddress) => "http://vnpay.mock/pay";
        public bool VerifySignature(IDictionary<string, string> queryData) => VerifySignatureResult;
    }

    private sealed class FakeEmailService : IEmailService
    {
        public List<(string email, string name, string packageName, decimal price, string reference)> SentConfirmationEmails { get; } = [];
        public Task SendAccountCreatedAsync(string recipientEmail, string recipientName, string? temporaryPassword, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SendCourseInvitationAsync(string recipientEmail, string recipientName, string courseTitle, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SendPaymentConfirmationAsync(string recipientEmail, string recipientName, string packageName, decimal price, string paymentReference, CancellationToken cancellationToken)
        {
            SentConfirmationEmails.Add((recipientEmail, recipientName, packageName, price, paymentReference));
            return Task.CompletedTask;
        }
    }
}
