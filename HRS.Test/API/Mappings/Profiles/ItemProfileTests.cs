using AutoMapper;
using FluentAssertions;
using HRS.API.Contracts.DTOs.Item;
using HRS.API.Mappings.Profiles;
using HRS.Domain.Entities;
using HRS.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace HRS.Test.API.Mappings.Profiles;

public class ItemProfileTests
{
    private readonly IMapper _mapper;

    public ItemProfileTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => { });

        var config = new MapperConfiguration(cfg => { cfg.AddProfile<ItemProfile>(); }, loggerFactory);

        config.AssertConfigurationIsValid();

        _mapper = config.CreateMapper();
    }

    [Fact]
    public void Should_Map_AddItemRequestDto_To_Item()
    {
        // Arrange
        var dto = new AddItemRequestDto
        {
            Name = "Tent",
            Description = "Camping tent",
            Price = 100,
            Quantity = 5,
            Children = new List<AddItemRequestDto>
            {
                new() { Name = "Child Tent", Description = "Small tent", Price = 50, Quantity = 2 }
            },
            Rates = new List<ItemRateRequestDto>
            {
                new() { MinDays = 1, DailyRate = 20, IsActive = true },
                new() { MinDays = 3, DailyRate = 15, IsActive = false }
            }
        };

        // Act
        var item = _mapper.Map<Item>(dto);

        // Assert
        item.Id.Should().Be(0);
        item.ParentId.Should().BeNull();
        item.Name.Should().Be(dto.Name);
        item.Children.Should().HaveCount(1);
        item.Children.First().Name.Should().Be("Child Tent");
        item.Rates.Should().HaveCount(2);
    }

    [Fact]
    public void Should_Map_Item_To_ItemResponseDto()
    {
        // Arrange
        var user = new User { Id = 1, FirstName = "Admin", LastName = "User", Email = "r@w.com", Role = UserRole.Admin, PasswordHash = "hashedpassword" };
        var item = new Item
        {
            Id = 1,
            Name = "Tent",
            Description = "Camping tent",
            Price = 100,
            Quantity = 5,
            CreatedBy = user,
            Children = new List<Item>
            {
                new() { Id = 2, Name = "Child Tent", Description = "Small tent", Price = 50, Quantity = 2, CreatedBy = user }
            },
            Rates = new List<ItemRate>
            {
                new() { Id = 1, MinDays = 1, DailyRate = 20, IsActive = true },
                new() { Id = 2, MinDays = 3, DailyRate = 15, IsActive = false }
            }
        };

        // Act
        var dto = _mapper.Map<ItemResponseDto>(item);

        // Assert
        dto.Id.Should().Be(1);
        dto.Name.Should().Be("Tent");
        dto.Children.Should().HaveCount(1);
        dto.Children.First().Name.Should().Be("Child Tent");
        dto.Rates.Should().HaveCount(2);
    }

    [Fact]
    public void Should_Map_ItemRateRequestDto_To_ItemRate()
    {
        // Arrange
        var dto = new ItemRateRequestDto { MinDays = 5, DailyRate = 99.99m, IsActive = true };

        // Act
        var entity = _mapper.Map<ItemRate>(dto);

        // Assert
        entity.MinDays.Should().Be(5);
        entity.DailyRate.Should().Be(99.99m);
        entity.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Should_Map_ItemRate_To_ItemRateResponseDto()
    {
        // Arrange
        var entity = new ItemRate { Id = 7, MinDays = 2, DailyRate = 55.5m, IsActive = false };

        // Act
        var dto = _mapper.Map<ItemRateResponseDto>(entity);

        // Assert
        dto.MinDays.Should().Be(2);
        dto.DailyRate.Should().Be(55.5m);
        dto.IsActive.Should().BeFalse();
    }
}
