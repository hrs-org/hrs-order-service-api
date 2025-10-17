using HRS.Domain.Entities;
using HRS.Domain.Enums;
using HRS.Infrastructure;
using HRS.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace HRS.Test.Infrastructure.Repositories;

public class ItemMaintenanceRepositoryTests
{
    private static AppDbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetRepairingQuantityAsync_ReturnsCorrectSum()
    {
        // Arrange
        var dbName = $"ItemMaintenanceRepo_{Guid.NewGuid()}";
        using var dbContext = CreateDbContext(dbName);
        var repo = new ItemMaintenanceRepository(dbContext);
        var itemId = 1;
        dbContext.ItemMaintenances.AddRange(
            new ItemMaintenance { Id = 1, ItemId = itemId, Type = ItemMaintenanceType.Repair, Quantity = 5, QuantityFixed = 2 }, // 5-2=3
            new ItemMaintenance { Id = 2, ItemId = itemId, Type = ItemMaintenanceType.Repair, Quantity = 4, QuantityFixed = null }, // 4-0=4
            new ItemMaintenance { Id = 3, ItemId = itemId, Type = ItemMaintenanceType.Broken, Quantity = 10, QuantityFixed = null }, // not counted
            new ItemMaintenance { Id = 4, ItemId = 2, Type = ItemMaintenanceType.Repair, Quantity = 7, QuantityFixed = 1 } // not counted
        );
        await dbContext.SaveChangesAsync();

        // Act
        var repairing = await repo.GetRepairingQuantityAsync(itemId);

        // Assert
        Assert.Equal(7, repairing);
    }

    [Fact]
    public async Task GetRepairingQuantityAsync_ReturnsZeroIfNoneFound()
    {
        // Arrange
        var dbName = $"ItemMaintenanceRepo_Zero_{Guid.NewGuid()}";
        using var dbContext = CreateDbContext(dbName);
        var repo = new ItemMaintenanceRepository(dbContext);

        // Act
        var repairing = await repo.GetRepairingQuantityAsync(99);

        // Assert
        Assert.Equal(0, repairing);
    }
}
