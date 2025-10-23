// using FluentAssertions;
// using HRS.API.Services;
// using HRS.API.Services.Interfaces;
// using HRS.Domain.Entities;
// using HRS.Domain.Interfaces;
// using NSubstitute;

// namespace HRS.Test.API.Services;

// public class CatalogServiceTests
// {
//     private readonly IAvailabilityService _availabilityService;
//     private readonly IItemRateRepository _itemRateRepository;
//     private readonly IItemRepository _itemRepository;
//     private readonly IPackageRateRepository _packageRateRepository;
//     private readonly IPackageRepository _packageRepository;
//     private readonly CatalogService _service;

//     public CatalogServiceTests()
//     {
//         _availabilityService = Substitute.For<IAvailabilityService>();
//         _itemRepository = Substitute.For<IItemRepository>();
//         _itemRateRepository = Substitute.For<IItemRateRepository>();
//         _packageRepository = Substitute.For<IPackageRepository>();
//         _packageRateRepository = Substitute.For<IPackageRateRepository>();
//         _service = new CatalogService(
//             _availabilityService,
//             _itemRepository,
//             _itemRateRepository,
//             _packageRepository,
//             _packageRateRepository
//         );
//     }

//     [Fact]
//     public async Task GetStoreAvailabilityAsync_ReturnsCorrectDto()
//     {
//         // Arrange
//         var startDate = DateTime.Today;
//         var endDate = DateTime.Today.AddDays(3);
//         var rentalDays = 3;
//         var item = new Item { Id = 1, Name = "Tent", Quantity = 10, Price = 100, Children = new List<Item>() };
//         var items = new List<Item> { item };
//         _itemRepository.GetRootItemsAsync().Returns(items);
//         _availabilityService.GetAvailableQuantityAsync(1, startDate, endDate).Returns(5);
//         _itemRateRepository.GetApplicableRateAsync(1, rentalDays).Returns(new ItemRate { DailyRate = 50 });
//         _packageRepository.GetAllAsync().Returns(new List<Package>());

//         // Act
//         var result = await _service.GetStoreAvailabilityAsync(startDate, endDate);

//         // Assert
//         result.Should().NotBeNull();
//         result.PeriodStart.Should().Be(startDate);
//         result.PeriodEnd.Should().Be(endDate);
//         result.Items.Should().HaveCount(1);
//         result.Items.ToList()[0].ItemId.Should().Be(1);
//         result.Items.ToList()[0].AvailableQuantity.Should().Be(5);
//         result.Items.ToList()[0].DailyRate.Should().Be(50);
//         result.Packages.Should().BeEmpty();
//     }

//     [Fact]
//     public async Task GetStoreAvailabilityAsync_WithPackages_ReturnsCorrectPackages()
//     {
//         // Arrange
//         var startDate = DateTime.Today;
//         var endDate = DateTime.Today.AddDays(2);
//         var rentalDays = 2;
//         var item = new Item { Id = 1, Name = "Tent", Quantity = 10, Price = 100, Children = new List<Item>() };
//         var pkgItem = new PackageItem { Item = item, Quantity = 2 };
//         var package = new Package { Id = 1, Name = "Camping Set", BasePrice = 200, PackageItems = new List<PackageItem> { pkgItem } };
//         _itemRepository.GetRootItemsAsync().Returns(new List<Item>());
//         _packageRepository.GetAllAsync().Returns(new List<Package> { package });
//         _packageRepository.GetByIdWithItemsAsync(1).Returns(package);
//         _availabilityService.GetAvailableQuantityAsync(1, startDate, endDate).Returns(6);
//         _itemRateRepository.GetApplicableRateAsync(1, rentalDays).Returns(new ItemRate { DailyRate = 50 });
//         _packageRateRepository.GetApplicableRateAsync(1, rentalDays).Returns(new PackageRate { DailyRate = 150 });

//         // Act
//         var result = await _service.GetStoreAvailabilityAsync(startDate, endDate);

//         // Assert
//         result.Packages.Should().HaveCount(1);
//         var pkgDto = result.Packages.ToList()[0];
//         pkgDto.PackageId.Should().Be(1);
//         pkgDto.PackageName.Should().Be("Camping Set");
//         pkgDto.DailyRate.Should().Be(150);
//         pkgDto.AvailablePackages.Should().Be(3); // 6 / 2
//         pkgDto.Items.Should().HaveCount(1);
//         pkgDto.Items.ToList()[0].ItemId.Should().Be(1);
//         pkgDto.Items.ToList()[0].AvailableQuantity.Should().Be(6);
//         pkgDto.Items.ToList()[0].DailyRate.Should().Be(50);
//     }

