using HRS.Domain.Entities;
using HRS.Domain.Enums;
using HRS.Infrastructure;
using HRS.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace HRS.Test.Infrastructure.Repositories;

public class ItemRepositoryTests
{
    private static AppDbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetParentItemsAsync_ReturnsOnlyParentItemsWithChildren()
    {
        var dbName = $"ItemRepositoryTestsDb_{nameof(GetParentItemsAsync_ReturnsOnlyParentItemsWithChildren)}_{Guid.NewGuid()}";
        using var dbContext = CreateDbContext(dbName);
        var repository = new ItemRepository(dbContext);
        // Arrange
        var user = new User { Id = 1, Email = "test@mail.com", FirstName = "Test", LastName = "User", PasswordHash = "as231sdfqwe123q" };
        var parent1 = new Item { Id = 1, Name = "Parent1", Description = "This is parent1", Quantity = 10, Price = 10, CreatedBy = user };
        var parent2 = new Item { Id = 2, Name = "Parent2", Description = "This is parent2", Quantity = 10, Price = 10, CreatedBy = user };
        var child1 = new Item { Id = 3, Name = "Child1", Description = "This is child1", Quantity = 10, Price = 10, ParentId = 1, CreatedBy = user };
        parent1.Children = new List<Item> { child1 };
        dbContext.Items.AddRange(parent1, parent2, child1);
        await dbContext.SaveChangesAsync();

        // Act
        var result = (await repository.GetRootItemsAsync()).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, i => i.Id == 1 && i.Children.Any(c => c.Id == 3));
        Assert.Contains(result, i => i.Id == 2 && (i.Children == null || i.Children.Count == 0));
    }

    [Fact]
    public async Task GetByIdWithChildrenAsync_ReturnsItemWithChildren()
    {
        var dbName = $"ItemRepositoryTestsDb_{nameof(GetByIdWithChildrenAsync_ReturnsItemWithChildren)}_{Guid.NewGuid()}";
        using var dbContext = CreateDbContext(dbName);
        var repository = new ItemRepository(dbContext);
        // Arrange
        var user = new User { Id = 1, Email = "test@mail.com", FirstName = "Test", LastName = "User", PasswordHash = "as231sdfqwe123q" };
        var parent = new Item { Id = 10, Name = "Parent", Description = "This is parent", Quantity = 10, Price = 10, CreatedBy = user };
        var child = new Item { Id = 11, Name = "Child", Description = "This is child", Quantity = 10, Price = 10, ParentId = 10, CreatedBy = user };
        parent.Children = new List<Item> { child };
        dbContext.Items.AddRange(parent, child);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await repository.GetByIdWithChildrenAsync(10);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(10, result.Id);
        Assert.Single(result.Children);
        Assert.Equal(11, result.Children.First().Id);
    }

    [Fact]
    public async Task GetByIdWithChildrenAsync_ReturnsNullIfNotFound()
    {
        var dbName = $"ItemRepositoryTestsDb_{nameof(GetByIdWithChildrenAsync_ReturnsNullIfNotFound)}_{Guid.NewGuid()}";
        using var dbContext = CreateDbContext(dbName);
        var repository = new ItemRepository(dbContext);
        // Act
        var result = await repository.GetByIdWithChildrenAsync(999);
        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdWithParentAsync_ReturnsItemWithParentAndRates()
    {
        var dbName = $"ItemRepositoryTestsDb_{nameof(GetByIdWithParentAsync_ReturnsItemWithParentAndRates)}_{Guid.NewGuid()}";
        using var dbContext = CreateDbContext(dbName);
        var repository = new ItemRepository(dbContext);
        var user = new User { Id = 1, Email = "test@mail.com", FirstName = "Test", LastName = "User", PasswordHash = "pw" };
        var parent = new Item { Id = 100, Name = "Parent", Description = "Parent", Quantity = 1, Price = 1, CreatedBy = user };
        var child = new Item { Id = 101, Name = "Child", Description = "Child", Quantity = 2, Price = 2, ParentId = 100, CreatedBy = user };
        var rate = new ItemRate { Id = 1, ItemId = 101, MinDays = 1, DailyRate = 10, IsActive = true };
        child.Parent = parent;
        child.Rates = new List<ItemRate> { rate };
        dbContext.Users.Add(user);
        dbContext.Items.AddRange(parent, child);
        dbContext.ItemRates.Add(rate);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await repository.GetByIdWithParentAsync(101);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(101, result.Id);
        Assert.NotNull(result.Parent);
        Assert.Equal(100, result.Parent.Id);
        Assert.Single(result.Rates);
        Assert.Equal(1, result.Rates.First().Id);
    }

    [Fact]
    public async Task RemoveItem_RemovesItem_WhenNoActiveOrders()
    {
        var dbName = $"ItemRepositoryTestsDb_{nameof(RemoveItem_RemovesItem_WhenNoActiveOrders)}_{Guid.NewGuid()}";
        using var dbContext = CreateDbContext(dbName);
        var repository = new ItemRepository(dbContext);
        var item = new Item { Id = 1, Name = "Test", Description = "Test", Quantity = 1, Price = 1 };
        dbContext.Items.Add(item);
        await dbContext.SaveChangesAsync();

        // Act
        await repository.RemoveItem(item);

        // Assert
        Assert.False(dbContext.Items.Any(i => i.Id == 1));
    }

    [Fact]
    public async Task RemoveItem_Throws_WhenHasActiveOrders()
    {
        var dbName = $"ItemRepositoryTestsDb_{nameof(RemoveItem_Throws_WhenHasActiveOrders)}_{Guid.NewGuid()}";
        using var dbContext = CreateDbContext(dbName);
        var repository = new ItemRepository(dbContext);
        var item = new Item { Id = 2, Name = "Test2", Description = "Test2", Quantity = 1, Price = 1 };
        var order = new RentalOrder { Id = 1, Status = RentalStatus.Booked };
        var orderItem = new RentalOrderItem { Id = 1, ItemNameSnapshot = "Test2", ItemId = 2, RentalOrder = order };
        dbContext.Items.Add(item);
        dbContext.RentalOrders.Add(order);
        dbContext.RentalOrderItems.Add(orderItem);
        await dbContext.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.RemoveItem(item));
        Assert.True(dbContext.Items.Any(i => i.Id == 2));
    }
}
