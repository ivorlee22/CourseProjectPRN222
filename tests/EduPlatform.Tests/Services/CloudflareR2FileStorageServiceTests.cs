using EduPlatform.BLL.Services;
using Microsoft.Extensions.Configuration;

namespace EduPlatform.Tests.Services;

[TestClass]
public sealed class CloudflareR2FileStorageServiceTests
{
    [TestMethod]
    public void Constructor_WithoutConfiguration_DoesNotThrow()
    {
        var configuration = new ConfigurationBuilder().Build();
        using var httpClient = new HttpClient();

        using var service = new CloudflareR2FileStorageService(configuration, httpClient);

        Assert.IsNotNull(service);
    }

    [TestMethod]
    public async Task UploadAsync_WithoutConfiguration_ThrowsWhenStorageIsUsed()
    {
        var configuration = new ConfigurationBuilder().Build();
        using var httpClient = new HttpClient();
        using var service = new CloudflareR2FileStorageService(configuration, httpClient);
        await using var stream = new MemoryStream([1, 2, 3]);

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => service.UploadAsync(stream, "lesson.txt", "text/plain"));

        Assert.Contains("CloudStorage:R2:Endpoint", exception.Message);
    }
}