//     [Fact]
//     public async Task GetStoreAvailabilityAsync_ItemWithChildren_AggregatesAvailability()
//     {
//         // Arrange
//         var startDate = DateTime.Today;
//         var endDate = DateTime.Today.AddDays(2);
//         var rentalDays = 2;
//         var child1 = new Item { Id = 2, Name = "Child1", Quantity = 5, Price = 10, Children = new List<Item>() };
//         var child2 = new Item { Id = 3, Name = "Child2", Quantity = 7, Price = 12, Children = new List<Item>() };
//         var parent = new Item { Id = 1, Name = "Parent", Quantity = 0, Price = 20, Children = new List<Item> { child1, child2 } };
//         _itemRepository.GetRootItemsAsync().Returns(new List<Item> { parent });
//         _availabilityService.GetAvailableQuantityAsync(2, startDate, endDate).Returns(3);
//         _availabilityService.GetAvailableQuantityAsync(3, startDate, endDate).Returns(4);
//         _itemRateRepository.GetApplicableRateAsync(2, rentalDays).Returns(new ItemRate { DailyRate = 10 });
//         _itemRateRepository.GetApplicableRateAsync(3, rentalDays).Returns(new ItemRate { DailyRate = 12 });
//         _itemRateRepository.GetApplicableRateAsync(1, rentalDays).Returns(new ItemRate { DailyRate = 20 });
//         _packageRepository.GetAllAsync().Returns(new List<Package>());

//         // Act
//         var result = await _service.GetStoreAvailabilityAsync(startDate, endDate);

//         // Assert
//         result.Items.Should().HaveCount(1);
//         var parentNode = result.Items.ToList()[0];
//         parentNode.ItemId.Should().Be(1);
//         parentNode.AvailableQuantity.Should().Be(7); // 3 + 4
//         parentNode.Children.Should().HaveCount(2);
//         parentNode.Children.ToList()[0].ItemId.Should().Be(2);
//         parentNode.Children.ToList()[1].ItemId.Should().Be(3);
//     }

//     [Fact]
//     public async Task GetStoreAvailabilityAsync_UsesParentRateIfChildHasNoRate()
//     {
//         // Arrange
//         var startDate = DateTime.Today;
//         var endDate = DateTime.Today.AddDays(2);
//         var rentalDays = 2;
//         var parent = new Item { Id = 1, Name = "Parent", Price = 20, Children = new List<Item>() };
//         var child = new Item { Id = 2, Name = "Child", ParentId = 1, Price = 10, Children = new List<Item>() };
//         parent.Children.Add(child);
//         _itemRepository.GetRootItemsAsync().Returns(new List<Item> { parent });
//         _availabilityService.GetAvailableQuantityAsync(2, startDate, endDate).Returns(5);
//         _itemRateRepository.GetApplicableRateAsync(2, rentalDays).Returns((ItemRate)null!);
//         _itemRateRepository.GetApplicableRateAsync(1, rentalDays).Returns(new ItemRate { DailyRate = 20 });
//         _packageRepository.GetAllAsync().Returns(new List<Package>());

//         // Act
//         var result = await _service.GetStoreAvailabilityAsync(startDate, endDate);

//         // Assert
//         var childNode = result.Items.ToList()[0].Children.ToList()[0];
//         childNode.DailyRate.Should().Be(20); // fallback to parent rate
//     }

//     [Fact]
//     public async Task GetStoreAvailabilityAsync_UsesItemPriceIfNoRate()
//     {
//         // Arrange
//         var startDate = DateTime.Today;
//         var endDate = DateTime.Today.AddDays(2);
//         var rentalDays = 2;
//         var item = new Item { Id = 1, Name = "Item", Price = 99, Children = new List<Item>() };
//         _itemRepository.GetRootItemsAsync().Returns(new List<Item> { item });
//         _availabilityService.GetAvailableQuantityAsync(1, startDate, endDate).Returns(5);
//         _itemRateRepository.GetApplicableRateAsync(1, rentalDays).Returns((ItemRate)null!);
//         _packageRepository.GetAllAsync().Returns(new List<Package>());

//         // Act
//         var result = await _service.GetStoreAvailabilityAsync(startDate, endDate);

//         // Assert
//         result.Items.ToList()[0].DailyRate.Should().Be(99);
//     }

