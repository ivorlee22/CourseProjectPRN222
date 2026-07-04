using EduPlatform.DAL.Entities;

namespace EduPlatform.DAL.Repositories;

public interface ISubscriptionRepository
{
    Task<Subscription?> GetActiveSubscriptionAsync(Guid userId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Subscription>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);
    
    Task<Subscription?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task AddAsync(Subscription subscription, CancellationToken cancellationToken);

    void Update(Subscription subscription);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
