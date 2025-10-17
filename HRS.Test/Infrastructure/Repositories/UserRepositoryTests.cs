using HRS.Domain.Entities;
using HRS.Domain.Enums;
using HRS.Infrastructure;
using HRS.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace HRS.Test.Infrastructure.Repositories;

public class UserRepositoryTests
{
    private static AppDbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetByEmailAsync_ReturnsUser_WhenExists()
    {
        // Arrange
        var dbName = $"UserRepoDb_{nameof(GetByEmailAsync_ReturnsUser_WhenExists)}_{Guid.NewGuid()}";
        using var dbContext = CreateDbContext(dbName);
        var repository = new UserRepository(dbContext);
        var user = new User { Id = 2, FirstName = "Evan", LastName = "Feri", Email = "test@mail.com", Role = UserRole.Manager, PasswordHash = "123456" };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await repository.GetByEmailAsync("test@mail.com");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test@mail.com", result!.Email);
    }

    [Fact]
    public async Task GetByEmailAsync_ReturnsNull_WhenNotFound()
    {
        // Arrange
        var dbName = $"UserRepoDb_{nameof(GetByEmailAsync_ReturnsNull_WhenNotFound)}_{Guid.NewGuid()}";
        using var dbContext = CreateDbContext(dbName);
        var repository = new UserRepository(dbContext);

        // Act
        var result = await repository.GetByEmailAsync("notfound@mail.com");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateUserAsync_UpdatesToken_WhenUserExists()
    {
        // Arrange
        var dbName = $"UserRepoDb_{nameof(UpdateUserAsync_UpdatesToken_WhenUserExists)}_{Guid.NewGuid()}";
        using var dbContext = CreateDbContext(dbName);
        var repository = new UserRepository(dbContext);
        var user = new User { Id = 1, FirstName = "Evan", LastName = "Feri", Email = "test@mail.com", Role = UserRole.Manager, PasswordHash = "123456" };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        var update = new User
        {
            Id = 1
        };

        // Act
        await repository.UpdateUserAsync(update);

        // Assert
        var updated = await dbContext.Users.FindAsync(1);
        Assert.NotNull(updated);
    }

    [Fact]
    public async Task UpdateUserAsync_Throws_WhenUserNotFound()
    {
        // Arrange
        var dbName = $"UserRepoDb_{nameof(UpdateUserAsync_Throws_WhenUserNotFound)}_{Guid.NewGuid()}";
        using var dbContext = CreateDbContext(dbName);
        var repository = new UserRepository(dbContext);
        var update = new User { Id = 999 };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => repository.UpdateUserAsync(update));
    }

    [Fact]
    public async Task GetAllEmployee_ReturnsOnlyEmployees()
    {
        // Arrange
        var dbName = $"UserRepoDb_{nameof(GetAllEmployee_ReturnsOnlyEmployees)}_{Guid.NewGuid()}";
        using var dbContext = CreateDbContext(dbName);
        var repository = new UserRepository(dbContext);
        dbContext.Users.AddRange(
            new User { Id = 1, FirstName = "Evan", LastName = "Feri", Email = "test@mail.com", Role = UserRole.Employee, PasswordHash = "123456" },
            new User { Id = 2, FirstName = "Jaseper", LastName = "Shen", Email = "test2@mail.com", Role = UserRole.Manager, PasswordHash = "123456" }
        );
        await dbContext.SaveChangesAsync();

        // Act
        var employees = await repository.GetAllEmployee();

        // Assert
        Assert.Single(employees);
        Assert.Equal(UserRole.Employee, employees.First().Role);
    }

    [Fact]
    public async Task IsEmailUniqueAsync_ReturnsFalse_WhenEmailExists()
    {
        // Arrange
        var dbName = $"UserRepoDb_{nameof(IsEmailUniqueAsync_ReturnsFalse_WhenEmailExists)}_{Guid.NewGuid()}";
        using var dbContext = CreateDbContext(dbName);
        var repository = new UserRepository(dbContext);
        dbContext.Users.Add(new User
        { Id = 1, FirstName = "Evan", LastName = "Feri", Email = "test@mail.com", Role = UserRole.Employee, PasswordHash = "123456" });
        await dbContext.SaveChangesAsync();

        // Act
        var result = await repository.IsEmailUniqueAsync("test@mail.com");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsEmailUniqueAsync_ReturnsTrue_WhenEmailNotExists()
    {
        // Arrange
        var dbName = $"UserRepoDb_{nameof(IsEmailUniqueAsync_ReturnsTrue_WhenEmailNotExists)}_{Guid.NewGuid()}";
        using var dbContext = CreateDbContext(dbName);
        var repository = new UserRepository(dbContext);

        // Act
        var result = await repository.IsEmailUniqueAsync("unique@mail.com");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsIdUniqueAsync_ReturnsFalse_WhenIdExists()
    {
        // Arrange
        var dbName = $"UserRepoDb_{nameof(IsIdUniqueAsync_ReturnsFalse_WhenIdExists)}_{Guid.NewGuid()}";
        using var dbContext = CreateDbContext(dbName);
        var repository = new UserRepository(dbContext);
        dbContext.Users.Add(new User
        { Id = 3, FirstName = "Evan", LastName = "Feri", Email = "test@mail.com", Role = UserRole.Employee, PasswordHash = "123456" });
        await dbContext.SaveChangesAsync();

        // Act
        var result = await repository.IsIdUniqueAsync(3);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsIdUniqueAsync_ReturnsTrue_WhenIdNotExists()
    {
        // Arrange
        var dbName = $"UserRepoDb_{nameof(IsIdUniqueAsync_ReturnsTrue_WhenIdNotExists)}_{Guid.NewGuid()}";
        using var dbContext = CreateDbContext(dbName);
        var repository = new UserRepository(dbContext);

        // Act
        var result = await repository.IsIdUniqueAsync(123);

        // Assert
        Assert.True(result);
    }
}
