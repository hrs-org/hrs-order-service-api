using System.Reflection;
using AutoMapper;
using FluentAssertions;
using HRS.API.Contracts.DTOs.Package;
using HRS.API.Services;
using HRS.API.Services.Interfaces;
using HRS.Domain.Entities;
using HRS.Domain.Interfaces;
using NSubstitute;

namespace HRS.Test.API.Services;

public class PackageServiceTests
{
    private readonly IMapper _mapper;
    private readonly IPackageRateRepository _packageRateRepository;
    private readonly IPackageRepository _packageRepository;
    private readonly PackageService _service;
    private readonly IUserContextService _userContextService;

    public PackageServiceTests()
    {
        _packageRepository = Substitute.For<IPackageRepository>();
        _packageRateRepository = Substitute.For<IPackageRateRepository>();
        _mapper = Substitute.For<IMapper>();
        _userContextService = Substitute.For<IUserContextService>();
        _service = new PackageService(_mapper, _userContextService, _packageRepository, _packageRateRepository);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsMappedDtos()
    {
        // Arrange
        var packages = new List<Package> { new() { Id = 1 }, new() { Id = 2 } };
        var dtos = new List<PackageResponseDto> { new() { Id = 1 }, new() { Id = 2 } };
        _packageRepository.GetAllWithDetailsAsync().Returns(packages);
        _mapper.Map<IEnumerable<PackageResponseDto>>(packages).Returns(dtos);

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        result.Should().BeEquivalentTo(dtos);
    }

    [Fact]
    public async Task GetByIdAsync_WhenFound_ReturnsMappedDto()
    {
        // Arrange
        var package = new Package { Id = 1 };
        var dto = new PackageResponseDto { Id = 1 };
        _packageRepository.GetByIdWithDetailsAsync(1).Returns(package);
        _mapper.Map<PackageResponseDto>(package).Returns(dto);

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        result.Should().BeEquivalentTo(dto);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _packageRepository.GetByIdWithDetailsAsync(1).Returns((Package)null!);
        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.GetByIdAsync(1));
    }

    [Fact]
    public async Task CreateAsync_CreatesAndReturnsDto()
    {
        // Arrange
        var addDto = new AddPackageRequestDto { Name = "Test", Description = "Desc", BasePrice = 10 };
        var user = new User { Id = 5 };
        var entity = new Package { Name = "Test", Description = "Desc", BasePrice = 10, PackageRates = new List<PackageRate>() };
        var responseDto = new PackageResponseDto { Name = "Test", Description = "Desc", BasePrice = 10 };
        var trx = Substitute.For<IAsyncDisposable>();
        _userContextService.GetUserAsync().Returns(user);
        _mapper.Map<Package>(addDto).Returns(entity);
        _mapper.Map<PackageResponseDto>(entity).Returns(responseDto);

        // Act
        var result = await _service.CreateAsync(addDto);

        // Assert
        await _packageRepository.Received(1).AddAsync(entity);
        await _packageRepository.Received(1).SaveChangesAsync();
        result.Should().BeEquivalentTo(responseDto);
    }

    [Fact]
    public async Task CreateAsync_WhenException_RollsBackAndThrows()
    {
        // Arrange
        var addDto = new AddPackageRequestDto { Name = "Test" };
        var user = new User { Id = 1 };
        var entity = new Package { Name = "Test", PackageRates = new List<PackageRate>() };
        var trx = Substitute.For<IAsyncDisposable>();
        _userContextService.GetUserAsync().Returns(user);
        _mapper.Map<Package>(addDto).Returns(entity);
        _packageRepository.AddAsync(entity).Returns(x => { throw new Exception("fail"); });

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _service.CreateAsync(addDto));
    }

    [Fact]
    public async Task UpdateAsync_WhenFound_UpdatesAndReturnsDto()
    {
        // Arrange
        var updateDto = new UpdatePackageRequestDto { Id = 1, Name = "Updated", Description = "Desc", BasePrice = 20 };
        var user = new User { Id = 2 };
        var entity = new Package
        { Id = 1, Name = "Old", Description = "OldDesc", BasePrice = 10, PackageItems = new List<PackageItem>(), PackageRates = new List<PackageRate>() };
        var responseDto = new PackageResponseDto { Id = 1, Name = "Updated", Description = "Desc", BasePrice = 20 };
        var trx = Substitute.For<IAsyncDisposable>();
        _userContextService.GetUserAsync().Returns(user);
        _packageRepository.GetByIdWithDetailsAsync(1).Returns(entity);
        _mapper.Map<PackageResponseDto>(entity).Returns(responseDto);
        _packageRateRepository.GetRatesByPackageIdAsync(1).Returns(new List<PackageRate>());

        // Act
        var result = await _service.UpdateAsync(updateDto);

        // Assert
        await _packageRepository.Received(1).SaveChangesAsync();
        result.Should().BeEquivalentTo(responseDto);
    }

    [Fact]
    public async Task UpdateAsync_WhenNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var updateDto = new UpdatePackageRequestDto
        {
            Id = 1,
            Name = "Updated"
        };
        var user = new User { Id = 2 };
        var trx = Substitute.For<IAsyncDisposable>();
        _userContextService.GetUserAsync().Returns(user);
        _packageRepository.GetByIdWithDetailsAsync(1).Returns((Package)null!);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.UpdateAsync(updateDto));
    }

    [Fact]
    public async Task DeleteAsync_WhenFound_RemovesAndSaves()
    {
        // Arrange
        var entity = new Package { Id = 1 };
        _packageRepository.GetByIdAsync(1).Returns(entity);

        // Act
        await _service.DeleteAsync(1);

        // Assert
        _packageRepository.Received(1).Remove(entity);
        await _packageRepository.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task DeleteAsync_WhenNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _packageRepository.GetByIdAsync(1).Returns((Package)null!);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.DeleteAsync(1));
    }

    [Fact]
    public void SyncPackageItemsAsync_RemovesAndAddsCorrectly()
    {
        // Arrange
        var package = new Package
        {
            Id = 1,
            PackageItems = new List<PackageItem>
            {
                new() { PackageId = 1, ItemId = 1, Quantity = 2 },
                new() { PackageId = 1, ItemId = 2, Quantity = 3 }
            }
        };
        var items = new List<PackageItemRequestDto>
        {
            new() { ItemId = 1, Quantity = 5 }, // update
            new() { ItemId = 3, Quantity = 7 } // add
        };

        // Act
        var method = typeof(PackageService).GetMethod("SyncPackageItemsAsync", BindingFlags.NonPublic | BindingFlags.Static);
        method.Invoke(null, new object[] { package, items });

        // Assert
        package.PackageItems.Should().HaveCount(2);
        package.PackageItems.Should().ContainSingle(x => x.ItemId == 1 && x.Quantity == 5);
        package.PackageItems.Should().ContainSingle(x => x.ItemId == 3 && x.Quantity == 7);
    }
}
