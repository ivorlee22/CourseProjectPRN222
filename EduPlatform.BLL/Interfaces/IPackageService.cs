using EduPlatform.BLL.DTOs.Packages;

namespace EduPlatform.BLL.Interfaces;

public interface IPackageService
{
    Task<IReadOnlyList<PackageDto>> GetActivePackagesAsync(CancellationToken cancellationToken);

    Task<PackageDto> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<PackageAdminDto>> GetAllPackagesAsync(CancellationToken cancellationToken);

    Task<Guid> CreatePackageAsync(CreatePackageCommand command, CancellationToken cancellationToken);

    Task UpdatePackageAsync(UpdatePackageCommand command, CancellationToken cancellationToken);

    Task TogglePackageStatusAsync(Guid id, bool isActive, CancellationToken cancellationToken);
}
