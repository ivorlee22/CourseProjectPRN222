using EduPlatform.DAL.Entities;
using EduPlatform.DAL.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EduPlatform.DAL.Repositories;

public sealed class UserRepository(AppDbContext dbContext) : IUserRepository
{
    public Task<User?> GetByNormalizedEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken)
    {
        return dbContext.Users
            .SingleOrDefaultAsync(x => x.NormalizedEmail == normalizedEmail, cancellationToken);
    }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.Users
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<(IReadOnlyList<User> Items, int TotalCount)> GetAllAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Users.AsNoTracking();

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(x => x.FullName)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<User>> GetByRoleAsync(
        UserRole role,
        CancellationToken cancellationToken)
    {
        return await dbContext.Users
            .AsNoTracking()
            .Where(x => x.Role == role)
            .OrderBy(x => x.FullName)
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(User user, CancellationToken cancellationToken)
    {
        return dbContext.Users.AddAsync(user, cancellationToken).AsTask();
    }

    public void Remove(User user)
    {
        dbContext.Users.Remove(user);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
