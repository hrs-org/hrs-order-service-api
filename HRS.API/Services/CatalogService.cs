using System.Collections.Generic;
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
        IHttpClientFactory httpClientFactory)
    {
        _availabilityService = availabilityService;
        _itemClient = httpClientFactory.CreateClient("ItemService");
    }

    public async Task<CatalogResponseDto> GetStoreAvailabilityAsync(DateTime startDate, DateTime endDate, int storeId)
    {
        var rentalDays = Math.Max(1, (endDate.Date - startDate.Date).Days);
        var response = await _itemClient.GetFromJsonAsync<ApiResponse<IEnumerable<ItemResponseDto>>>($"/api/items/store/{storeId}"); // Adjust the endpoint as necessary
        if (response == null || response.Data == null)
            throw new InvalidOperationException("Failed to retrieve root items from Item Service.");
        var rootItems = response.Data;
        var itemNodes = new List<CatalogItemNodeDto>();
        foreach (var item in rootItems)
            itemNodes.Add(await BuildItemNodeAsync(item, startDate, endDate, rentalDays));

        var storePackages = await BuildPackageNodesAsync(startDate, endDate, rentalDays, storeId);

        return new CatalogResponseDto
        {
            PeriodStart = startDate,
            PeriodEnd = endDate,
            Items = itemNodes,
            Packages = storePackages
        };
    }

    private async Task<CatalogItemNodeDto> BuildItemNodeAsync(ItemResponseDto item, DateTime startDate, DateTime endDate, int rentalDays, ItemResponseDto? parentItem = null)
    {
        if (item.Children?.Count == 0)
        {
            var available = await _availabilityService.GetAvailableQuantityAsync(item.Id, startDate, endDate, item);
            var dailyRate = await ResolveItemDailyRateAsync(item, rentalDays, parentItem);

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
            var childNode = await BuildItemNodeAsync(child, startDate, endDate, rentalDays, item);
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

    private async Task<List<CatalogPackageDto>> BuildPackageNodesAsync(DateTime startDate, DateTime endDate, int rentalDays, int storeId)
    {
        var response = await _itemClient.GetFromJsonAsync<ApiResponse<List<PackageResponseDto>>>($"/api/packages/store/{storeId}");
        var storePackages = new List<CatalogPackageDto>();
        if (response == null || response.Data == null || response.Data.Count == 0)
            return new List<CatalogPackageDto>();
        else
        {


            var packages = response.Data;


            foreach (var pkg in packages)
            {
                var pkgWithItems = pkg;
                var pkgRate = await ResolvePackageDailyRateAsync(pkgWithItems, rentalDays);

                var minAvailablePackages = int.MaxValue;
                var packageItemNodes = new List<CatalogPackageItemNodeDto>();
                if (pkgWithItems.Items != null)
                {
                    foreach (var pkgItem in pkgWithItems.Items)
                    {

                        var itemid = pkgItem.ItemId;
                        var responseItem = await _itemClient.GetFromJsonAsync<ApiResponse<ItemResponseDto>>($"/api/items/{itemid}"); // Adjust the endpoint as necessary
                        if (responseItem == null || responseItem.Data == null)
                            throw new InvalidOperationException($"Failed to retrieve item with ID {itemid} for package {pkgWithItems.Id} from Item Service.");
                        var item = responseItem.Data;

                        var childNodes = new List<CatalogItemNodeDto>();
                        var availableForParent = 0;
                        if (item.Children?.Count > 0)
                            foreach (var child in item.Children)
                            {
                                var childAvailable = await _availabilityService.GetAvailableQuantityAsync(child.Id, startDate, endDate, child);
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
                            availableForParent = await _availabilityService.GetAvailableQuantityAsync(item.Id, startDate, endDate, item);

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
        }

        return storePackages;
    }

    private static Task<decimal> ResolveItemDailyRateAsync(ItemResponseDto item, int rentalDays, ItemResponseDto? parentItem = null)
    {

        var response = item;
        var applicableRate = null as ItemRateResponseDto;
        foreach (var rate in response.Rates!)
        {
            if (rate.MinDays <= rentalDays)
            {
                applicableRate = rate;
            }

        }
        if (applicableRate != null)
            return Task.FromResult(applicableRate.DailyRate);
        if (parentItem != null)
        {
            var responseParentRate = null as ItemRateResponseDto;
            foreach (var rate in parentItem.Rates!)
            {
                if (rate.MinDays <= rentalDays)
                {
                    responseParentRate = rate;
                }

            }
            var parentRate = responseParentRate;
            if (parentRate != null)
                return Task.FromResult(parentRate.DailyRate);
        }

        return Task.FromResult(item.Price);
    }

    private static Task<decimal> ResolvePackageDailyRateAsync(PackageResponseDto package, int rentalDays)
    {
        // var rate = await _packageRateRepository.GetApplicableRateAsync(package.Id, rentalDays);

        var applicableRate = null as PackageRateResponseDto;
        foreach (var dummyRate in package.Rates!)
        {
            if (dummyRate.MinDays <= rentalDays)
            {
                applicableRate = dummyRate;
            }

        }
        if (applicableRate != null)
            return Task.FromResult(applicableRate.DailyRate);
        return Task.FromResult(package.BasePrice);
    }
}
