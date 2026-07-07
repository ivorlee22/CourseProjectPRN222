using System;
using System.Threading;
using System.Threading.Tasks;

namespace EduPlatform.BLL.Interfaces;

public interface IChatQuotaService
{
    Task EnsureCanSendMessageAsync(Guid userId, CancellationToken cancellationToken);
}
