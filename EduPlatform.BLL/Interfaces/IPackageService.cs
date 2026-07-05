using EduPlatform.BLL.DTOs.Packages;

namespace EduPlatform.BLL.Interfaces;

public interface IPackageService
{
    Task<IReadOnlyList<PackageDto>> GetActivePackagesAsync(CancellationToken cancellationToken);

    Task<PackageDto> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
