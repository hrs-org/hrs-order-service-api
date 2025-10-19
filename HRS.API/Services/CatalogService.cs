using HRS.API.Contracts.DTOs.Catalog;
using HRS.API.Services.Interfaces;
using HRS.Domain.Entities;
using HRS.Domain.Interfaces;
using HRS.Shared.Core.Dtos;

namespace HRS.API.Services;

public class CatalogService : ICatalogService
{
    private readonly IAvailabilityService _availabilityService;
    private readonly HttpClient _itemClient;

    public CatalogService(
        IAvailabilityService availabilityService,
        IHttpClientFactory httpClientFactory )
    {
        _availabilityService = availabilityService;
        _itemClient = httpClientFactory.CreateClient("ItemService");
    }

    public async Task<CatalogResponseDto> GetStoreAvailabilityAsync(DateTime startDate, DateTime endDate)
    {
        var rentalDays = Math.Max(1, (endDate.Date - startDate.Date).Days);
        // var rootItems = await _itemRepository.GetRootItemsAsync();
        var response = await _itemClient.GetFromJsonAsync<List<ItemResponseDto>>("/api/item/getrootitems"); // Adjust the endpoint as necessary
        if (response == null || response.Count == 0)
            throw new InvalidOperationException("Failed to retrieve root items from Item Service.");
        var rootItems = response;
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

    private async Task<CatalogItemNodeDto> BuildItemNodeAsync(ItemResponseDto item, DateTime startDate, DateTime endDate, int rentalDays)
    {
        if (item.Children?.Count == 0)
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
        if (item.Children == null) throw new InvalidOperationException("BuildItemNode:Item children cannot be null here.");
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

        // var packages = await _packageRepository.GetAllAsync();
        var response = await _itemClient.GetFromJsonAsync<List<PackageResponseDto>>("/api/package/getall"); // Adjust the endpoint as necessary
        if (response == null || response.Count == 0)
            throw new InvalidOperationException("Failed to retrieve packages from Item Service.");
        var packages = response;
        var storePackages = new List<CatalogPackageDto>();

        foreach (var pkg in packages)
        {   // var pkgWithItems = await _packageRepository.GetByIdWithItemsAsync(pkg.Id);
            var pkgWithItems = await _itemClient.GetFromJsonAsync<PackageResponseDto>($"/api/package/getbyidwithitems/{pkg.Id}"); // Adjust the endpoint as necessary
            if (pkgWithItems == null || pkgWithItems.Items == null || pkgWithItems.Items.Count == 0) throw new InvalidOperationException($"Package with ID {pkg.Id} not found. Or it has no items.");

            var pkgRate = await ResolvePackageDailyRateAsync(pkgWithItems, rentalDays);

            var minAvailablePackages = int.MaxValue;
            var packageItemNodes = new List<CatalogPackageItemNodeDto>();

            foreach (var pkgItem in pkgWithItems.Items)
            {

                var itemid = pkgItem.ItemId;
                var responseItem = await _itemClient.GetFromJsonAsync<ItemResponseDto>($"/api/item/getbyid/{itemid}"); // Adjust the endpoint as necessary
                var item = responseItem;
                if (item == null)
                    throw new InvalidOperationException($"Item with ID {itemid} not found for package {pkgWithItems.Id}.");
                var childNodes = new List<CatalogItemNodeDto>();
                var availableForParent = 0;

                if (item.Children?.Count > 0)
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

    private async Task<decimal> ResolveItemDailyRateAsync(ItemResponseDto item, int rentalDays)
    {
        // var rate = await _itemRateRepository.GetApplicableRateAsync(item.Id, rentalDays);
        var response = await _itemClient.GetFromJsonAsync<ItemRateResponseDto>($"/api/itemrate/getapplicablerate/{item.Id}/{rentalDays}"); // Adjust the endpoint as necessary
        var rate = response;
        if (rate != null)
            return rate.DailyRate;
        var itemResponse = await _itemClient.GetFromJsonAsync<ItemResponseDto>($"/api/item/getbyidwithchildren/{item.Id}"); // Adjust the endpoint as necessary
        var ParentId = itemResponse?.Id;
        if (ParentId.HasValue)
        {
            var responseParentRate = await _itemClient.GetFromJsonAsync<ItemRateResponseDto>($"/api/itemrate/getapplicablerate/{ParentId.Value}/{rentalDays}"); // Adjust the endpoint as necessary
            var parentRate = responseParentRate;
            // var parentRate = await _itemRateRepository.GetApplicableRateAsync(ParentId.Value, rentalDays);
            if (parentRate != null)
                return parentRate.DailyRate;
        }

        return item.Price;
    }

    private async Task<decimal> ResolvePackageDailyRateAsync(PackageResponseDto package, int rentalDays)
    {
        // var rate = await _packageRateRepository.GetApplicableRateAsync(package.Id, rentalDays);
        var response = await _itemClient.GetFromJsonAsync<PackageRateResponseDto>($"/api/packagerate/getapplicablerate/{package.Id}/{rentalDays}"); // Adjust the endpoint as necessary
        var rate = response;
        return rate?.DailyRate ?? package.BasePrice;
    }
}
