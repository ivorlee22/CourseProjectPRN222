namespace EduPlatform.BLL.Options;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string Host { get; init; } = "smtp.gmail.com";

    public int Port { get; init; } = 587;

    public bool UseStartTls { get; init; } = true;

    public string FromName { get; init; } = "EduPlatform";

    public string FromAddress { get; init; } = string.Empty;

    public string Username { get; init; } = string.Empty;

    public string AppPassword { get; init; } = string.Empty;
}
