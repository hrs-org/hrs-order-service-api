using HRS.API.Contracts.DTOs.Package;

namespace HRS.API.Services.Interfaces;

public interface IPackageService
{
    Task<IEnumerable<PackageResponseDto>> GetAllAsync();
    Task<PackageResponseDto> GetByIdAsync(int id);
    Task<PackageResponseDto> CreateAsync(AddPackageRequestDto dto);
    Task<PackageResponseDto> UpdateAsync(UpdatePackageRequestDto dto);
    Task DeleteAsync(int id);
}
