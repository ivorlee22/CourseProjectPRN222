using EduPlatform.BLL.DTOs.Users;
using EduPlatform.BLL.Interfaces;
using EduPlatform.BLL.Models;
using EduPlatform.BLL.Services;
using EduPlatform.DAL.Entities;
using EduPlatform.DAL.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using BllUserRole = EduPlatform.BLL.Enums.UserRole;
using DalUserRole = EduPlatform.DAL.Entities.UserRole;

namespace EduPlatform.Tests.Services;

[TestClass]
public sealed class UserServiceEmailTests
{
    private readonly FakeUserRepository _repository = new();
    private readonly FakeEmailService _emailService = new();
    private readonly UserService _service;

    public UserServiceEmailTests()
    {
        _service = new UserService(
            _repository,
            _emailService,
            NullLogger<UserService>.Instance);
    }

    [TestMethod]
    public async Task RegisterAsync_SendsAccountEmailToRegisteredAddress()
    {
        await _service.RegisterAsync(
            new RegisterCommand("Demo Student", "student@example.test", "Password1"),
            CancellationToken.None);

        Assert.HasCount(1, _emailService.AccountEmails);
        var email = _emailService.AccountEmails[0];
        Assert.AreEqual("student@example.test", email.RecipientEmail);
        Assert.AreEqual("Demo Student", email.RecipientName);
        Assert.IsNull(email.TemporaryPassword);
    }

    [TestMethod]
    public async Task CreateAsync_SendsAccountEmailWithInitialPassword()
    {
        var actor = new ActorContext(Guid.NewGuid(), BllUserRole.Admin);

        await _service.CreateAsync(
            new CreateUserCommand("Demo Teacher", "teacher@example.test", "Teacher123", BllUserRole.Teacher),
            actor,
            CancellationToken.None);

        Assert.HasCount(1, _emailService.AccountEmails);
        var email = _emailService.AccountEmails[0];
        Assert.AreEqual("teacher@example.test", email.RecipientEmail);
        Assert.AreEqual("Teacher123", email.TemporaryPassword);
    }

    [TestMethod]
    public async Task RegisterAsync_EmailFailureKeepsCreatedAccount()
    {
        _emailService.ThrowOnAccountEmail = true;

        var userId = await _service.RegisterAsync(
            new RegisterCommand("Demo Student", "student@example.test", "Password1"),
            CancellationToken.None);

        Assert.HasCount(1, _repository.Users);
        Assert.AreEqual(userId, _repository.Users[0].Id);
        Assert.AreEqual("student@example.test", _repository.Users[0].Email);
        Assert.AreEqual(1, _repository.SaveChangesCallCount);
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public List<User> Users { get; } = [];

        public int SaveChangesCallCount { get; private set; }

        public Task<User?> GetByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
        {
            return Task.FromResult(Users.SingleOrDefault(x => x.NormalizedEmail == normalizedEmail));
        }

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(Users.SingleOrDefault(x => x.Id == id));
        }

        public Task<(IReadOnlyList<User> Items, int TotalCount)> GetAllAsync(
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(((IReadOnlyList<User>)Users, Users.Count));
        }

        public Task<IReadOnlyList<User>> GetByRoleAsync(DalUserRole role, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<User>>(Users.Where(x => x.Role == role).ToArray());
        }

        public Task AddAsync(User user, CancellationToken cancellationToken)
        {
            Users.Add(user);
            return Task.CompletedTask;
        }

        public void Remove(User user)
        {
            Users.Remove(user);
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCallCount++;
            return Task.FromResult(1);
        }
    }

    private sealed class FakeEmailService : IEmailService
    {
        public List<AccountEmail> AccountEmails { get; } = [];

        public bool ThrowOnAccountEmail { get; set; }

        public Task SendAccountCreatedAsync(
            string recipientEmail,
            string recipientName,
            string? temporaryPassword,
            CancellationToken cancellationToken)
        {
            if (ThrowOnAccountEmail)
            {
                throw new InvalidOperationException("SMTP unavailable.");
            }

            AccountEmails.Add(new AccountEmail(recipientEmail, recipientName, temporaryPassword));
            return Task.CompletedTask;
        }

        public Task SendCourseInvitationAsync(
            string recipientEmail,
            string recipientName,
            string courseTitle,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task SendPaymentConfirmationAsync(
            string recipientEmail,
            string recipientName,
            string packageName,
            decimal amount,
            string transactionReference,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed record AccountEmail(
        string RecipientEmail,
        string RecipientName,
        string? TemporaryPassword);
}
