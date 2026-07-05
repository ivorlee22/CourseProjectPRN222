namespace EduPlatform.BLL.Interfaces;

public interface IChatCompletionService
{
    Task<string> GenerateAsync(string prompt, CancellationToken cancellationToken);

    IAsyncEnumerable<string> StreamAsync(
        string prompt,
        CancellationToken cancellationToken);
}
