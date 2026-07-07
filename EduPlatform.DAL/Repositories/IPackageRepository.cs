using EduPlatform.DAL.Entities;

namespace EduPlatform.DAL.Repositories;

public interface IPackageRepository
{
    Task<IReadOnlyList<Package>> GetActivePackagesAsync(CancellationToken cancellationToken);

    Task<Package?> GetFreePackageAsync(CancellationToken cancellationToken);

    Task<Package?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
