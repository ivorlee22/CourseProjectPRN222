namespace EduPlatform.BLL.DTOs.Settings;

public sealed record ChunkingConfigDto(
    int ChunkSize,
    int ChunkOverlap,
    long MaxFileSizeBytes,
    DateTimeOffset? LastUpdatedUtc);

public sealed record UpdateChunkingConfigCommand(
    int ChunkSize,
    int ChunkOverlap,
    long MaxFileSizeMb);
