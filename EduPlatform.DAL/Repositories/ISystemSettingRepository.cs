using EduPlatform.DAL.Entities;

namespace EduPlatform.DAL.Repositories;

public interface ISystemSettingRepository
{
    Task<SystemSetting?> GetByKeyAsync(string key, CancellationToken cancellationToken);

    Task<IReadOnlyList<SystemSetting>> GetByPrefixAsync(string prefix, CancellationToken cancellationToken);

    Task UpsertAsync(SystemSetting setting, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
