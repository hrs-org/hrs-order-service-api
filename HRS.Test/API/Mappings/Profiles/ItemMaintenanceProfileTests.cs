using AutoMapper;
using FluentAssertions;
using HRS.API.Contracts.DTOs.Maintenance;
using HRS.API.Mappings.Profiles;
using HRS.Domain.Entities;
using HRS.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace HRS.Test.API.Mappings.Profiles;

public class ItemMaintenanceProfileTests
{
    private readonly IMapper _mapper;

    public ItemMaintenanceProfileTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => { });
        var config = new MapperConfiguration(cfg => { cfg.AddProfile<ItemMaintenanceProfile>(); }, loggerFactory);
        config.AssertConfigurationIsValid();
        _mapper = config.CreateMapper();
    }

    [Fact]
    public void Should_Map_ItemMaintenance_To_ItemMaintenanceResponseDto()
    {
        // Arrange
        var entity = new ItemMaintenance
        {
            Id = 1,
            ItemId = 2,
            RentalOrderId = 3,
            Type = ItemMaintenanceType.Broken,
            Quantity = 5,
            QuantityFixed = 0,
            Remarks = "Broken zipper"
        };

        // Act
        var dto = _mapper.Map<ItemMaintenanceResponseDto>(entity);

        // Assert
        dto.Should().NotBeNull();
        dto.Id.Should().Be(1);
        dto.ItemId.Should().Be(2);
        dto.Type.Should().Be(ItemMaintenanceType.Broken.ToString());
        dto.Remarks.Should().BeNull();
    }
}
