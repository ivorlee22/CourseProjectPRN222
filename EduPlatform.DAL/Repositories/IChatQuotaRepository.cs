using System;
using System.Threading;
using System.Threading.Tasks;

namespace EduPlatform.DAL.Repositories;

public interface IChatQuotaRepository
{
    Task<int> CountMessagesTodayAsync(Guid userId, DateTimeOffset startOfDay, CancellationToken cancellationToken);
    
    Task LockUserRowAsync(Guid userId, CancellationToken cancellationToken);
}
