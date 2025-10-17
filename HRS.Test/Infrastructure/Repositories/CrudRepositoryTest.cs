using HRS.Domain.Entities;
using HRS.Domain.Enums;
using HRS.Infrastructure;
using HRS.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace HRS.Test.Infrastructure.Repositories;

public class CrudRepositoryTests
{
    private static AppDbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        return new AppDbContext(options);
    }


    [Fact]
    public async Task AddAsync_ShouldAddItem()
    {
        var dbName = $"CrudRepo_AddAsync_{Guid.NewGuid()}";
        using var dbContext = CreateDbContext(dbName);
        var repo = new CrudRepository<Item>(dbContext);
        var user = new User { Id = 2, FirstName = "Evan", LastName = "Feri", Email = "test@mail.com", Role = UserRole.Manager, PasswordHash = "123456" };
        var item = new Item { Id = 1, Name = "TestItem", Description = "Desc", Quantity = 10, Price = 100, CreatedBy = user };

        await repo.AddAsync(item);
        await repo.SaveChangesAsync();

        var result = await dbContext.Items.FindAsync(1);
        Assert.NotNull(result);
        Assert.Equal("TestItem", result!.Name);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnItem()
    {
        var dbName = $"CrudRepo_GetById_{Guid.NewGuid()}";
        using var dbContext = CreateDbContext(dbName);
        var repo = new CrudRepository<Item>(dbContext);
        var user = new User { Id = 2, FirstName = "Evan", LastName = "Feri", Email = "test@mail.com", Role = UserRole.Manager, PasswordHash = "123456" };
        var item = new Item { Id = 2, Name = "Item2", Description = "Desc2", Quantity = 5, Price = 50, CreatedBy = user };
        dbContext.Items.Add(item);
        await dbContext.SaveChangesAsync();

        var result = await repo.GetByIdAsync(2);
        Assert.NotNull(result);
        Assert.Equal("Item2", result!.Name);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllItems()
    {
        var dbName = $"CrudRepo_GetAll_{Guid.NewGuid()}";
        using var dbContext = CreateDbContext(dbName);
        var repo = new CrudRepository<Item>(dbContext);
        var user = new User { Id = 2, FirstName = "Evan", LastName = "Feri", Email = "test@mail.com", Role = UserRole.Manager, PasswordHash = "123456" };
        dbContext.Items.AddRange(
            new Item { Id = 3, Name = "A", Description = "D1", Quantity = 1, Price = 10, CreatedBy = user },
            new Item { Id = 4, Name = "B", Description = "D2", Quantity = 2, Price = 20, CreatedBy = user });
        await dbContext.SaveChangesAsync();

        var result = (await repo.GetAllAsync()).ToList();
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task FindAsync_ShouldReturnFilteredItems()
    {
        var dbName = $"CrudRepo_Find_{Guid.NewGuid()}";
        using var dbContext = CreateDbContext(dbName);
        var repo = new CrudRepository<Item>(dbContext);
        var user = new User { Id = 2, FirstName = "Evan", LastName = "Feri", Email = "test@mail.com", Role = UserRole.Manager, PasswordHash = "123456" };
        dbContext.Items.AddRange(
            new Item { Id = 5, Name = "Apple", Description = "Fruit", Quantity = 10, Price = 5, CreatedBy = user },
            new Item { Id = 6, Name = "Banana", Description = "Fruit", Quantity = 8, Price = 3, CreatedBy = user });
        await dbContext.SaveChangesAsync();

        var result = (await repo.FindAsync(i => i.Name.StartsWith("A"))).ToList();
        Assert.Single(result);
        Assert.Equal("Apple", result[0].Name);
    }

    [Fact]
    public async Task Update_ShouldModifyItem()
    {
        var dbName = $"CrudRepo_Update_{Guid.NewGuid()}";
        using var dbContext = CreateDbContext(dbName);
        var repo = new CrudRepository<Item>(dbContext);
        var user = new User { Id = 2, FirstName = "Evan", LastName = "Feri", Email = "test@mail.com", Role = UserRole.Manager, PasswordHash = "123456" };
        var item = new Item { Id = 7, Name = "OldName", Description = "D", Quantity = 1, Price = 10, CreatedBy = user };
        dbContext.Items.Add(item);
        await dbContext.SaveChangesAsync();

        item.Name = "NewName";
        repo.Update(item);
        await repo.SaveChangesAsync();

        var result = await dbContext.Items.FindAsync(7);
        Assert.Equal("NewName", result!.Name);
    }

    [Fact]
    public async Task Remove_ShouldDeleteItem()
    {
        var dbName = $"CrudRepo_Remove_{Guid.NewGuid()}";
        using var dbContext = CreateDbContext(dbName);
        var repo = new CrudRepository<Item>(dbContext);
        var user = new User { Id = 2, FirstName = "Evan", LastName = "Feri", Email = "test@mail.com", Role = UserRole.Manager, PasswordHash = "123456" };
        var item = new Item { Id = 8, Name = "ToDelete", Description = "D", Quantity = 1, Price = 10, CreatedBy = user };
        dbContext.Items.Add(item);
        await dbContext.SaveChangesAsync();

        repo.Remove(item);
        await repo.SaveChangesAsync();

        var result = await dbContext.Items.FindAsync(8);
        Assert.Null(result);
    }

    [Fact]
    public async Task AddRangeAsync_ShouldAddMultipleItems()
    {
        var dbName = $"CrudRepo_AddRange_{Guid.NewGuid()}";
        using var dbContext = CreateDbContext(dbName);
        var repo = new CrudRepository<Item>(dbContext);
        var user = new User { Id = 2, FirstName = "Evan", LastName = "Feri", Email = "test@mail.com", Role = UserRole.Manager, PasswordHash = "123456" };
        var items = new[]
        {
            new Item { Id = 9, Name = "Item9", Description = "D9", Quantity = 1, Price = 10, CreatedBy = user },
            new Item { Id = 10, Name = "Item10", Description = "D10", Quantity = 2, Price = 20, CreatedBy = user }
        };

        await repo.AddRangeAsync(items);
        await repo.SaveChangesAsync();

        var result = await dbContext.Items.ToListAsync();
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task RemoveRange_ShouldDeleteMultipleItems()
    {
        var dbName = $"CrudRepo_RemoveRange_{Guid.NewGuid()}";
        using var dbContext = CreateDbContext(dbName);
        var repo = new CrudRepository<Item>(dbContext);
        var user = new User { Id = 2, FirstName = "Evan", LastName = "Feri", Email = "test@mail.com", Role = UserRole.Manager, PasswordHash = "123456" };
        var items = new[]
        {
            new Item { Id = 11, Name = "Item11", Description = "D11", Quantity = 1, Price = 10, CreatedBy = user },
            new Item { Id = 12, Name = "Item12", Description = "D12", Quantity = 2, Price = 20, CreatedBy = user }
        };

        dbContext.Items.AddRange(items);
        await dbContext.SaveChangesAsync();

        repo.RemoveRange(items);
        await repo.SaveChangesAsync();

        var result = await dbContext.Items.ToListAsync();
        Assert.Empty(result);
    }
}
