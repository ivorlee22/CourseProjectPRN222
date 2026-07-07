using EduPlatform.DAL.Entities;
using EduPlatform.DAL.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EduPlatform.DAL.Repositories;

public sealed class PackageRepository(AppDbContext dbContext) : IPackageRepository
{
    public async Task<IReadOnlyList<Package>> GetActivePackagesAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Packages
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Price)
            .ToListAsync(cancellationToken);
    }

    public Task<Package?> GetFreePackageAsync(CancellationToken cancellationToken)
    {
        return dbContext.Packages
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.IsActive && x.Name == "Free" && x.Price == 0m,
                cancellationToken);
    }

    public Task<Package?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.Packages
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    }
}
