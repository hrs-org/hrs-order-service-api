using HRS.API.Contracts.DTOs.Availability;
using HRS.API.Services.Interfaces;
using HRS.Domain.Interfaces;

namespace HRS.API.Services;

public class AvailabilityService : IAvailabilityService
{
    private readonly IItemMaintenanceRepository _itemMaintenanceRepository;
    private readonly IItemRepository _itemRepository;
    private readonly IPackageRepository _packageRepository;
    private readonly IRentalOrderItemRepository _rentalOrderItemRepository;
    private readonly IRentalOrderPackageItemRepository _rentalOrderPackageItemRepository;

    public AvailabilityService(
        IItemRepository itemRepository,
        IPackageRepository packageRepository,
        IRentalOrderItemRepository rentalOrderItemRepository,
        IRentalOrderPackageItemRepository rentalOrderPackageItemRepository,
        IItemMaintenanceRepository itemMaintenanceRepository)
    {
        _itemRepository = itemRepository;
        _packageRepository = packageRepository;
        _rentalOrderItemRepository = rentalOrderItemRepository;
        _rentalOrderPackageItemRepository = rentalOrderPackageItemRepository;
        _itemMaintenanceRepository = itemMaintenanceRepository;
    }

    public async Task<int> GetAvailableQuantityAsync(int itemId, DateTime startDate, DateTime endDate)
    {
        var item = await _itemRepository.GetByIdAsync(itemId)
                   ?? throw new KeyNotFoundException($"Item {itemId} not found.");

        var reservedFromItems =
            await _rentalOrderItemRepository.GetReservedQuantityAsync(itemId, startDate, endDate);

        var reservedFromPackages =
            await _rentalOrderPackageItemRepository.GetReservedQuantityFromPackagesAsync(itemId, startDate, endDate);

        var repairingQty = await _itemMaintenanceRepository
            .GetRepairingQuantityAsync(itemId);

        var totalReserved = reservedFromItems + reservedFromPackages + repairingQty;
        var available = item.Quantity - totalReserved;

        return available;
    }

    public async Task<IEnumerable<ItemAvailabilityDto>> GetAvailableItemsAsync(DateTime startDate, DateTime endDate)
    {
        var items = await _itemRepository.GetAllAsync();
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
        var packages = await _packageRepository.GetAllAsync();
        var results = new List<PackageAvailabilityDto>();

        foreach (var package in packages)
        {
            var pkg = await _packageRepository.GetByIdWithItemsAsync(package.Id);
            if (pkg == null || pkg.PackageItems.Count == 0)
                continue;

            var minAvailableUnits = int.MaxValue;
            var itemBreakdown = new List<ItemAvailabilityDto>();

            foreach (var pi in pkg.PackageItems)
            {
                var item = pi.Item;
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
