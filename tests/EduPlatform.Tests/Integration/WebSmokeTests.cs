using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EduPlatform.Tests.Integration;

[TestClass]
[TestCategory("Integration")]
public sealed class WebSmokeTests
{
    private readonly TestContext _testContext;

    public WebSmokeTests(TestContext testContext)
    {
        _testContext = testContext;
    }

    [TestMethod]
    public async Task HomePage_WithoutDatabaseConnection_ReturnsSuccess()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

        using var response = await client.GetAsync(
            "/",
            _testContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual(
            "nosniff",
            Assert.ContainsSingle(response.Headers.GetValues("X-Content-Type-Options")));
        Assert.Contains(
            "frame-ancestors 'none'",
            Assert.ContainsSingle(response.Headers.GetValues("Content-Security-Policy")));
        var html = await response.Content.ReadAsStringAsync(
            _testContext.CancellationToken);
        Assert.Contains("EduPlatform", html);
        Assert.DoesNotContain("@page", html);
    }

    [TestMethod]
    public async Task ChatPage_AnonymousUser_RedirectsToLogin()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

        using var response = await client.GetAsync(
            $"/Chat?courseId={Guid.NewGuid()}",
            _testContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode);
        Assert.IsNotNull(response.Headers.Location);
        Assert.AreEqual("/Account/Login", response.Headers.Location.AbsolutePath);
    }
}
