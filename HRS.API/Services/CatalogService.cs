using HRS.API.Contracts.DTOs.Catalog;
using HRS.API.Services.Interfaces;
using HRS.Domain.Entities;
using HRS.Domain.Interfaces;

namespace HRS.API.Services;

public class CatalogService : ICatalogService
{
    private readonly IAvailabilityService _availabilityService;
    private readonly IItemRateRepository _itemRateRepository;
    private readonly IItemRepository _itemRepository;
    private readonly IPackageRateRepository _packageRateRepository;
    private readonly IPackageRepository _packageRepository;

    public CatalogService(
        IAvailabilityService availabilityService,
        IItemRepository itemRepository,
        IItemRateRepository itemRateRepository,
        IPackageRepository packageRepository,
        IPackageRateRepository packageRateRepository)
    {
        _availabilityService = availabilityService;
        _itemRepository = itemRepository;
        _itemRateRepository = itemRateRepository;
        _packageRepository = packageRepository;
        _packageRateRepository = packageRateRepository;
    }

    public async Task<CatalogResponseDto> GetStoreAvailabilityAsync(DateTime startDate, DateTime endDate)
    {
        var rentalDays = Math.Max(1, (endDate.Date - startDate.Date).Days);

        var rootItems = await _itemRepository.GetRootItemsAsync();
        var itemNodes = new List<CatalogItemNodeDto>();
        foreach (var item in rootItems)
            itemNodes.Add(await BuildItemNodeAsync(item, startDate, endDate, rentalDays));

        var storePackages = await BuildPackageNodesAsync(startDate, endDate, rentalDays);

        return new CatalogResponseDto
        {
            PeriodStart = startDate,
            PeriodEnd = endDate,
            Items = itemNodes,
            Packages = storePackages
        };
    }

    private async Task<CatalogItemNodeDto> BuildItemNodeAsync(Item item, DateTime startDate, DateTime endDate, int rentalDays)
    {
        if (item.Children.Count == 0)
        {
            var available = await _availabilityService.GetAvailableQuantityAsync(item.Id, startDate, endDate);
            var dailyRate = await ResolveItemDailyRateAsync(item, rentalDays);

            return new CatalogItemNodeDto
            {
                ItemId = item.Id,
                ItemName = item.Name,
                AvailableQuantity = available,
                DailyRate = dailyRate,
                Children = []
            };
        }

        var childrenDtos = new List<CatalogItemNodeDto>();
        var totalAvailable = 0;

        foreach (var child in item.Children)
        {
            var childNode = await BuildItemNodeAsync(child, startDate, endDate, rentalDays);
            childrenDtos.Add(childNode);
            totalAvailable += childNode.AvailableQuantity;
        }

        var parentDailyRate = await ResolveItemDailyRateAsync(item, rentalDays);

        return new CatalogItemNodeDto
        {
            ItemId = item.Id,
            ItemName = item.Name,
            AvailableQuantity = totalAvailable,
            DailyRate = parentDailyRate,
            Children = childrenDtos
        };
    }

    private async Task<List<CatalogPackageDto>> BuildPackageNodesAsync(DateTime startDate, DateTime endDate, int rentalDays)
    {
        var packages = await _packageRepository.GetAllAsync();
        var storePackages = new List<CatalogPackageDto>();

        foreach (var pkg in packages)
        {
            var pkgWithItems = await _packageRepository.GetByIdWithItemsAsync(pkg.Id);
            if (pkgWithItems == null || pkgWithItems.PackageItems.Count == 0)
                continue;

            var pkgRate = await ResolvePackageDailyRateAsync(pkgWithItems, rentalDays);

            var minAvailablePackages = int.MaxValue;
            var packageItemNodes = new List<CatalogPackageItemNodeDto>();

            foreach (var pkgItem in pkgWithItems.PackageItems)
            {
                if (pkgItem.Item == null)
                    continue;

                var item = pkgItem.Item;

                var childNodes = new List<CatalogItemNodeDto>();
                var availableForParent = 0;

                if (item.Children.Count > 0)
                    foreach (var child in item.Children)
                    {
                        var childAvailable = await _availabilityService.GetAvailableQuantityAsync(child.Id, startDate, endDate);
                        var childRate = await ResolveItemDailyRateAsync(child, rentalDays);

                        childNodes.Add(new CatalogItemNodeDto
                        {
                            ItemId = child.Id,
                            ItemName = child.Name,
                            DailyRate = childRate,
                            AvailableQuantity = childAvailable,
                            Children = []
                        });

                        availableForParent += childAvailable;
                    }
                else
                    availableForParent = await _availabilityService.GetAvailableQuantityAsync(item.Id, startDate, endDate);

                var requiredPerPackage = pkgItem.Quantity;
                var possiblePackages = requiredPerPackage == 0 ? 0 : availableForParent / requiredPerPackage;
                minAvailablePackages = Math.Min(minAvailablePackages, possiblePackages);

                var itemRate = await ResolveItemDailyRateAsync(item, rentalDays);

                packageItemNodes.Add(new CatalogPackageItemNodeDto
                {
                    ItemId = item.Id,
                    ItemName = item.Name,
                    DailyRate = itemRate,
                    AvailableQuantity = availableForParent,
                    Children = childNodes
                });
            }

            storePackages.Add(new CatalogPackageDto
            {
                PackageId = pkgWithItems.Id,
                PackageName = pkgWithItems.Name,
                DailyRate = pkgRate,
                AvailablePackages = Math.Max(0, minAvailablePackages == int.MaxValue ? 0 : minAvailablePackages),
                Items = packageItemNodes
            });
        }

        return storePackages;
    }

    private async Task<decimal> ResolveItemDailyRateAsync(Item item, int rentalDays)
    {
        var rate = await _itemRateRepository.GetApplicableRateAsync(item.Id, rentalDays);
        if (rate != null)
            return rate.DailyRate;

        if (item.ParentId.HasValue)
        {
            var parentRate = await _itemRateRepository.GetApplicableRateAsync(item.ParentId.Value, rentalDays);
            if (parentRate != null)
                return parentRate.DailyRate;
        }

        return item.Price;
    }

    private async Task<decimal> ResolvePackageDailyRateAsync(Package package, int rentalDays)
    {
        var rate = await _packageRateRepository.GetApplicableRateAsync(package.Id, rentalDays);
        return rate?.DailyRate ?? package.BasePrice;
    }
}
