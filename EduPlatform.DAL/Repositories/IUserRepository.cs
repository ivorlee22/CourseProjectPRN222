using EduPlatform.DAL.Entities;

namespace EduPlatform.DAL.Repositories;

public interface IUserRepository
{
    Task<User?> GetByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken);

    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<(IReadOnlyList<User> Items, int TotalCount)> GetAllAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    Task AddAsync(User user, CancellationToken cancellationToken);

    void Remove(User user);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
