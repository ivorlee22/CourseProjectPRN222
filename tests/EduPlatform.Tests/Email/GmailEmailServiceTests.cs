using EduPlatform.BLL.Options;
using EduPlatform.BLL.Services;
using Microsoft.Extensions.Options;

namespace EduPlatform.Tests.Email;

[TestClass]
public sealed class GmailEmailServiceTests
{
    [TestMethod]
    public async Task SendAccountCreatedAsync_MissingCredentials_ThrowsBeforeNetworkCall()
    {
        var options = Options.Create(new EmailOptions
        {
            Host = "smtp.gmail.com",
            Port = 587,
            UseStartTls = true,
            FromName = "EduPlatform"
        });
        var service = new GmailEmailService(options);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            async () => await service.SendAccountCreatedAsync(
                "student@example.test",
                "Demo Student",
                temporaryPassword: null,
                CancellationToken.None));
    }
}
