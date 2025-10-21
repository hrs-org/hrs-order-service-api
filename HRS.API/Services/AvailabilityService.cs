using HRS.API.Contracts.DTOs.Availability;
using HRS.API.Services.Interfaces;
using HRS.Domain.Interfaces;
using HRS.Shared.Core.Dtos;

namespace HRS.API.Services;

public class AvailabilityService : IAvailabilityService
{
    private readonly HttpClient _itemClient;
    private readonly HttpClient _itemMaintenanceClient;
    private readonly IRentalOrderMongoDBRepository _rentalOrderMongoDBRepository;

    public AvailabilityService(

        IRentalOrderMongoDBRepository rentalOrderMongoDBRepository,
        IHttpClientFactory httpClientFactory
        )
    {
        _itemClient = httpClientFactory.CreateClient("ItemService");
        _itemMaintenanceClient = httpClientFactory.CreateClient("ItemMaintenanceService");
        _rentalOrderMongoDBRepository = rentalOrderMongoDBRepository;

    }

    public async Task<int> GetAvailableQuantityAsync(string itemId, DateTime startDate, DateTime endDate, ItemResponseDto? item = null)
    {   if(item == null)
        {
            var response = await _itemClient.GetFromJsonAsync<ApiResponse<ItemResponseDto>>($"/api/items/{itemId}"); // Adjust the endpoint as necessary
            if (response == null || response.Data == null)
                throw new KeyNotFoundException($"Item {itemId} not found.");
            item = response.Data;
        }
        var reservedItemsAndPackages = await _rentalOrderMongoDBRepository.GetReservedQuantityAsync(item.Id, startDate, endDate);
        // var repairingQty = await _itemMaintenanceRepository
        //     .GetRepairingQuantityAsync(itemId);
        var repairingQty = 0;
        var repairingResponse = await _itemMaintenanceClient.GetFromJsonAsync<ApiResponse<ItemMaintenanceResponseDto>>($"/api/item-maintenances/items/{item.Id}"); // Adjust the endpoint as necessary
        if (repairingResponse == null || repairingResponse.Data == null)
        {
            repairingQty = 0;
        }
        else
        {
            repairingQty = repairingResponse.Data.Quantity;
        }
        var totalReserved = reservedItemsAndPackages + repairingQty;
        var available = item.Quantity - totalReserved;
        return available;
    }


}