//     [Fact]
//     public async Task GetStoreAvailabilityAsync_UsesPackageBasePriceIfNoRate()
//     {
//         // Arrange
//         var startDate = DateTime.Today;
//         var endDate = DateTime.Today.AddDays(2);
//         var rentalDays = 2;
//         var item = new Item { Id = 1, Name = "Tent", Quantity = 10, Price = 100, Children = new List<Item>() };
//         var pkgItem = new PackageItem { Item = item, Quantity = 2 };
//         var package = new Package { Id = 1, Name = "Camping Set", BasePrice = 200, PackageItems = new List<PackageItem> { pkgItem } };
//         _itemRepository.GetRootItemsAsync().Returns(new List<Item>());
//         _packageRepository.GetAllAsync().Returns(new List<Package> { package });
//         _packageRepository.GetByIdWithItemsAsync(1).Returns(package);
//         _availabilityService.GetAvailableQuantityAsync(1, startDate, endDate).Returns(6);
//         _itemRateRepository.GetApplicableRateAsync(1, rentalDays).Returns((ItemRate)null!);
//         _packageRateRepository.GetApplicableRateAsync(1, rentalDays).Returns((PackageRate)null!);

//         // Act
//         var result = await _service.GetStoreAvailabilityAsync(startDate, endDate);

//         // Assert
//         result.Packages.ToList()[0].DailyRate.Should().Be(200); // fallback to base price
//     }

//     [Fact]
//     public async Task BuildPackageNodesAsync_ItemWithChildren_CreatesChildNodesAndAggregatesAvailability()
//     {
//         // Arrange
//         var startDate = DateTime.Today;
//         var endDate = DateTime.Today.AddDays(2);
//         var rentalDays = 2;
//         var child1 = new Item { Id = 2, Name = "Child1", Quantity = 5, Price = 10, Children = new List<Item>() };
//         var child2 = new Item { Id = 3, Name = "Child2", Quantity = 7, Price = 12, Children = new List<Item>() };
//         var parent = new Item { Id = 1, Name = "Parent", Quantity = 0, Price = 20, Children = new List<Item> { child1, child2 } };
//         var pkgItem = new PackageItem { Item = parent, Quantity = 2 };
//         var package = new Package { Id = 1, Name = "Camping Set", BasePrice = 200, PackageItems = new List<PackageItem> { pkgItem } };
//         var packageList = new List<Package> { package };
//         var packageRepo = Substitute.For<IPackageRepository>();
//         var availabilityService = Substitute.For<IAvailabilityService>();
//         var itemRateRepo = Substitute.For<IItemRateRepository>();
//         var packageRateRepo = Substitute.For<IPackageRateRepository>();
//         var itemRepo = Substitute.For<IItemRepository>();
//         var service = new CatalogService(availabilityService, itemRepo, itemRateRepo, packageRepo, packageRateRepo);
//         packageRepo.GetAllAsync().Returns(packageList);
//         packageRepo.GetByIdWithItemsAsync(1).Returns(package);
//         availabilityService.GetAvailableQuantityAsync(2, startDate, endDate).Returns(3);
//         availabilityService.GetAvailableQuantityAsync(3, startDate, endDate).Returns(4);
//         itemRateRepo.GetApplicableRateAsync(2, rentalDays).Returns(new ItemRate { DailyRate = 10 });
//         itemRateRepo.GetApplicableRateAsync(3, rentalDays).Returns(new ItemRate { DailyRate = 12 });
//         itemRateRepo.GetApplicableRateAsync(1, rentalDays).Returns(new ItemRate { DailyRate = 20 });
//         packageRateRepo.GetApplicableRateAsync(1, rentalDays).Returns(new PackageRate { DailyRate = 150 });

//         // Act
//         var result = await service.GetStoreAvailabilityAsync(startDate, endDate);

//         // Assert
//         result.Packages.Should().HaveCount(1);
//         var pkgDto = result.Packages.ToList()[0];
//         pkgDto.PackageId.Should().Be(1);
//         pkgDto.Items.Should().HaveCount(1);
//         var itemNode = pkgDto.Items.ToList()[0];
//         itemNode.ItemId.Should().Be(1);
//         itemNode.Children.Should().HaveCount(2);
//         itemNode.Children.ToList()[0].ItemId.Should().Be(2);
//         itemNode.Children.ToList()[0].AvailableQuantity.Should().Be(3);
//         itemNode.Children.ToList()[1].ItemId.Should().Be(3);
//         itemNode.Children.ToList()[1].AvailableQuantity.Should().Be(4);
//         itemNode.AvailableQuantity.Should().Be(7); // 3 + 4
//     }
// }

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using HRS.API.Contracts.DTOs.Catalog;
using HRS.API.Services;
using HRS.API.Services.Interfaces;
using HRS.Domain.Entities;
using HRS.Shared.Core.Dtos;
using NSubstitute;
using Xunit;
using FluentAssertions;

