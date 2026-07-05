using System.IO;
using System.Threading.Tasks;

namespace EduPlatform.BLL.Interfaces;

public interface IFileStorageService
{
    Task<string> UploadAsync(Stream fileStream, string fileName, string contentType);
    Task<string> GetDownloadUrlAsync(string storedPath, string fileName, string contentType);
    Task DeleteAsync(string storedPath);
}
