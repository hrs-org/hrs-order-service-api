using HRS.API.Contracts.DTOs.Catalog;

namespace HRS.API.Services.Interfaces;

public interface ICatalogService
{
    Task<CatalogResponseDto> GetStoreAvailabilityAsync(DateTime startDate, DateTime endDate);
}
