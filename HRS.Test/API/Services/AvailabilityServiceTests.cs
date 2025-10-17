using FluentAssertions;
using HRS.API.Services;
using HRS.Domain.Entities;
using HRS.Domain.Interfaces;
using NSubstitute;

namespace HRS.Test.API.Services;

public class AvailabilityServiceTests
{
    private readonly IItemMaintenanceRepository _itemMaintenanceRepository;
    private readonly IItemRepository _itemRepository;
    private readonly IPackageRepository _packageRepository;
    private readonly IRentalOrderItemRepository _rentalOrderItemRepository;
    private readonly IRentalOrderPackageItemRepository _rentalOrderPackageItemRepository;
    private readonly AvailabilityService _service;

    public AvailabilityServiceTests()
    {
        _itemRepository = Substitute.For<IItemRepository>();
        _packageRepository = Substitute.For<IPackageRepository>();
        _rentalOrderItemRepository = Substitute.For<IRentalOrderItemRepository>();
        _rentalOrderPackageItemRepository = Substitute.For<IRentalOrderPackageItemRepository>();
        _itemMaintenanceRepository = Substitute.For<IItemMaintenanceRepository>();
        _service = new AvailabilityService(
            _itemRepository,
            _packageRepository,
            _rentalOrderItemRepository,
            _rentalOrderPackageItemRepository,
            _itemMaintenanceRepository
        );
    }

    [Fact]
    public async Task GetAvailableQuantityAsync_ReturnsCorrectAvailable()
    {
        // Arrange
        var item = new Item { Id = 1, Name = "Tent", Quantity = 10 };
        _itemRepository.GetByIdAsync(1).Returns(item);
        _rentalOrderItemRepository.GetReservedQuantityAsync(1, Arg.Any<DateTime>(), Arg.Any<DateTime>()).Returns(2);
        _rentalOrderPackageItemRepository.GetReservedQuantityFromPackagesAsync(1, Arg.Any<DateTime>(), Arg.Any<DateTime>()).Returns(3);
        _itemMaintenanceRepository.GetRepairingQuantityAsync(1).Returns(1);

        // Act
        var available = await _service.GetAvailableQuantityAsync(1, DateTime.Today, DateTime.Today.AddDays(1));

        // Assert
        available.Should().Be(4); // 10 - (2+3+1)
    }

    [Fact]
    public async Task GetAvailableQuantityAsync_WhenItemNotFound_ThrowsKeyNotFoundException()
    {
        _itemRepository.GetByIdAsync(1).Returns((Item)null!);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.GetAvailableQuantityAsync(1, DateTime.Today, DateTime.Today.AddDays(1)));
    }

    [Fact]
    public async Task GetAvailableItemsAsync_ReturnsAvailabilityDtos()
    {
        // Arrange
        var items = new List<Item>
        {
            new() { Id = 1, Name = "Tent", Quantity = 10 },
            new() { Id = 2, Name = "Stove", Quantity = 5 }
        };
        _itemRepository.GetAllAsync().Returns(items);
        _itemRepository.GetByIdAsync(1).Returns(items[0]);
        _itemRepository.GetByIdAsync(2).Returns(items[1]);
        _rentalOrderItemRepository.GetReservedQuantityAsync(Arg.Any<int>(), Arg.Any<DateTime>(), Arg.Any<DateTime>()).Returns(0);
        _rentalOrderPackageItemRepository.GetReservedQuantityFromPackagesAsync(Arg.Any<int>(), Arg.Any<DateTime>(), Arg.Any<DateTime>()).Returns(0);
        _itemMaintenanceRepository.GetRepairingQuantityAsync(Arg.Any<int>()).Returns(0);

        // Act
        var result = (await _service.GetAvailableItemsAsync(DateTime.Today, DateTime.Today.AddDays(1))).ToList();

        // Assert
        result.Should().HaveCount(2);
        result[0].ItemId.Should().Be(1);
        result[1].ItemId.Should().Be(2);
        result.All(r => r.AvailableQuantity == r.TotalQuantity).Should().BeTrue();
    }

    [Fact]
    public async Task GetAvailablePackagesAsync_ReturnsAvailabilityDtos()
    {
        // Arrange
        var item = new Item { Id = 1, Name = "Tent", Quantity = 10 };
        var package = new Package
        {
            Id = 1,
            Name = "Camping Set",
            PackageItems = new List<PackageItem>
            {
                new() { Item = item, Quantity = 2 }
            }
        };
        _packageRepository.GetAllAsync().Returns(new List<Package> { package });
        _packageRepository.GetByIdWithItemsAsync(1).Returns(package);
        _itemRepository.GetByIdAsync(1).Returns(item);
        _rentalOrderItemRepository.GetReservedQuantityAsync(1, Arg.Any<DateTime>(), Arg.Any<DateTime>()).Returns(0);
        _rentalOrderPackageItemRepository.GetReservedQuantityFromPackagesAsync(1, Arg.Any<DateTime>(), Arg.Any<DateTime>()).Returns(0);
        _itemMaintenanceRepository.GetRepairingQuantityAsync(1).Returns(0);

        // Act
        var result = (await _service.GetAvailablePackagesAsync(DateTime.Today, DateTime.Today.AddDays(1))).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].PackageId.Should().Be(1);
        result[0].AvailablePackages.Should().Be(5); // 10 / 2
        result[0].ItemBreakdown.Should().HaveCount(1);
        result[0].ItemBreakdown.ToList()[0].ItemId.Should().Be(1);
    }

    [Fact]
    public async Task GetAvailablePackagesAsync_SkipsPackagesWithNoItems()
    {
        // Arrange
        var package = new Package { Id = 1, Name = "Empty", PackageItems = new List<PackageItem>() };
        _packageRepository.GetAllAsync().Returns(new List<Package> { package });
        _packageRepository.GetByIdWithItemsAsync(1).Returns(package);

        // Act
        var result = (await _service.GetAvailablePackagesAsync(DateTime.Today, DateTime.Today.AddDays(1))).ToList();

        // Assert
        result.Should().BeEmpty();
    }
}
