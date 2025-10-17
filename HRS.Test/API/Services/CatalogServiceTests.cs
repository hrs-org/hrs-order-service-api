using FluentAssertions;
using HRS.API.Services;
using HRS.API.Services.Interfaces;
using HRS.Domain.Entities;
using HRS.Domain.Interfaces;
using NSubstitute;

namespace HRS.Test.API.Services;

public class CatalogServiceTests
{
    private readonly IAvailabilityService _availabilityService;
    private readonly IItemRateRepository _itemRateRepository;
    private readonly IItemRepository _itemRepository;
    private readonly IPackageRateRepository _packageRateRepository;
    private readonly IPackageRepository _packageRepository;
    private readonly CatalogService _service;

    public CatalogServiceTests()
    {
        _availabilityService = Substitute.For<IAvailabilityService>();
        _itemRepository = Substitute.For<IItemRepository>();
        _itemRateRepository = Substitute.For<IItemRateRepository>();
        _packageRepository = Substitute.For<IPackageRepository>();
        _packageRateRepository = Substitute.For<IPackageRateRepository>();
        _service = new CatalogService(
            _availabilityService,
            _itemRepository,
            _itemRateRepository,
            _packageRepository,
            _packageRateRepository
        );
    }

    [Fact]
    public async Task GetStoreAvailabilityAsync_ReturnsCorrectDto()
    {
        // Arrange
        var startDate = DateTime.Today;
        var endDate = DateTime.Today.AddDays(3);
        var rentalDays = 3;
        var item = new Item { Id = 1, Name = "Tent", Quantity = 10, Price = 100, Children = new List<Item>() };
        var items = new List<Item> { item };
        _itemRepository.GetRootItemsAsync().Returns(items);
        _availabilityService.GetAvailableQuantityAsync(1, startDate, endDate).Returns(5);
        _itemRateRepository.GetApplicableRateAsync(1, rentalDays).Returns(new ItemRate { DailyRate = 50 });
        _packageRepository.GetAllAsync().Returns(new List<Package>());

        // Act
        var result = await _service.GetStoreAvailabilityAsync(startDate, endDate);

        // Assert
        result.Should().NotBeNull();
        result.PeriodStart.Should().Be(startDate);
        result.PeriodEnd.Should().Be(endDate);
        result.Items.Should().HaveCount(1);
        result.Items.ToList()[0].ItemId.Should().Be(1);
        result.Items.ToList()[0].AvailableQuantity.Should().Be(5);
        result.Items.ToList()[0].DailyRate.Should().Be(50);
        result.Packages.Should().BeEmpty();
    }

    [Fact]
    public async Task GetStoreAvailabilityAsync_WithPackages_ReturnsCorrectPackages()
    {
        // Arrange
        var startDate = DateTime.Today;
        var endDate = DateTime.Today.AddDays(2);
        var rentalDays = 2;
        var item = new Item { Id = 1, Name = "Tent", Quantity = 10, Price = 100, Children = new List<Item>() };
        var pkgItem = new PackageItem { Item = item, Quantity = 2 };
        var package = new Package { Id = 1, Name = "Camping Set", BasePrice = 200, PackageItems = new List<PackageItem> { pkgItem } };
        _itemRepository.GetRootItemsAsync().Returns(new List<Item>());
        _packageRepository.GetAllAsync().Returns(new List<Package> { package });
        _packageRepository.GetByIdWithItemsAsync(1).Returns(package);
        _availabilityService.GetAvailableQuantityAsync(1, startDate, endDate).Returns(6);
        _itemRateRepository.GetApplicableRateAsync(1, rentalDays).Returns(new ItemRate { DailyRate = 50 });
        _packageRateRepository.GetApplicableRateAsync(1, rentalDays).Returns(new PackageRate { DailyRate = 150 });

        // Act
        var result = await _service.GetStoreAvailabilityAsync(startDate, endDate);

        // Assert
        result.Packages.Should().HaveCount(1);
        var pkgDto = result.Packages.ToList()[0];
        pkgDto.PackageId.Should().Be(1);
        pkgDto.PackageName.Should().Be("Camping Set");
        pkgDto.DailyRate.Should().Be(150);
        pkgDto.AvailablePackages.Should().Be(3); // 6 / 2
        pkgDto.Items.Should().HaveCount(1);
        pkgDto.Items.ToList()[0].ItemId.Should().Be(1);
        pkgDto.Items.ToList()[0].AvailableQuantity.Should().Be(6);
        pkgDto.Items.ToList()[0].DailyRate.Should().Be(50);
    }