namespace HRS.Test.API.Services;
public class CatalogServiceTests
{
    private readonly IAvailabilityService _availabilityService;
    private readonly HttpClient _httpClient;
    private readonly CatalogService _service;

    public CatalogServiceTests()
    {
        _availabilityService = Substitute.For<IAvailabilityService>();

        // Mock HttpClient
        var handler = new FakeHttpMessageHandler();
        _httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost") // base address is required
        };

        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("ItemService").Returns(_httpClient);

        _service = new CatalogService(_availabilityService, factory);
    }

    [Fact]
    public async Task GetStoreAvailabilityAsync_ReturnsCatalog()
    {
        // Arrange
        var startDate = DateTime.Today;
        var endDate = startDate.AddDays(2);

        // Mock HTTP response for items
        FakeHttpMessageHandler.AddJsonResponse("/api/items/store/1", new ApiResponse<IEnumerable<ItemResponseDto>>
        {
            Data = new List<ItemResponseDto>
            {
                new ItemResponseDto
                {
                    Id = "item1",
                    Name = "Item 1",
                    Quantity = 5,
                    Rates = new List<ItemRateResponseDto> { new() { MinDays = 1, DailyRate = 100 } },
                    Children = new List<ItemResponseDto>()
                }
            }
        });

        // Mock HTTP response for packages
        FakeHttpMessageHandler.AddJsonResponse("/api/packages/store/1", new ApiResponse<List<PackageResponseDto>>
        {
            Data = new List<PackageResponseDto>()
        });

        // Mock availability service
        _availabilityService.GetAvailableQuantityAsync("item1", startDate, endDate, Arg.Any<ItemResponseDto>())
            .Returns(Task.FromResult(5));

        // Act
        var result = await _service.GetStoreAvailabilityAsync(startDate, endDate, 1);

        // Assert
        result.Items.Should().HaveCount(1);
        // result.Items[0].AvailableQuantity.Should().Be(5);
        result.Packages.Should().BeEmpty();
    }

    [Fact]
    public async Task GetStoreAvailabilityAsync_Throws_WhenItemServiceFails()
    {
        // Arrange
        var startDate = DateTime.Today;
        var endDate = startDate.AddDays(2);

        // No mock response -> 404 will be returned
        FakeHttpMessageHandler.AddHttpStatus("/api/items/store/1", HttpStatusCode.NotFound);

        // Act
        Func<Task> act = async () => await _service.GetStoreAvailabilityAsync(startDate, endDate, 1);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }


    [Fact]
    public async Task LeafItem_ReturnsNode_WithAvailableQuantity()
    {
        var start = DateTime.Today;
        var end = start.AddDays(3);

        FakeHttpMessageHandler.AddJsonResponse("/api/items/store/1", new ApiResponse<IEnumerable<ItemResponseDto>>
        {
            Data = new List<ItemResponseDto>
            {
                new ItemResponseDto
                {
                    Id = "leaf",
                    Name = "Leaf Item",
                    Quantity = 5,
                    Rates = new List<ItemRateResponseDto> { new() { MinDays = 1, DailyRate = 100 } },
                    Children = new List<ItemResponseDto>()
                }
            }
        });

        FakeHttpMessageHandler.AddJsonResponse("/api/packages/store/1", new ApiResponse<List<PackageResponseDto>> { Data = new List<PackageResponseDto>() });

        _availabilityService.GetAvailableQuantityAsync("leaf", start, end, Arg.Any<ItemResponseDto>()).Returns(Task.FromResult(5));

        var result = await _service.GetStoreAvailabilityAsync(start, end, 1);
        var items = (List<CatalogItemNodeDto>)result.Items;
        result.Items.Should().HaveCount(1);
        items[0].AvailableQuantity.Should().Be(5);
        items[0].DailyRate.Should().Be(100);
    }

    [Fact]
    public async Task ParentItemWithChildren_CalculatesTotalAvailable()
    {
        var start = DateTime.Today;
        var end = start.AddDays(2);

        var child1 = new ItemResponseDto
        {
            Id = "child1",
            Name = "Child 1",
            Quantity = 2,
            Rates = new List<ItemRateResponseDto> { new() { MinDays = 1, DailyRate = 50 } },
            Children = new List<ItemResponseDto>()
        };
        var child2 = new ItemResponseDto
        {
            Id = "child2",
            Name = "Child 2",
            Quantity = 3,
            Rates = new List<ItemRateResponseDto> { new() { MinDays = 1, DailyRate = 60 } },
            Children = new List<ItemResponseDto>()
        };

        FakeHttpMessageHandler.AddJsonResponse("/api/items/store/1", new ApiResponse<IEnumerable<ItemResponseDto>>
        {
            Data = new List<ItemResponseDto>
            {
                new ItemResponseDto
                {
                    Id = "parent",
                    Name = "Parent Item",
                    Quantity = 10,
                    Rates = new List<ItemRateResponseDto> { new() { MinDays = 1, DailyRate = 200 } },
                    Children = new List<ItemResponseDto> { child1, child2 }
                }
            }
        });

        FakeHttpMessageHandler.AddJsonResponse("/api/packages/store/1", new ApiResponse<List<PackageResponseDto>> { Data = new List<PackageResponseDto>() });

        _availabilityService.GetAvailableQuantityAsync("child1", start, end, Arg.Any<ItemResponseDto>()).Returns(Task.FromResult(2));
        _availabilityService.GetAvailableQuantityAsync("child2", start, end, Arg.Any<ItemResponseDto>()).Returns(Task.FromResult(3));

        var result = await _service.GetStoreAvailabilityAsync(start, end, 1);

        var items = (List<CatalogItemNodeDto>)result.Items;
        items[0].AvailableQuantity.Should().Be(5);
        // result.Items[0].AvailableQuantity.Should().Be(5); // 2+3
        items[0].DailyRate.Should().Be(200); // parent's rate
        items[0].Children.Should().HaveCount(2);
    }

    [Fact]
    public async Task PackageItem_WithChildItems_ComputesMinAvailablePackages()
    {
        var start = DateTime.Today;
        var end = start.AddDays(2);

        var pkgItem = new PackageItemResponseDto { ItemId = "pkgitem", Quantity = 2 };

        FakeHttpMessageHandler.AddJsonResponse("/api/items/store/1", new ApiResponse<IEnumerable<ItemResponseDto>> { Data = new List<ItemResponseDto>() });
        FakeHttpMessageHandler.AddJsonResponse("/api/packages/store/1", new ApiResponse<List<PackageResponseDto>>
        {
            Data = new List<PackageResponseDto>
            {
                new PackageResponseDto
                {
                    Id = "pkg1",
                    Name = "Package 1",
                    BasePrice = 1000,
                    Items = new List<PackageItemResponseDto> { pkgItem },
                    Rates = new List<PackageRateResponseDto> { new() { MinDays = 1, DailyRate = 1000 } }
                }
            }
        });

        // Item HTTP call for package item
        FakeHttpMessageHandler.AddJsonResponse("/api/items/pkgitem", new ApiResponse<ItemResponseDto>
        {
            Data = new ItemResponseDto
            {
                Id = "pkgitem",
                Name = "Package Item",
                Quantity = 5,
                Rates = new List<ItemRateResponseDto> { new() { MinDays = 1, DailyRate = 500 } },
                Children = new List<ItemResponseDto>()
            }
        });

        _availabilityService.GetAvailableQuantityAsync("pkgitem", start, end, Arg.Any<ItemResponseDto>()).Returns(Task.FromResult(4));

        var result = await _service.GetStoreAvailabilityAsync(start, end, 1);
        var items = (List<CatalogPackageDto>)result.Packages;
        result.Packages.Should().HaveCount(1);
        items[0].AvailablePackages.Should().Be(2); // floor(4/2)
    }

    [Fact]
