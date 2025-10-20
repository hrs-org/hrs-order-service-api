using HRS.API.Contracts.DTOs.Availability;

namespace HRS.API.Services.Interfaces;

public interface IAvailabilityService
{
    Task<int> GetAvailableQuantityAsync(string itemId, DateTime startDate, DateTime endDate);

}
