using EduPlatform.BLL.DTOs.Packages;
using EduPlatform.BLL.Exceptions;
using EduPlatform.BLL.Interfaces;
using EduPlatform.DAL.Entities;
using EduPlatform.DAL.Repositories;

namespace EduPlatform.BLL.Services;

public sealed class PackageService(IPackageRepository packageRepository) : IPackageService
{
    public async Task<IReadOnlyList<PackageDto>> GetActivePackagesAsync(CancellationToken cancellationToken)
    {
        var packages = await packageRepository.GetActivePackagesAsync(cancellationToken);
        return packages.Select(Map).ToArray();
    }

    public async Task<PackageDto> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var package = await packageRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new ResourceNotFoundException("Không tìm thấy gói cước.");
            
        return Map(package);
    }

    private static PackageDto Map(Package package)
    {
        return new PackageDto(
            package.Id,
            package.Name,
            package.Description,
            package.Price,
            package.MaxCourses,
            package.DailyChats,
            package.DurationDays);
    }
}
