using HRS.API.Contracts.DTOs.Maintenance;

namespace HRS.API.Services.Interfaces;

public interface IItemMaintenanceService
{
    Task<ItemMaintenanceResponseDto> GetAsync(int id);
    Task<IEnumerable<ItemMaintenanceResponseDto>> GetAllAsync();
    Task<ItemMaintenanceResponseDto> MarkAsFixedAsync(ItemMaintenanceRequestDto request);
}
