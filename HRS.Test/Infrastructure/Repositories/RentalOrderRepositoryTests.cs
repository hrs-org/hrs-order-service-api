using HRS.Domain.Entities;
using HRS.Domain.Enums;
using HRS.Infrastructure;
using HRS.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace HRS.Test.Infrastructure.Repositories;

public class RentalOrderRepositoryTests
{
    private static AppDbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetByIdWithDetailsAsync_ReturnsOrderWithAllDetails()
    {
        var dbName = $"RentalOrderRepo_GetByIdWithDetails_{Guid.NewGuid()}";
        using var dbContext = CreateDbContext(dbName);
        var repo = new RentalOrderRepository(dbContext);

        // Arrange
        var customer = new User { Id = 1, FirstName = "Test", LastName = "User", Email = "test@mail.com", PasswordHash = "pw" };
        var item = new Item { Id = 1, Name = "Item1", Description = "Desc", Quantity = 1, Price = 10, CreatedBy = customer };
        var itemRate = new ItemRate { Id = 1, ItemId = 1, MinDays = 1, DailyRate = 10, IsActive = true };
        var package = new Package { Id = 1, Name = "Package1" };
        var packageRate = new PackageRate { Id = 1, PackageId = 1, MinDays = 1, DailyRate = 100, IsActive = true };
        var rentalOrderItem = new RentalOrderItem { Id = 1, ItemNameSnapshot = "Item 1", Item = item, ItemRate = itemRate };
        var rentalOrderPackageItem = new RentalOrderPackageItem { Id = 1, ItemNameSnapshot = "Item 1", Item = item };
        var rentalOrderPackage = new RentalOrderPackage
        {
            Id = 1,
            PackageNameSnapshot = "Package1",
            Package = package,
            PackageRate = packageRate,
            Items = new List<RentalOrderPackageItem> { rentalOrderPackageItem }
        };
        var order = new RentalOrder
        {
            Id = 1,
            Customer = customer,
            RentalOrderItems = new List<RentalOrderItem> { rentalOrderItem },
            RentalOrderPackages = new List<RentalOrderPackage> { rentalOrderPackage },
            Status = RentalStatus.Pending
        };
        dbContext.Users.Add(customer);
        dbContext.Items.Add(item);
        dbContext.ItemRates.Add(itemRate);
        dbContext.Packages.Add(package);
        dbContext.PackageRates.Add(packageRate);
        dbContext.RentalOrders.Add(order);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await repo.GetByIdWithDetailsAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.NotNull(result.RentalOrderItems);
        Assert.NotNull(result.RentalOrderPackages);
        Assert.Equal(1, result.RentalOrderItems.First().Item.Id);
        Assert.Equal(1, result.RentalOrderItems.First().ItemRate.Id);
        Assert.Equal(1, result.RentalOrderPackages.First().Package.Id);
        Assert.Equal(1, result.RentalOrderPackages.First().PackageRate.Id);
        Assert.Equal(1, result.RentalOrderPackages.First().Items.First().Item.Id);
    }

    [Fact]
    public async Task GetByIdWithDetailsAsync_ReturnsNullIfNotFound()
    {
        // Arrange
        var dbName = $"RentalOrderRepo_GetByIdWithDetails_Null_{Guid.NewGuid()}";
        using var dbContext = CreateDbContext(dbName);
        var repo = new RentalOrderRepository(dbContext);
        var result = await repo.GetByIdWithDetailsAsync(999);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByStatusesWithDetailsAsync_ReturnsOrdersWithStatusesAndDetails()
    {
        // Arrange
        var dbName = $"RentalOrderRepo_GetByStatusesWithDetails_{Guid.NewGuid()}";
        using var dbContext = CreateDbContext(dbName);
        var repo = new RentalOrderRepository(dbContext);
        var customer = new User { Id = 1, FirstName = "Test", LastName = "User", Email = "test@mail.com", PasswordHash = "pw" };
        var order1 = new RentalOrder { Id = 1, Customer = customer, Status = RentalStatus.Pending };
        var order2 = new RentalOrder { Id = 2, Customer = customer, Status = RentalStatus.Completed };
        dbContext.Users.Add(customer);
        dbContext.RentalOrders.AddRange(order1, order2);
        await dbContext.SaveChangesAsync();

        // Act & Assert
        var result = await repo.GetByStatusesWithDetailsAsync(new[] { RentalStatus.Pending });
        Assert.Single(result);
        Assert.Equal(1, result.First().Id);
        Assert.Equal(RentalStatus.Pending, result.First().Status);
    }

    [Fact]
    public async Task GetByStripeSessionIdAsync_ReturnsCorrectOrderOrNull()
    {
        // Arrange
        var dbName = $"RentalOrderRepo_GetByStripeSessionId_{Guid.NewGuid()}";
        using var dbContext = CreateDbContext(dbName);
        var repo = new RentalOrderRepository(dbContext);
        var order = new RentalOrder { Id = 1, StripeSessionId = "sess_123" };
        dbContext.RentalOrders.Add(order);
        await dbContext.SaveChangesAsync();

        // Act & Assert
        var found = await repo.GetByStripeSessionIdAsync("sess_123");
        Assert.NotNull(found);
        Assert.Equal(1, found.Id);

        var notFound = await repo.GetByStripeSessionIdAsync("not_found");
        Assert.Null(notFound);
    }
}
