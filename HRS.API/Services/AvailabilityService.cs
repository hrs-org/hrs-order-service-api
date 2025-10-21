using HRS.API.Contracts.DTOs.Availability;
using HRS.API.Services.Interfaces;
using HRS.Domain.Interfaces;
using HRS.Shared.Core.Dtos;

namespace HRS.API.Services;

public class AvailabilityService : IAvailabilityService
{
    private readonly HttpClient _itemClient;
    private readonly HttpClient _itemMaintenanceClient;
    private readonly IRentalOrderMongoDBRepository _rentalOrderMongoDbRepository;

    public AvailabilityService(
        IRentalOrderMongoDBRepository rentalOrderMongoDbRepository,
        IHttpClientFactory httpClientFactory
        )
    {
        _itemClient = httpClientFactory.CreateClient("ItemService");
        _itemMaintenanceClient = httpClientFactory.CreateClient("ItemMaintenanceService");
        _rentalOrderMongoDbRepository = rentalOrderMongoDbRepository;

    }

    public async Task<int> GetAvailableQuantityAsync(string itemId, DateTime startDate, DateTime endDate, ItemResponseDto? item = null)
    {
        if (item == null)
        {
            var response = await _itemClient.GetFromJsonAsync<ApiResponse<ItemResponseDto>>($"/api/items/{itemId}");
            item = response?.Data ?? throw new KeyNotFoundException($"Item {itemId} not found.");
        }
        var reservedItemsAndPackages = await _rentalOrderMongoDbRepository.GetReservedQuantityAsync(item.Id, startDate, endDate);

        var repairingResponse = await _itemMaintenanceClient.GetFromJsonAsync<ApiResponse<ItemMaintenanceResponseDto>>($"/api/item-maintenances/items/{item.Id}");

        var repairingQty = repairingResponse?.Data?.Quantity ?? 0;

        var totalReserved = reservedItemsAndPackages + repairingQty;

        var available = item.Quantity - totalReserved;
        return available;
    }


}
