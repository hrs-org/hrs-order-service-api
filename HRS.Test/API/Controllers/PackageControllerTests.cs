using FluentAssertions;
using HRS.API.Contracts.DTOs.Package;
using HRS.API.Controllers;
using HRS.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace HRS.Test.API.Controllers;

public class PackageControllerTests
{
    private readonly PackageController _controller;
    private readonly IPackageService _packageService;

    public PackageControllerTests()
    {
        _packageService = Substitute.For<IPackageService>();
        _controller = new PackageController(_packageService);
    }

    [Fact]
    public async Task GetPackagesAsync_ReturnsOkWithList()
    {
        // Arrange
        var packages = new List<PackageResponseDto> { new() { Id = 1 }, new() { Id = 2 } };
        _packageService.GetAllAsync().Returns(packages);

        // Act
        var result = await _controller.GetPackagesAsync();

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        var apiResponse = okResult.Value as dynamic;
        ((IEnumerable<PackageResponseDto>)apiResponse?.Data!).Should().BeEquivalentTo(packages);
    }

    [Fact]
    public async Task GetPackageAsync_ReturnsOkWithPackage()
    {
        // Arrange
        var package = new PackageResponseDto { Id = 1 };
        _packageService.GetByIdAsync(1).Returns(package);

        // Act
        var result = await _controller.GetPackageAsync(1);

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        var apiResponse = okResult.Value as dynamic;
        ((PackageResponseDto)apiResponse?.Data!).Should().BeEquivalentTo(package);
    }

    [Fact]
    public async Task CreatePackageAsync_ReturnsOkWithCreatedPackage()
    {
        // Arrange
        var addDto = new AddPackageRequestDto { Name = "Test Package", Description = "Test Desc" };
        var created = new PackageResponseDto { Id = 1, Name = "Test Package" };
        _packageService.CreateAsync(addDto).Returns(created);

        // Act
        var result = await _controller.CreatePackageAsync(addDto);

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        var apiResponse = okResult.Value as dynamic;
        ((PackageResponseDto)apiResponse?.Data!).Should().BeEquivalentTo(created);
        ((string)apiResponse?.Message!).Should().Be("Package created successfully");
    }

    [Fact]
    public async Task UpdatePackageAsync_ReturnsOkWithUpdatedPackage()
    {
        // Arrange
        var updateDto = new UpdatePackageRequestDto { Id = 1, Name = "Updated", Description = "Updated Desc" };
        var updated = new PackageResponseDto { Id = 1, Name = "Updated" };
        _packageService.UpdateAsync(updateDto).Returns(updated);

        // Act
        var result = await _controller.UpdatePackageAsync(1, updateDto);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        var apiResponse = okResult.Value as dynamic;
        ((PackageResponseDto)apiResponse?.Data!).Should().BeEquivalentTo(updated);
        ((string)apiResponse?.Message!).Should().Be("Package updated successfully");
        updateDto.Id.Should().Be(1);
        await _packageService.Received(1).UpdateAsync(updateDto);
    }

    [Fact]
    public async Task DeletePackageAsync_ReturnsOkWithApiResponse()
    {
        // Arrange
        _packageService.DeleteAsync(1).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeletePackageAsync(1);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        var apiResponse = okResult.Value as dynamic;
        ((string)apiResponse?.Message!).Should().Be("Package 1 deleted successfully");
        await _packageService.Received(1).DeleteAsync(1);
    }
}