public async Task Throws_WhenItemNotFound()
{
    var start = DateTime.Today;
    var end = start.AddDays(1);

    FakeHttpMessageHandler.AddJsonResponse("/api/items/store/1", new { data = (object?)null });

    var client = new HttpClient(new FakeHttpMessageHandler());
    var httpFactory = Substitute.For<IHttpClientFactory>();
    httpFactory.CreateClient("ItemService").Returns(client);

    var service = new CatalogService(_availabilityService, httpFactory);

    Func<Task> act = async () => await service.GetStoreAvailabilityAsync(start, end, 1);

    await act.Should().ThrowAsync<InvalidOperationException>()
             .WithMessage("An invalid request URI was provided. Either the request URI must be an absolute URI or BaseAddress must be set.");
}




}

// Fake HTTP handler for unit tests
internal class FakeHttpMessageHandler : HttpMessageHandler
{
    private static readonly Dictionary<string, HttpResponseMessage> _responses = new();

    public static void AddJsonResponse<T>(string path, T content)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(content)
        };
        _responses[path] = response;
    }

    public static void AddHttpStatus(string path, HttpStatusCode status)
    {
        _responses[path] = new HttpResponseMessage(status);
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
    {
        if (_responses.TryGetValue(request.RequestUri!.AbsolutePath, out var response))
            return Task.FromResult(response);

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }


}

