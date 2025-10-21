using HRS.API.Contracts.DTOs.Availability;
using HRS.Shared.Core.Dtos;

namespace HRS.API.Services.Interfaces;

public interface IAvailabilityService
{
    Task<int> GetAvailableQuantityAsync(string itemId, DateTime startDate, DateTime endDate, ItemResponseDto? item = null);

}
