using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HRS.Domain.Entities;
using HRS.Infrastructure;
using HRS.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HRS.Test.Infrastructure.Repositories;

public class PackageRepositoryTests
{
    private static AppDbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetAllWithDetailsAsync_ReturnsPackagesWithItemsAndRates()
    {
        // Arrange
        var dbName = $"PackageRepo_GetAllWithDetails_{Guid.NewGuid()}";
        using var dbContext = CreateDbContext(dbName);
        var repo = new PackageRepository(dbContext);
        var item = new Item { Id = 1, Name = "Item1", Description = "Desc", Quantity = 1, Price = 10 };
        var package = new Package
        {
            Id = 1,
            Name = "Package1",
            PackageItems = new List<PackageItem> { new() { Id = 1, ItemId = 1, Item = item, Quantity = 2 } },
            PackageRates = new List<PackageRate> { new() { Id = 1, PackageId = 1, MinDays = 1, DailyRate = 100, IsActive = true } }
        };
        dbContext.Items.Add(item);
        dbContext.Packages.Add(package);
        await dbContext.SaveChangesAsync();

        // Act
        var result = (await repo.GetAllWithDetailsAsync()).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal("Package1", result[0].Name);
        Assert.Single(result[0].PackageItems);
        Assert.Equal(1, result[0].PackageItems.First().ItemId);
        Assert.Single(result[0].PackageRates);
        Assert.Equal(1, result[0].PackageRates.First().MinDays);
    }

    [Fact]
    public async Task GetByIdWithDetailsAsync_ReturnsPackageWithItemsAndRates()
    {
        // Arrange
        var dbName = $"PackageRepo_GetByIdWithDetails_{Guid.NewGuid()}";
        using var dbContext = CreateDbContext(dbName);
        var repo = new PackageRepository(dbContext);
        var item = new Item { Id = 2, Name = "Item2", Description = "Desc", Quantity = 1, Price = 10 };
        var package = new Package
        {
            Id = 2,
            Name = "Package2",
            PackageItems = new List<PackageItem> { new() { Id = 2, ItemId = 2, Item = item, Quantity = 3 } },
            PackageRates = new List<PackageRate> { new() { Id = 2, PackageId = 2, MinDays = 2, DailyRate = 200, IsActive = true } }
        };
        dbContext.Items.Add(item);
        dbContext.Packages.Add(package);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await repo.GetByIdWithDetailsAsync(2);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Package2", result.Name);
        Assert.Single(result.PackageItems);
        Assert.Equal(2, result.PackageItems.First().ItemId);
        Assert.Single(result.PackageRates);
        Assert.Equal(2, result.PackageRates.First().MinDays);
    }

    [Fact]
    public async Task GetByIdWithDetailsAsync_ReturnsNullIfNotFound()
    {
        // Arrange
        var dbName = $"PackageRepo_GetByIdWithDetails_Null_{Guid.NewGuid()}";
        using var dbContext = CreateDbContext(dbName);
        var repo = new PackageRepository(dbContext);

        // Act
        var result = await repo.GetByIdWithDetailsAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdWithItemsAsync_ReturnsPackageWithItems()
    {
        // Arrange
        var dbName = $"PackageRepo_GetByIdWithItems_{Guid.NewGuid()}";
        using var dbContext = CreateDbContext(dbName);
        var repo = new PackageRepository(dbContext);
        var item = new Item { Id = 3, Name = "Item3", Description = "Desc", Quantity = 1, Price = 10 };
        var package = new Package
        {
            Id = 3,
            Name = "Package3",
            PackageItems = new List<PackageItem> { new() { Id = 3, ItemId = 3, Item = item, Quantity = 4 } }
        };
        dbContext.Items.Add(item);
        dbContext.Packages.Add(package);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await repo.GetByIdWithItemsAsync(3);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Package3", result.Name);
        Assert.Single(result.PackageItems);
        Assert.Equal(3, result.PackageItems.First().ItemId);
        Assert.Equal("Item3", result.PackageItems.First().Item.Name);
    }
}
