using EduPlatform.BLL.DTOs.Settings;

namespace EduPlatform.BLL.Interfaces;

public interface ISystemSettingService
{
    /// <summary>
    /// Returns the current chunking configuration, merging database overrides
    /// with the defaults from <c>DocumentOptions</c>.
    /// </summary>
    Task<ChunkingConfigDto> GetChunkingConfigAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Persists updated chunking configuration to the database so it survives
    /// application restarts. Also updates the in-memory options snapshot.
    /// </summary>
    Task UpdateChunkingConfigAsync(
        UpdateChunkingConfigCommand command,
        CancellationToken cancellationToken);
}
