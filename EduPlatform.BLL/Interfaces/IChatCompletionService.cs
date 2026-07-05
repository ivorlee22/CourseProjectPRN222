namespace EduPlatform.BLL.Interfaces;

public interface IChatCompletionService
{
    Task<string> GenerateAsync(string prompt, CancellationToken cancellationToken);
}
