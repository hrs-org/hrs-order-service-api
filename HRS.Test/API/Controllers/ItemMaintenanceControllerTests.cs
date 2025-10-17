using FluentAssertions;
using HRS.API.Contracts.DTOs.Maintenance;
using HRS.API.Controllers;
using HRS.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace HRS.Test.API.Controllers;

public class ItemMaintenanceControllerTests
{
    private readonly ItemMaintenanceController _controller;
    private readonly IItemMaintenanceService _service;

    public ItemMaintenanceControllerTests()
    {
        _service = Substitute.For<IItemMaintenanceService>();
        _controller = new ItemMaintenanceController(_service);
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithList()
    {
        // Arrange
        var maintenances = new List<ItemMaintenanceResponseDto> { new() { Id = 1 }, new() { Id = 2 } };
        _service.GetAllAsync().Returns(maintenances);

        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        var apiResponse = okResult.Value as dynamic;
        ((IEnumerable<ItemMaintenanceResponseDto>)apiResponse?.Data!).Should().BeEquivalentTo(maintenances);
    }

    [Fact]
    public async Task GetById_ReturnsOkWithItem()
    {
        // Arrange
        var maintenance = new ItemMaintenanceResponseDto { Id = 1 };
        _service.GetAsync(1).Returns(maintenance);

        // Act
        var result = await _controller.GetById(1);

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        var apiResponse = okResult.Value as dynamic;
        ((ItemMaintenanceResponseDto)apiResponse?.Data!).Should().BeEquivalentTo(maintenance);
    }

    [Fact]
    public async Task MarkAsFixed_ReturnsOkWithApiResponse()
    {
        // Arrange
        var request = new ItemMaintenanceRequestDto { Id = 1 };
        var response = new ItemMaintenanceResponseDto { Id = 1 };
        _service.MarkAsFixedAsync(request).Returns(response);

        // Act
        var result = await _controller.MarkAsFixed(1, request);

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        var apiResponse = okResult.Value as dynamic;
        ((ItemMaintenanceResponseDto)apiResponse?.Data!).Should().BeEquivalentTo(response);
        ((string)apiResponse?.Message!).Should().Be("Item maintenance marked as fixed successfully");
        request.Id.Should().Be(1);
        await _service.Received(1).MarkAsFixedAsync(request);
    }
}
