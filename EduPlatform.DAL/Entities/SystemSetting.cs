namespace EduPlatform.DAL.Entities;

/// <summary>
/// A simple key-value pair persisted in the database for runtime-configurable
/// system settings such as chunking parameters.
/// </summary>
public sealed class SystemSetting
{
    public string Key { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public DateTimeOffset UpdatedAtUtc { get; set; }
}
