using HRS.API.Contracts.DTOs.Availability;
using HRS.API.Services.Interfaces;
using HRS.Domain.Interfaces;
using HRS.Shared.Core.Dtos;

namespace HRS.API.Services;

public class AvailabilityService : IAvailabilityService
{
    private readonly HttpClient _itemClient;
    private readonly HttpClient _itemMaintenanceClient;
    private readonly IRentalOrderItemMongoDBRepository _rentalOrderItemRepository;
    private readonly IRentalOrderPackageItemMongoDBRepository _rentalOrderPackageItemRepository;

    public AvailabilityService(

        IRentalOrderItemMongoDBRepository rentalOrderItemRepository,
        IRentalOrderPackageItemMongoDBRepository rentalOrderPackageItemRepository,
        IHttpClientFactory httpClientFactory
        )
    {
        _itemClient = httpClientFactory.CreateClient("ItemService");
        _itemMaintenanceClient = httpClientFactory.CreateClient("ItemMaintenanceService");
        _rentalOrderItemRepository = rentalOrderItemRepository;
        _rentalOrderPackageItemRepository = rentalOrderPackageItemRepository;

    }

    public async Task<int> GetAvailableQuantityAsync(int itemId, DateTime startDate, DateTime endDate)
    {
        // var item = await _itemRepository.GetByIdAsync(itemId)
        var response = await _itemClient.GetFromJsonAsync<ItemResponseDto>($"/api/item/getbyid/{itemId}"); // Adjust the endpoint as necessary
        if (response == null)
            throw new KeyNotFoundException($"Item {itemId} not found.");
        var item = response;
        var reservedFromItems =
            await _rentalOrderItemRepository.GetReservedQuantityAsync(itemId, startDate, endDate);

        var reservedFromPackages =
            await _rentalOrderPackageItemRepository.GetReservedQuantityFromPackagesAsync(itemId, startDate, endDate);

        // var repairingQty = await _itemMaintenanceRepository
        //     .GetRepairingQuantityAsync(itemId);
        var repairingResponse = await _itemMaintenanceClient.GetFromJsonAsync<int>($"/api/itemmaintenance/getrepairingquantity/{itemId}"); // Adjust the endpoint as necessary
        var repairingQty = repairingResponse;
        var totalReserved = reservedFromItems + reservedFromPackages + repairingQty;
        var available = item.Quantity - totalReserved;

        return available;
    }

    public async Task<IEnumerable<ItemAvailabilityDto>> GetAvailableItemsAsync(DateTime startDate, DateTime endDate)
    {
        // var items = await _itemRepository.GetAllAsync();
        var response = await _itemClient.GetFromJsonAsync<List<ItemResponseDto>>("/api/item"); // Adjust the endpoint as necessary
        if (response == null)
            throw new InvalidOperationException("Failed to retrieve items from Item Service.");
        var items = response;
        var results = new List<ItemAvailabilityDto>();

        foreach (var item in items)
        {
            var available = await GetAvailableQuantityAsync(item.Id, startDate, endDate);
            var totalReserved = item.Quantity - available;

            results.Add(new ItemAvailabilityDto
            {
                ItemId = item.Id,
                ItemName = item.Name,
                TotalQuantity = item.Quantity,
                ReservedQuantity = totalReserved,
                AvailableQuantity = available
            });
        }

        return results.OrderByDescending(r => r.AvailableQuantity);
    }

    public async Task<IEnumerable<PackageAvailabilityDto>> GetAvailablePackagesAsync(DateTime startDate, DateTime endDate)
    {
        // var packages = await _packageRepository.GetAllAsync();
        var response = await _itemClient.GetFromJsonAsync<List<PackageResponseDto>>("/api/package"); // Adjust the endpoint as necessary
        if (response == null || response.Count == 0)
            throw new InvalidOperationException("Failed to retrieve packages from Item Service.");
        var packages = response;
        var results = new List<PackageAvailabilityDto>();

        foreach (var package in packages)
        {   var pkg = await _itemClient.GetFromJsonAsync<PackageResponseDto>($"/api/package/getbyidwithitems/{package.Id}"); // Adjust the endpoint as necessary
            // var pkg = await _packageRepository.GetByIdWithItemsAsync(package.Id);
            if (pkg == null || pkg.Items == null || pkg.Items.Count == 0)
                continue;

            var minAvailableUnits = int.MaxValue;
            var itemBreakdown = new List<ItemAvailabilityDto>();

            foreach (var pi in pkg.Items)
            {
                var itemid = pi.ItemId;
                var responseItem = await _itemClient.GetFromJsonAsync<ItemResponseDto>($"/api/item/getbyid/{itemid}"); // Adjust the endpoint as necessary
                var item = responseItem;
                if (item == null) continue;

                var available = await GetAvailableQuantityAsync(item.Id, startDate, endDate);
                var requiredPerPackage = pi.Quantity;

                var possiblePackages = requiredPerPackage == 0 ? 0 : available / requiredPerPackage;
                minAvailableUnits = Math.Min(minAvailableUnits, possiblePackages);

                itemBreakdown.Add(new ItemAvailabilityDto
                {
                    ItemId = item.Id,
                    ItemName = item.Name,
                    TotalQuantity = item.Quantity,
                    ReservedQuantity = item.Quantity - available,
                    AvailableQuantity = available
                });
            }

            results.Add(new PackageAvailabilityDto
            {
                PackageId = package.Id,
                PackageName = package.Name,
                AvailablePackages = Math.Max(0, minAvailableUnits == int.MaxValue ? 0 : minAvailableUnits),
                ItemBreakdown = itemBreakdown
            });
        }

        return results;
    }
}