    [Fact]
    public async Task GetStoreAvailabilityAsync_ItemWithChildren_AggregatesAvailability()
    {
        // Arrange
        var startDate = DateTime.Today;
        var endDate = DateTime.Today.AddDays(2);
        var rentalDays = 2;
        var child1 = new Item { Id = 2, Name = "Child1", Quantity = 5, Price = 10, Children = new List<Item>() };
        var child2 = new Item { Id = 3, Name = "Child2", Quantity = 7, Price = 12, Children = new List<Item>() };
        var parent = new Item { Id = 1, Name = "Parent", Quantity = 0, Price = 20, Children = new List<Item> { child1, child2 } };
        _itemRepository.GetRootItemsAsync().Returns(new List<Item> { parent });
        _availabilityService.GetAvailableQuantityAsync(2, startDate, endDate).Returns(3);
        _availabilityService.GetAvailableQuantityAsync(3, startDate, endDate).Returns(4);
        _itemRateRepository.GetApplicableRateAsync(2, rentalDays).Returns(new ItemRate { DailyRate = 10 });
        _itemRateRepository.GetApplicableRateAsync(3, rentalDays).Returns(new ItemRate { DailyRate = 12 });
        _itemRateRepository.GetApplicableRateAsync(1, rentalDays).Returns(new ItemRate { DailyRate = 20 });
        _packageRepository.GetAllAsync().Returns(new List<Package>());

        // Act
        var result = await _service.GetStoreAvailabilityAsync(startDate, endDate);

        // Assert
        result.Items.Should().HaveCount(1);
        var parentNode = result.Items.ToList()[0];
        parentNode.ItemId.Should().Be(1);
        parentNode.AvailableQuantity.Should().Be(7); // 3 + 4
        parentNode.Children.Should().HaveCount(2);
        parentNode.Children.ToList()[0].ItemId.Should().Be(2);
        parentNode.Children.ToList()[1].ItemId.Should().Be(3);
    }

    [Fact]
    public async Task GetStoreAvailabilityAsync_UsesParentRateIfChildHasNoRate()
    {
        // Arrange
        var startDate = DateTime.Today;
        var endDate = DateTime.Today.AddDays(2);
        var rentalDays = 2;
        var parent = new Item { Id = 1, Name = "Parent", Price = 20, Children = new List<Item>() };
        var child = new Item { Id = 2, Name = "Child", ParentId = 1, Price = 10, Children = new List<Item>() };
        parent.Children.Add(child);
        _itemRepository.GetRootItemsAsync().Returns(new List<Item> { parent });
        _availabilityService.GetAvailableQuantityAsync(2, startDate, endDate).Returns(5);
        _itemRateRepository.GetApplicableRateAsync(2, rentalDays).Returns((ItemRate)null!);
        _itemRateRepository.GetApplicableRateAsync(1, rentalDays).Returns(new ItemRate { DailyRate = 20 });
        _packageRepository.GetAllAsync().Returns(new List<Package>());

        // Act
        var result = await _service.GetStoreAvailabilityAsync(startDate, endDate);

        // Assert
        var childNode = result.Items.ToList()[0].Children.ToList()[0];
        childNode.DailyRate.Should().Be(20); // fallback to parent rate
    }

    [Fact]
    public async Task GetStoreAvailabilityAsync_UsesItemPriceIfNoRate()
    {
        // Arrange
        var startDate = DateTime.Today;
        var endDate = DateTime.Today.AddDays(2);
        var rentalDays = 2;
        var item = new Item { Id = 1, Name = "Item", Price = 99, Children = new List<Item>() };
        _itemRepository.GetRootItemsAsync().Returns(new List<Item> { item });
        _availabilityService.GetAvailableQuantityAsync(1, startDate, endDate).Returns(5);
        _itemRateRepository.GetApplicableRateAsync(1, rentalDays).Returns((ItemRate)null!);
        _packageRepository.GetAllAsync().Returns(new List<Package>());

        // Act
        var result = await _service.GetStoreAvailabilityAsync(startDate, endDate);

        // Assert
        result.Items.ToList()[0].DailyRate.Should().Be(99);
    }

