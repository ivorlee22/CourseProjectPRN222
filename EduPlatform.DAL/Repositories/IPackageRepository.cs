using EduPlatform.DAL.Entities;

namespace EduPlatform.DAL.Repositories;

public interface IPackageRepository
{
    Task<IReadOnlyList<Package>> GetActivePackagesAsync(CancellationToken cancellationToken);

    Task<Package?> GetFreePackageAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<Package>> GetAllAsync(CancellationToken cancellationToken);

    Task<Package?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task AddAsync(Package package, CancellationToken cancellationToken);

    void Update(Package package);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
