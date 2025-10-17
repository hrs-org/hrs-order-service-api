using FluentAssertions;
using HRS.API.Contracts.DTOs.Catalog;
using HRS.API.Controllers;
using HRS.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace HRS.Test.API.Controllers;

public class CatalogControllerTests
{
    private readonly ICatalogService _catalogService;
    private readonly CatalogController _controller;

    public CatalogControllerTests()
    {
        _catalogService = Substitute.For<ICatalogService>();
        _controller = new CatalogController(_catalogService);
    }

    [Fact]
    public async Task GetAvailability_ReturnsOkWithStoreResponse()
    {
        // Arrange
        var request = new GetCatalogRequestDto { StartDate = DateTime.Today, EndDate = DateTime.Today.AddDays(2) };
        var storeResponse = new CatalogResponseDto { PeriodStart = request.StartDate, PeriodEnd = request.EndDate };
        _catalogService.GetStoreAvailabilityAsync(request.StartDate, request.EndDate).Returns(storeResponse);

        // Act
        var result = await _controller.GetAvailability(request);

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        var apiResponse = okResult.Value as dynamic;
        ((CatalogResponseDto)apiResponse?.Data!).Should().BeEquivalentTo(storeResponse);
    }
}
