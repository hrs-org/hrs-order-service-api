using HRS.Domain.Entities;
using HRS.Domain.Enums;
using HRS.Infrastructure;
using HRS.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace HRS.Test.Infrastructure.Repositories;

public class RentalOrderPackageItemRepositoryTests
{
    private static AppDbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetReservedQuantityFromPackagesAsync_ReturnsCorrectSum()
    {
        // Arrange
        var dbName = $"RentalOrderPackageItemRepo_{Guid.NewGuid()}";
        using var dbContext = CreateDbContext(dbName);
        var repo = new RentalOrderPackageItemRepository(dbContext);
        var startDate = new DateTime(2025, 10, 10);
        var endDate = new DateTime(2025, 10, 20);
        var itemId = 1;
        var order1 = new RentalOrder { Id = 1, Status = RentalStatus.Booked, StartDate = new DateTime(2025, 10, 12), EndDate = new DateTime(2025, 10, 15) };
        var order2 = new RentalOrder { Id = 2, Status = RentalStatus.Rented, StartDate = new DateTime(2025, 10, 18), EndDate = new DateTime(2025, 10, 22) };
        var order3 = new RentalOrder { Id = 3, Status = RentalStatus.Cancelled, StartDate = new DateTime(2025, 10, 12), EndDate = new DateTime(2025, 10, 15) };
        var pkg1 = new RentalOrderPackage { Id = 1, PackageNameSnapshot = "Package1", RentalOrder = order1, Quantity = 2 };
        var pkg2 = new RentalOrderPackage { Id = 2, PackageNameSnapshot = "Package2", RentalOrder = order2, Quantity = 1 };
        var pkg3 = new RentalOrderPackage { Id = 3, PackageNameSnapshot = "Package3", RentalOrder = order3, Quantity = 5 };
        var itemPkg1 = new RentalOrderPackageItem
        { Id = 1, ItemNameSnapshot = "Item1", ItemId = itemId, RentalOrderPackage = pkg1, QuantityPerPackageSnapshot = 3 };
        var itemPkg2 = new RentalOrderPackageItem
        { Id = 2, ItemNameSnapshot = "Item2", ItemId = itemId, RentalOrderPackage = pkg2, QuantityPerPackageSnapshot = 4 };
        var itemPkg3 = new RentalOrderPackageItem
        { Id = 3, ItemNameSnapshot = "Item3", ItemId = itemId, RentalOrderPackage = pkg3, QuantityPerPackageSnapshot = 10 };
        dbContext.RentalOrders.AddRange(order1, order2, order3);
        dbContext.RentalOrderPackages.AddRange(pkg1, pkg2, pkg3);
        dbContext.RentalOrderPackageItems.AddRange(itemPkg1, itemPkg2, itemPkg3);
        await dbContext.SaveChangesAsync();

        // Act
        var reserved = await repo.GetReservedQuantityFromPackagesAsync(itemId, startDate, endDate);

        // Assert
        Assert.Equal(10, reserved);
    }

    [Fact]
    public async Task GetReservedQuantityFromPackagesAsync_ReturnsZeroIfNoneFound()
    {
        // Arrange
        var dbName = $"RentalOrderPackageItemRepo_Zero_{Guid.NewGuid()}";
        using var dbContext = CreateDbContext(dbName);
        var repo = new RentalOrderPackageItemRepository(dbContext);

        // Act & Assert
        var reserved = await repo.GetReservedQuantityFromPackagesAsync(99, DateTime.Today, DateTime.Today.AddDays(1));
        Assert.Equal(0, reserved);
    }
}
