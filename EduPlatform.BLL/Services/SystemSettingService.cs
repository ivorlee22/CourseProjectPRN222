using System.Globalization;
using EduPlatform.BLL.DTOs.Settings;
using EduPlatform.BLL.Exceptions;
using EduPlatform.BLL.Interfaces;
using EduPlatform.BLL.Options;
using EduPlatform.DAL.Entities;
using EduPlatform.DAL.Repositories;
using Microsoft.Extensions.Options;

namespace EduPlatform.BLL.Services;

public sealed class SystemSettingService(
    ISystemSettingRepository settingRepository,
    IOptionsMonitor<DocumentOptions> documentOptions) : ISystemSettingService
{
    private const string KeyPrefix = "Chunking:";
    private const string ChunkSizeKey = "Chunking:ChunkSize";
    private const string ChunkOverlapKey = "Chunking:ChunkOverlap";
    private const string MaxFileSizeBytesKey = "Chunking:MaxFileSizeBytes";

    public async Task<ChunkingConfigDto> GetChunkingConfigAsync(CancellationToken cancellationToken)
    {
        var settings = await settingRepository.GetByPrefixAsync(KeyPrefix, cancellationToken);
        var lookup = settings.ToDictionary(s => s.Key, s => s);

        var defaults = documentOptions.CurrentValue;

        var chunkSize = GetIntValue(lookup, ChunkSizeKey, defaults.ChunkSize);
        var chunkOverlap = GetIntValue(lookup, ChunkOverlapKey, defaults.ChunkOverlap);
        var maxFileSize = GetLongValue(lookup, MaxFileSizeBytesKey, defaults.MaxFileSizeBytes);

        DateTimeOffset? lastUpdated = settings.Count > 0
            ? settings.Max(s => s.UpdatedAtUtc)
            : null;

        return new ChunkingConfigDto(chunkSize, chunkOverlap, maxFileSize, lastUpdated);
    }

    public async Task UpdateChunkingConfigAsync(
        UpdateChunkingConfigCommand command,
        CancellationToken cancellationToken)
    {
        if (command.ChunkSize < 100 || command.ChunkSize > 10000)
        {
            throw new BusinessValidationException(
                "Chunk Size phai tu 100 den 10.000 ky tu.");
        }

        if (command.ChunkOverlap < 0 || command.ChunkOverlap >= command.ChunkSize)
        {
            throw new BusinessValidationException(
                "Chunk Overlap phai lon hon hoac bang 0 va nho hon Chunk Size.");
        }

        if (command.MaxFileSizeMb < 1 || command.MaxFileSizeMb > 100)
        {
            throw new BusinessValidationException(
                "Kich thuoc toi da phai tu 1 MB den 100 MB.");
        }

        var maxFileSizeBytes = command.MaxFileSizeMb * 1024L * 1024L;
        var now = DateTimeOffset.UtcNow;

        await settingRepository.UpsertAsync(new SystemSetting
        {
            Key = ChunkSizeKey,
            Value = command.ChunkSize.ToString(CultureInfo.InvariantCulture),
            UpdatedAtUtc = now
        }, cancellationToken);

        await settingRepository.UpsertAsync(new SystemSetting
        {
            Key = ChunkOverlapKey,
            Value = command.ChunkOverlap.ToString(CultureInfo.InvariantCulture),
            UpdatedAtUtc = now
        }, cancellationToken);

        await settingRepository.UpsertAsync(new SystemSetting
        {
            Key = MaxFileSizeBytesKey,
            Value = maxFileSizeBytes.ToString(CultureInfo.InvariantCulture),
            UpdatedAtUtc = now
        }, cancellationToken);

        await settingRepository.SaveChangesAsync(cancellationToken);
    }

    private static int GetIntValue(
        Dictionary<string, SystemSetting> lookup,
        string key,
        int fallback)
    {
        if (lookup.TryGetValue(key, out var setting)
            && int.TryParse(setting.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }

        return fallback;
    }

    private static long GetLongValue(
        Dictionary<string, SystemSetting> lookup,
        string key,
        long fallback)
    {
        if (lookup.TryGetValue(key, out var setting)
            && long.TryParse(setting.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }

        return fallback;
    }
}
