using HRS.Domain.Entities;
using HRS.Domain.Enums;
using HRS.Infrastructure;
using HRS.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace HRS.Test.Infrastructure.Repositories;

public class RentalOrderItemRepositoryTests
{
    private static AppDbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetReservedQuantityAsync_ReturnsCorrectSum()
    {
        // Arrange
        var dbName = $"RentalOrderItemRepo_{Guid.NewGuid()}";
        using var dbContext = CreateDbContext(dbName);
        var repo = new RentalOrderItemRepository(dbContext);
        var startDate = new DateTime(2025, 10, 10);
        var endDate = new DateTime(2025, 10, 20);
        var itemId = 1;
        var order1 = new RentalOrder { Id = 1, Status = RentalStatus.Booked, StartDate = new DateTime(2025, 10, 12), EndDate = new DateTime(2025, 10, 15) };
        var order2 = new RentalOrder { Id = 2, Status = RentalStatus.Rented, StartDate = new DateTime(2025, 10, 18), EndDate = new DateTime(2025, 10, 22) };
        var order3 = new RentalOrder { Id = 3, Status = RentalStatus.Cancelled, StartDate = new DateTime(2025, 10, 12), EndDate = new DateTime(2025, 10, 15) };
        var item1 = new RentalOrderItem { Id = 1, ItemNameSnapshot = "Item1", ItemId = itemId, RentalOrder = order1, Quantity = 3 };
        var item2 = new RentalOrderItem { Id = 2, ItemNameSnapshot = "Item2", ItemId = itemId, RentalOrder = order2, Quantity = 4 };
        var item3 = new RentalOrderItem { Id = 3, ItemNameSnapshot = "Item3", ItemId = itemId, RentalOrder = order3, Quantity = 10 };
        dbContext.RentalOrders.AddRange(order1, order2, order3);
        dbContext.RentalOrderItems.AddRange(item1, item2, item3);
        await dbContext.SaveChangesAsync();

        // Act
        var reserved = await repo.GetReservedQuantityAsync(itemId, startDate, endDate);

        // Assert
        Assert.Equal(7, reserved);
    }

    [Fact]
    public async Task GetReservedQuantityAsync_ReturnsZeroIfNoneFound()
    {
        // Arrange
        var dbName = $"RentalOrderItemRepo_Zero_{Guid.NewGuid()}";
        using var dbContext = CreateDbContext(dbName);
        var repo = new RentalOrderItemRepository(dbContext);

        // Act & Assert
        var reserved = await repo.GetReservedQuantityAsync(99, DateTime.Today, DateTime.Today.AddDays(1));
        Assert.Equal(0, reserved);
    }
}
