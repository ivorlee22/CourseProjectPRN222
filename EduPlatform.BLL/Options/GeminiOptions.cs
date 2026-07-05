namespace EduPlatform.BLL.Options;

public sealed class GeminiOptions
{
    public const string SectionName = "Gemini";

    public string ApiKey { get; init; } = string.Empty;

    public string ApiBaseUrl { get; init; } = "https://generativelanguage.googleapis.com";

    public string ChatModel { get; init; } = "gemini-2.5-flash";
}
