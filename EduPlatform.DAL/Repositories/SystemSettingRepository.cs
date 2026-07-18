using EduPlatform.DAL.Entities;
using EduPlatform.DAL.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EduPlatform.DAL.Repositories;

public sealed class SystemSettingRepository(AppDbContext context) : ISystemSettingRepository
{
    public async Task<SystemSetting?> GetByKeyAsync(string key, CancellationToken cancellationToken)
    {
        return await context.SystemSettings
            .FirstOrDefaultAsync(s => s.Key == key, cancellationToken);
    }

    public async Task<IReadOnlyList<SystemSetting>> GetByPrefixAsync(
        string prefix,
        CancellationToken cancellationToken)
    {
        return await context.SystemSettings
            .Where(s => s.Key.StartsWith(prefix))
            .OrderBy(s => s.Key)
            .ToListAsync(cancellationToken);
    }

    public async Task UpsertAsync(SystemSetting setting, CancellationToken cancellationToken)
    {
        var existing = await context.SystemSettings
            .FirstOrDefaultAsync(s => s.Key == setting.Key, cancellationToken);

        if (existing is null)
        {
            await context.SystemSettings.AddAsync(setting, cancellationToken);
        }
        else
        {
            existing.Value = setting.Value;
            existing.UpdatedAtUtc = setting.UpdatedAtUtc;
        }
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await context.SaveChangesAsync(cancellationToken);
    }
}
