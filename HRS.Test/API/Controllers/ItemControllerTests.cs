using FluentAssertions;
using HRS.API.Contracts.DTOs.Item;
using HRS.API.Controllers;
using HRS.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace HRS.Test.API.Controllers;

public class ItemControllerTests
{
    private readonly ItemController _controller;
    private readonly IItemService _itemService;

    public ItemControllerTests()
    {
        _itemService = Substitute.For<IItemService>();
        _controller = new ItemController(_itemService);
    }

    [Fact]
    public async Task GetItemsAsync_ReturnsOkWithList()
    {
        // Arrange
        var items = new List<ItemResponseDto> { new() { Id = 1 }, new() { Id = 2 } };
        _itemService.GetRootItemsAsync().Returns(items);

        // Act
        var result = await _controller.GetItemsAsync();

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        var apiResponse = okResult.Value as dynamic;
        ((IEnumerable<ItemResponseDto>)apiResponse?.Data!).Should().BeEquivalentTo(items);
    }

    [Fact]
    public async Task GetItemAsync_ReturnsOkWithItem()
    {
        // Arrange
        var item = new ItemResponseDto { Id = 1 };
        _itemService.GetItemAsync(1).Returns(item);

        // Act
        var result = await _controller.GetItemAsync(1);

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        var apiResponse = okResult.Value as dynamic;
        ((ItemResponseDto)apiResponse?.Data!).Should().BeEquivalentTo(item);
    }

    [Fact]
    public async Task AddNewItem_ReturnsCreatedAtAction()
    {
        // Arrange
        var addDto = new AddItemRequestDto { Name = "Test", Description = "This is Test", Quantity = 10, Price = 10.5m };
        var created = new ItemResponseDto { Id = 1, Name = "Test" };
        _itemService.CreateAsync(addDto).Returns(created);

        // Act
        var result = await _controller.CreateItemAsync(addDto);

        // Assert
        var createdResult = result as CreatedAtActionResult;
        createdResult.Should().NotBeNull();
        var apiResponse = createdResult.Value as dynamic;
        ((ItemResponseDto)apiResponse?.Data!).Should().BeEquivalentTo(created);
        createdResult.RouteValues?["id"].Should().Be(created.Id);
    }

    [Fact]
    public async Task AddNewItem_WithRates_ReturnsCreatedAtAction()
    {
        // Arrange
        var addDto = new AddItemRequestDto
        {
            Name = "Tent",
            Description = "4-person tent",
            Quantity = 5,
            Price = 100m,
            Rates = new List<ItemRateRequestDto>
            {
                new() { MinDays = 1, DailyRate = 20m, IsActive = true },
                new() { MinDays = 3, DailyRate = 15m, IsActive = true }
            }
        };
        var created = new ItemResponseDto
        {
            Id = 10,
            Name = "Tent",
            Rates = new List<ItemRateResponseDto>
            {
                new() { MinDays = 1, DailyRate = 20m, IsActive = true },
                new() { MinDays = 3, DailyRate = 15m, IsActive = true }
            }
        };
        _itemService.CreateAsync(addDto).Returns(created);

        // Act
        var result = await _controller.CreateItemAsync(addDto);

        // Assert
        var createdResult = result as CreatedAtActionResult;
        createdResult.Should().NotBeNull();
        var apiResponse = createdResult.Value as dynamic;
        ((ItemResponseDto)apiResponse?.Data!).Should().BeEquivalentTo(created);
        createdResult.RouteValues?["id"].Should().Be(created.Id);
        createdResult.ActionName.Should().Be("GetItem");
    }

    [Fact]
    public async Task UpdateItemAsync_WithRates_ReturnsOkWithApiResponse()
    {
        // Arrange
        var updateDto = new UpdateItemRequestDto
        {
            Id = 10,
            Name = "Tent Updated",
            Description = "Updated tent",
            Quantity = 7,
            Price = 110m,
            Rates = new List<ItemRateRequestDto>
            {
                new() { MinDays = 1, DailyRate = 22m, IsActive = true }
            }
        };
        var updated = new ItemResponseDto
        {
            Id = 10,
            Name = "Tent Updated",
            Rates = new List<ItemRateResponseDto>
            {
                new() { MinDays = 1, DailyRate = 22m, IsActive = true }
            }
        };
        _itemService.UpdateAsync(updateDto).Returns(updated);

        // Act
        var result = await _controller.UpdateItemAsync(10, updateDto);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        var apiResponse = okResult.Value as dynamic;
        ((object)apiResponse?.Data!).Should().BeNull();
        ((string)apiResponse?.Message!).Should().Be("Item updated successfully");
        updateDto.Id.Should().Be(10);
        await _itemService.Received(1).UpdateAsync(updateDto);
    }

    [Fact]
    public async Task DeleteItemAsync_ReturnsOkWithApiResponse()
    {
        // Arrange
        _itemService.DeleteAsync(1).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteItemAsync(1);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        var apiResponse = okResult.Value as dynamic;
        ((object)apiResponse?.Data!).Should().BeNull();
        ((string)apiResponse?.Message!).Should().Be("Item deleted successfully");
        await _itemService.Received(1).DeleteAsync(1);
    }

    [Fact]
    public async Task SearchItemsAsync_WithKeyword_ReturnsOkWithResults()
    {
        // Arrange
        var dtos = new List<ItemResponseDto> { new() { Id = 1, Name = "Trekking Pole" } };
        _itemService.SearchItemsAsync("Trekking Pole").Returns(dtos);

        // Act
        var result = await _controller.SearchItemsAsync("Trekking Pole");

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        var apiResponse = okResult.Value as dynamic;
        ((List<ItemResponseDto>)apiResponse?.Data!).Should().BeEquivalentTo(dtos);
    }

    [Fact]
    public async Task SearchItemsAsync_WithNoKeyword_ReturnsOkWithAllResults()
    {
        // Arrange
        var dtos = new List<ItemResponseDto>
        {
            new() { Id = 1, Name = "Trekking Pole" },
            new() { Id = 2, Name = "Tent" }
        };
        _itemService.SearchItemsAsync(null).Returns(dtos);

        // Act
        var result = await _controller.SearchItemsAsync(null);

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        var apiResponse = okResult.Value as dynamic;
        ((List<ItemResponseDto>)apiResponse?.Data!).Should().BeEquivalentTo(dtos);
    }

    [Fact]
    public async Task SearchItemsAsync_WithNoResults_ReturnsOkWithEmptyList()
    {
        // Arrange
        var dtos = new List<ItemResponseDto>();
        _itemService.SearchItemsAsync("NonExistentGear").Returns(dtos);

        // Act
        var result = await _controller.SearchItemsAsync("NonExistentGear");

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        var apiResponse = okResult.Value as dynamic;
        ((List<ItemResponseDto>)apiResponse?.Data!).Should().BeEmpty();
    }
}
