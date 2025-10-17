using AutoMapper;
using FluentAssertions;
using HRS.API.Contracts.DTOs.Package;
using HRS.API.Mappings.Profiles;
using HRS.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace HRS.Test.API.Mappings.Profiles;

public class PackageProfileTests
{
    private readonly IMapper _mapper;

    public PackageProfileTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => { });
        var config = new MapperConfiguration(cfg => { cfg.AddProfile<PackageProfile>(); }, loggerFactory);
        config.AssertConfigurationIsValid();
        _mapper = config.CreateMapper();
    }

    [Fact]
    public void Should_Map_AddPackageRequestDto_To_Package()
    {
        // Arrange
        var dto = new AddPackageRequestDto
        {
            Name = "Adventure Package",
            Description = "A fun package",
            BasePrice = 100,
            Items = new List<PackageItemRequestDto>
            {
                new() { ItemId = 1, Quantity = 2 },
                new() { ItemId = 2, Quantity = 1 }
            },
            Rates = new List<PackageRateRequestDto>
            {
                new() { MinDays = 1, DailyRate = 50, IsActive = true },
                new() { MinDays = 3, DailyRate = 40, IsActive = false }
            }
        };

        // Act
        var entity = _mapper.Map<Package>(dto);

        // Assert
        entity.Id.Should().Be(0);
        entity.Name.Should().Be(dto.Name);
        entity.Description.Should().Be(dto.Description);
        entity.BasePrice.Should().Be(dto.BasePrice);
        entity.PackageItems.Should().HaveCount(2);
        entity.PackageRates.Should().HaveCount(2);
    }

    [Fact]
    public void Should_Map_Package_To_PackageResponseDto()
    {
        // Arrange
        var item1 = new PackageItem { Id = 1, ItemId = 1, Quantity = 2, Item = new Item { Name = "Tent" } };
        var item2 = new PackageItem { Id = 2, ItemId = 2, Quantity = 1, Item = new Item { Name = "Stove" } };
        var rate1 = new PackageRate { Id = 1, MinDays = 1, DailyRate = 50, IsActive = true };
        var rate2 = new PackageRate { Id = 2, MinDays = 3, DailyRate = 40, IsActive = false };
        var entity = new Package
        {
            Id = 10,
            Name = "Adventure Package",
            Description = "A fun package",
            BasePrice = 100,
            PackageItems = new List<PackageItem> { item1, item2 },
            PackageRates = new List<PackageRate> { rate1, rate2 }
        };

        // Act
        var dto = _mapper.Map<PackageResponseDto>(entity);

        // Assert
        dto.Id.Should().Be(10);
        dto.Name.Should().Be("Adventure Package");
        dto.Items.Should().HaveCount(2);
        dto.Items.First().ItemName.Should().Be("Tent");
        dto.Rates.Should().HaveCount(2);
    }

    [Fact]
    public void Should_Map_PackageItemRequestDto_To_PackageItem()
    {
        // Arrange
        var dto = new PackageItemRequestDto { ItemId = 5, Quantity = 3 };

        // Act
        var entity = _mapper.Map<PackageItem>(dto);

        // Assert
        entity.ItemId.Should().Be(5);
        entity.Quantity.Should().Be(3);
    }

    [Fact]
    public void Should_Map_PackageRateRequestDto_To_PackageRate()
    {
        // Arrange
        var dto = new PackageRateRequestDto { MinDays = 7, DailyRate = 99.99m, IsActive = true };

        // Act
        var entity = _mapper.Map<PackageRate>(dto);

        // Assert
        entity.MinDays.Should().Be(7);
        entity.DailyRate.Should().Be(99.99m);
        entity.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Should_Map_PackageRate_To_PackageRateResponseDto()
    {
        // Arrange
        var entity = new PackageRate { Id = 7, MinDays = 2, DailyRate = 55.5m, IsActive = false };

        // Act
        var dto = _mapper.Map<PackageRateResponseDto>(entity);

        // Assert
        dto.MinDays.Should().Be(2);
        dto.DailyRate.Should().Be(55.5m);
        dto.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Should_Map_PackageItem_To_PackageItemResponseDto()
    {
        // Arrange
        var entity = new PackageItem { Id = 3, ItemId = 8, Quantity = 4, Item = new Item { Name = "Lamp" } };

        // Act
        var dto = _mapper.Map<PackageItemResponseDto>(entity);

        // Assert
        dto.ItemId.Should().Be(8);
        dto.Quantity.Should().Be(4);
        dto.ItemName.Should().Be("Lamp");
    }
}
