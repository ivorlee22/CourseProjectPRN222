using System;
using System.Threading;
using System.Threading.Tasks;

namespace EduPlatform.BLL.Interfaces;

public interface IChatQuotaService
{
    Task EnsureCanSendMessageAsync(Guid userId, CancellationToken cancellationToken);
    Task<(int MessageCount, int DailyLimit)> GetQuotaInfoAsync(Guid userId, CancellationToken cancellationToken);
}
