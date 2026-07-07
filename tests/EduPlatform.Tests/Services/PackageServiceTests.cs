using EduPlatform.BLL.DTOs.Packages;
using EduPlatform.BLL.Exceptions;
using EduPlatform.BLL.Services;
using EduPlatform.DAL.Entities;
using EduPlatform.DAL.Repositories;

namespace EduPlatform.Tests.Services;

[TestClass]
public sealed class PackageServiceTests
{
    private readonly FakePackageRepository _repository = new();
    private readonly PackageService _service;

    public PackageServiceTests()
    {
        _service = new PackageService(_repository);
    }

    [TestMethod]
    public async Task GetActivePackagesAsync_ReturnsOnlyActivePackages()
    {
        _repository.Packages.Add(new Package { Id = Guid.NewGuid(), Name = "Free", Price = 0, IsActive = true });
        _repository.Packages.Add(new Package { Id = Guid.NewGuid(), Name = "Legacy", Price = 50000, IsActive = false });

        var packages = await _service.GetActivePackagesAsync(CancellationToken.None);

        Assert.HasCount(1, packages);
        Assert.AreEqual("Free", packages[0].Name);
    }

    [TestMethod]
    public async Task GetByIdAsync_PackageExists_ReturnsPackage()
    {
        var id = Guid.NewGuid();
        _repository.Packages.Add(new Package { Id = id, Name = "Pro", Price = 199000 });

        var package = await _service.GetByIdAsync(id, CancellationToken.None);

        Assert.IsNotNull(package);
        Assert.AreEqual("Pro", package.Name);
    }

    [TestMethod]
    public async Task GetByIdAsync_PackageNotExists_ThrowsNotFound()
    {
        var exception = await Assert.ThrowsExactlyAsync<ResourceNotFoundException>(
            async () => await _service.GetByIdAsync(Guid.NewGuid(), CancellationToken.None));

        Assert.AreEqual("Không tìm thấy gói cước.", exception.Message);
    }

    private sealed class FakePackageRepository : IPackageRepository
    {
        public List<Package> Packages { get; } = [];

        public Task<IReadOnlyList<Package>> GetActivePackagesAsync(CancellationToken cancellationToken)
        {
            var active = Packages.Where(x => x.IsActive).OrderBy(x => x.Price).ToArray();
            return Task.FromResult<IReadOnlyList<Package>>(active);
        }

        public Task<Package?> GetFreePackageAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(Packages.SingleOrDefault(
                x => x.IsActive && x.Name == "Free" && x.Price == 0m));
        }

        public Task<Package?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(Packages.SingleOrDefault(x => x.Id == id));
        }
    }
}
