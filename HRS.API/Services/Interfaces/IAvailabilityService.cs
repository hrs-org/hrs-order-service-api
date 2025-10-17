using HRS.API.Contracts.DTOs.Availability;

namespace HRS.API.Services.Interfaces;

public interface IAvailabilityService
{
    Task<int> GetAvailableQuantityAsync(int itemId, DateTime startDate, DateTime endDate);
    Task<IEnumerable<ItemAvailabilityDto>> GetAvailableItemsAsync(DateTime startDate, DateTime endDate);
    Task<IEnumerable<PackageAvailabilityDto>> GetAvailablePackagesAsync(DateTime startDate, DateTime endDate);
}
