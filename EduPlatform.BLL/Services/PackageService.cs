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

    public async Task<IReadOnlyList<PackageAdminDto>> GetAllPackagesAsync(CancellationToken cancellationToken)
    {
        var packages = await packageRepository.GetAllAsync(cancellationToken);
        return packages.Select(MapAdmin).ToArray();
    }

    public async Task<Guid> CreatePackageAsync(CreatePackageCommand command, CancellationToken cancellationToken)
    {
        var package = new Package
        {
            Id = Guid.NewGuid(),
            Name = command.Name,
            Description = command.Description,
            Price = command.Price,
            MaxCourses = command.MaxCourses,
            DailyChats = command.DailyChats,
            DurationDays = command.DurationDays,
            IsActive = command.IsActive,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        await packageRepository.AddAsync(package, cancellationToken);
        await packageRepository.SaveChangesAsync(cancellationToken);

        return package.Id;
    }

    public async Task UpdatePackageAsync(UpdatePackageCommand command, CancellationToken cancellationToken)
    {
        var package = await packageRepository.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new ResourceNotFoundException("Không tìm thấy gói cước.");

        package.Name = command.Name;
        package.Description = command.Description;
        package.Price = command.Price;
        package.MaxCourses = command.MaxCourses;
        package.DailyChats = command.DailyChats;
        package.DurationDays = command.DurationDays;
        package.IsActive = command.IsActive;
        package.UpdatedAtUtc = DateTimeOffset.UtcNow;

        packageRepository.Update(package);
        await packageRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task TogglePackageStatusAsync(Guid id, bool isActive, CancellationToken cancellationToken)
    {
        var package = await packageRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new ResourceNotFoundException("Không tìm thấy gói cước.");

        package.IsActive = isActive;
        package.UpdatedAtUtc = DateTimeOffset.UtcNow;

        packageRepository.Update(package);
        await packageRepository.SaveChangesAsync(cancellationToken);
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
            package.DurationDays,
            package.IsActive);
    }

    private static PackageAdminDto MapAdmin(Package package)
    {
        return new PackageAdminDto(
            package.Id,
            package.Name,
            package.Description,
            package.Price,
            package.MaxCourses,
            package.DailyChats,
            package.DurationDays,
            package.IsActive,
            package.CreatedAtUtc);
    }
}
