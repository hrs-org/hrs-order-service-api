using HRS.Domain.Entities;
using HRS.Infrastructure;
using HRS.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace HRS.Test.Infrastructure.Repositories;

public class UserVerificationRepositoryTests
{
    private static AppDbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task ValidateAndConsumeAsync_ReturnsVerification_WhenValid()
    {
        // Arrange
        var dbName = $"VerificationRepoDb_{nameof(ValidateAndConsumeAsync_ReturnsVerification_WhenValid)}_{Guid.NewGuid()}";
        using var dbContext = CreateDbContext(dbName);
        var repo = new UserVerificationRepository(dbContext);
        var verification = new UserVerification
        {
            Id = 1,
            UserId = 1,
            Token = "token123",
            Type = "email",
            Expiry = DateTime.UtcNow.AddMinutes(10),
            ConsumedAt = null,
            IsUsed = false
        };
        dbContext.UserVerifications.Add(verification);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await repo.ValidateAndConsumeAsync("token123", "email");

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsUsed);
        Assert.NotNull(result.ConsumedAt);
    }

    [Fact]
    public async Task ValidateAndConsumeAsync_ReturnsNull_WhenExpired()
    {
        // Arrange
        var dbName = $"VerificationRepoDb_{nameof(ValidateAndConsumeAsync_ReturnsNull_WhenExpired)}_{Guid.NewGuid()}";
        using var dbContext = CreateDbContext(dbName);
        var repo = new UserVerificationRepository(dbContext);
        var verification = new UserVerification
        {
            Id = 2,
            UserId = 1,
            Token = "tokenExpired",
            Type = "email",
            Expiry = DateTime.UtcNow.AddMinutes(-10),
            ConsumedAt = null,
            IsUsed = false
        };
        dbContext.UserVerifications.Add(verification);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await repo.ValidateAndConsumeAsync("tokenExpired", "email");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateAndConsumeAsync_ReturnsNull_WhenAlreadyConsumed()
    {
        // Arrange
        var dbName = $"VerificationRepoDb_{nameof(ValidateAndConsumeAsync_ReturnsNull_WhenAlreadyConsumed)}_{Guid.NewGuid()}";
        using var dbContext = CreateDbContext(dbName);
        var repo = new UserVerificationRepository(dbContext);
        var verification = new UserVerification
        {
            Id = 3,
            UserId = 1,
            Token = "tokenUsed",
            Type = "email",
            Expiry = DateTime.UtcNow.AddMinutes(10),
            ConsumedAt = DateTime.UtcNow,
            IsUsed = true
        };
        dbContext.UserVerifications.Add(verification);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await repo.ValidateAndConsumeAsync("tokenUsed", "email");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateAndConsumeAsync_ReturnsNull_WhenWrongType()
    {
        // Arrange
        var dbName = $"VerificationRepoDb_{nameof(ValidateAndConsumeAsync_ReturnsNull_WhenWrongType)}_{Guid.NewGuid()}";
        using var dbContext = CreateDbContext(dbName);
        var repo = new UserVerificationRepository(dbContext);
        var verification = new UserVerification
        {
            Id = 4,
            UserId = 1,
            Token = "tokenType",
            Type = "sms",
            Expiry = DateTime.UtcNow.AddMinutes(10),
            ConsumedAt = null,
            IsUsed = false
        };
        dbContext.UserVerifications.Add(verification);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await repo.ValidateAndConsumeAsync("tokenType", "email");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateAndConsumeAsync_ReturnsNull_WhenNotFound()
    {
        // Arrange
        var dbName = $"VerificationRepoDb_{nameof(ValidateAndConsumeAsync_ReturnsNull_WhenNotFound)}_{Guid.NewGuid()}";
        using var dbContext = CreateDbContext(dbName);
        var repo = new UserVerificationRepository(dbContext);

        // Act
        var result = await repo.ValidateAndConsumeAsync("notfound", "email");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task RevokeExistingAsync_RevokesUnconsumedUnexpiredTokens()
    {
        // Arrange
        var dbName = $"VerificationRepoDb_{nameof(RevokeExistingAsync_RevokesUnconsumedUnexpiredTokens)}_{Guid.NewGuid()}";
        using var dbContext = CreateDbContext(dbName);
        var repo = new UserVerificationRepository(dbContext);
        dbContext.UserVerifications.AddRange(
            new UserVerification { Id = 1, UserId = 1, Token = "t1", Type = "email", Expiry = DateTime.UtcNow.AddMinutes(10), ConsumedAt = null },
            new UserVerification { Id = 2, UserId = 1, Token = "t2", Type = "email", Expiry = DateTime.UtcNow.AddMinutes(-10), ConsumedAt = null }, // expired
            new UserVerification
            { Id = 3, UserId = 1, Token = "t3", Type = "email", Expiry = DateTime.UtcNow.AddMinutes(10), ConsumedAt = DateTime.UtcNow }, // already consumed
            new UserVerification
            { Id = 4, UserId = 2, Token = "t4", Type = "email", Expiry = DateTime.UtcNow.AddMinutes(10), ConsumedAt = null } // different user
        );
        await dbContext.SaveChangesAsync();

        // Act
        await repo.RevokeExistingAsync(1, "email");

        // Assert

        var revoked = dbContext.UserVerifications.Where(x => x.UserId == 1 && x.Type == "email" && x.ConsumedAt != null).ToList();
        Assert.Equal(2, revoked.Count);
        Assert.Contains(revoked, v => v.Id == 1 && v.ConsumedAt != null);
        Assert.Contains(revoked, v => v.Id == 3 && v.ConsumedAt != null);
    }
}
