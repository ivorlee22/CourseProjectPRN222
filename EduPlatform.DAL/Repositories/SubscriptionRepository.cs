using EduPlatform.DAL.Entities;
using EduPlatform.DAL.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EduPlatform.DAL.Repositories;

public sealed class SubscriptionRepository(AppDbContext dbContext) : ISubscriptionRepository
{
    public Task<Subscription?> GetActiveSubscriptionAsync(Guid userId, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        return dbContext.Subscriptions
            .Include(x => x.Package)
            .Where(x => x.UserId == userId 
                     && x.Status == SubscriptionStatus.Active 
                     && x.StartsAtUtc <= now 
                     && x.EndsAtUtc > now)
            .OrderByDescending(x => x.EndsAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Subscription>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.Subscriptions
            .AsNoTracking()
            .Include(x => x.Package)
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<Subscription> Items, int TotalCount)> GetAllPagedAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = dbContext.Subscriptions
            .AsNoTracking()
            .Include(x => x.Package)
            .Include(x => x.User)
            .Where(x => x.User.Role == UserRole.Student);
            
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
            
        return (items, totalCount);
    }

    public Task<Subscription?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.Subscriptions
            .Include(x => x.Package)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task AddAsync(Subscription subscription, CancellationToken cancellationToken)
    {
        return dbContext.Subscriptions.AddAsync(subscription, cancellationToken).AsTask();
    }

    public void Update(Subscription subscription)
    {
        dbContext.Subscriptions.Update(subscription);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