    [Fact]
    public async Task GetStoreAvailabilityAsync_UsesPackageBasePriceIfNoRate()
    {
        // Arrange
        var startDate = DateTime.Today;
        var endDate = DateTime.Today.AddDays(2);
        var rentalDays = 2;
        var item = new Item { Id = 1, Name = "Tent", Quantity = 10, Price = 100, Children = new List<Item>() };
        var pkgItem = new PackageItem { Item = item, Quantity = 2 };
        var package = new Package { Id = 1, Name = "Camping Set", BasePrice = 200, PackageItems = new List<PackageItem> { pkgItem } };
        _itemRepository.GetRootItemsAsync().Returns(new List<Item>());
        _packageRepository.GetAllAsync().Returns(new List<Package> { package });
        _packageRepository.GetByIdWithItemsAsync(1).Returns(package);
        _availabilityService.GetAvailableQuantityAsync(1, startDate, endDate).Returns(6);
        _itemRateRepository.GetApplicableRateAsync(1, rentalDays).Returns((ItemRate)null!);
        _packageRateRepository.GetApplicableRateAsync(1, rentalDays).Returns((PackageRate)null!);

        // Act
        var result = await _service.GetStoreAvailabilityAsync(startDate, endDate);

        // Assert
        result.Packages.ToList()[0].DailyRate.Should().Be(200); // fallback to base price
    }

    [Fact]
    public async Task BuildPackageNodesAsync_ItemWithChildren_CreatesChildNodesAndAggregatesAvailability()
    {
        // Arrange
        var startDate = DateTime.Today;
        var endDate = DateTime.Today.AddDays(2);
        var rentalDays = 2;
        var child1 = new Item { Id = 2, Name = "Child1", Quantity = 5, Price = 10, Children = new List<Item>() };
        var child2 = new Item { Id = 3, Name = "Child2", Quantity = 7, Price = 12, Children = new List<Item>() };
        var parent = new Item { Id = 1, Name = "Parent", Quantity = 0, Price = 20, Children = new List<Item> { child1, child2 } };
        var pkgItem = new PackageItem { Item = parent, Quantity = 2 };
        var package = new Package { Id = 1, Name = "Camping Set", BasePrice = 200, PackageItems = new List<PackageItem> { pkgItem } };
        var packageList = new List<Package> { package };
        var packageRepo = Substitute.For<IPackageRepository>();
        var availabilityService = Substitute.For<IAvailabilityService>();
        var itemRateRepo = Substitute.For<IItemRateRepository>();
        var packageRateRepo = Substitute.For<IPackageRateRepository>();
        var itemRepo = Substitute.For<IItemRepository>();
        var service = new CatalogService(availabilityService, itemRepo, itemRateRepo, packageRepo, packageRateRepo);
        packageRepo.GetAllAsync().Returns(packageList);
        packageRepo.GetByIdWithItemsAsync(1).Returns(package);
        availabilityService.GetAvailableQuantityAsync(2, startDate, endDate).Returns(3);
        availabilityService.GetAvailableQuantityAsync(3, startDate, endDate).Returns(4);
        itemRateRepo.GetApplicableRateAsync(2, rentalDays).Returns(new ItemRate { DailyRate = 10 });
        itemRateRepo.GetApplicableRateAsync(3, rentalDays).Returns(new ItemRate { DailyRate = 12 });
        itemRateRepo.GetApplicableRateAsync(1, rentalDays).Returns(new ItemRate { DailyRate = 20 });
        packageRateRepo.GetApplicableRateAsync(1, rentalDays).Returns(new PackageRate { DailyRate = 150 });

        // Act
        var result = await service.GetStoreAvailabilityAsync(startDate, endDate);

        // Assert
        result.Packages.Should().HaveCount(1);
        var pkgDto = result.Packages.ToList()[0];
        pkgDto.PackageId.Should().Be(1);
        pkgDto.Items.Should().HaveCount(1);
        var itemNode = pkgDto.Items.ToList()[0];
        itemNode.ItemId.Should().Be(1);
        itemNode.Children.Should().HaveCount(2);
        itemNode.Children.ToList()[0].ItemId.Should().Be(2);
        itemNode.Children.ToList()[0].AvailableQuantity.Should().Be(3);
        itemNode.Children.ToList()[1].ItemId.Should().Be(3);
        itemNode.Children.ToList()[1].AvailableQuantity.Should().Be(4);
        itemNode.AvailableQuantity.Should().Be(7); // 3 + 4
    }
}
