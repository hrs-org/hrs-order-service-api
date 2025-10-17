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

public class UserSessionRepositoryTests
{
    private static AppDbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task RefreshSessionCandidateAsync_ReturnsActiveSessions()
    {
        // Arrange
        var dbName = $"SessionRepoDb_{nameof(RefreshSessionCandidateAsync_ReturnsActiveSessions)}_{Guid.NewGuid()}";
        using var dbContext = CreateDbContext(dbName);
        var repo = new UserSessionRepository(dbContext);
        dbContext.UserSessions.AddRange(
            new UserSession { Id = 1, UserId = 1, IsRevoked = false, ExpiresAt = DateTime.UtcNow.AddMinutes(10), RefreshTokenHash = "h", RefreshTokenSalt = "s" },
            new UserSession { Id = 2, UserId = 1, IsRevoked = true, ExpiresAt = DateTime.UtcNow.AddMinutes(10), RefreshTokenHash = "h", RefreshTokenSalt = "s" },
            new UserSession { Id = 3, UserId = 1, IsRevoked = false, ExpiresAt = DateTime.UtcNow.AddMinutes(-10), RefreshTokenHash = "h", RefreshTokenSalt = "s" }
        );
        await dbContext.SaveChangesAsync();

        // Act
        var result = await repo.RefreshSessionCandidateAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
    }

    [Fact]
    public async Task GetRevokedSessionsAsync_ReturnsRevokedSessions()
    {
        // Arrange
        var dbName = $"SessionRepoDb_{nameof(GetRevokedSessionsAsync_ReturnsRevokedSessions)}_{Guid.NewGuid()}";
        using var dbContext = CreateDbContext(dbName);
        var repo = new UserSessionRepository(dbContext);
        dbContext.UserSessions.AddRange(
            new UserSession { Id = 1, UserId = 1, IsRevoked = false, ExpiresAt = DateTime.UtcNow.AddMinutes(10), RefreshTokenHash = "h", RefreshTokenSalt = "s" },
            new UserSession { Id = 2, UserId = 1, IsRevoked = true, ExpiresAt = DateTime.UtcNow.AddMinutes(10), RefreshTokenHash = "h", RefreshTokenSalt = "s" }
        );
        await dbContext.SaveChangesAsync();

        // Act
        var result = await repo.GetRevokedSessionsAsync();

        // Assert
        Assert.Single(result);
        Assert.True(result[0].IsRevoked);
    }

    [Fact]
    public async Task RevokeAllSessionsAsync_RevokesActiveSessions()
    {
        // Arrange
        var dbName = $"SessionRepoDb_{nameof(RevokeAllSessionsAsync_RevokesActiveSessions)}_{Guid.NewGuid()}";
        using var dbContext = CreateDbContext(dbName);
        var repo = new UserSessionRepository(dbContext);
        dbContext.UserSessions.AddRange(
            new UserSession { Id = 1, UserId = 1, IsRevoked = false, ExpiresAt = DateTime.UtcNow.AddMinutes(10), RefreshTokenHash = "h", RefreshTokenSalt = "s" },
            new UserSession { Id = 2, UserId = 1, IsRevoked = false, ExpiresAt = DateTime.UtcNow.AddMinutes(10), RefreshTokenHash = "h", RefreshTokenSalt = "s" },
            new UserSession { Id = 3, UserId = 2, IsRevoked = false, ExpiresAt = DateTime.UtcNow.AddMinutes(10), RefreshTokenHash = "h", RefreshTokenSalt = "s" }
        );
        await dbContext.SaveChangesAsync();

        // Act
        await repo.RevokeAllSessionsAsync(1, "test-reason");

        // Assert
        var sessions = dbContext.UserSessions.Where(s => s.UserId == 1).ToList();
        Assert.All(sessions, s => Assert.True(s.IsRevoked));
        Assert.All(sessions, s => Assert.Equal("test-reason", s.RevokedReason));
    }

    [Fact]
    public async Task CleanUpExpiredSessionsAsync_DeletesExpiredOrRevoked()
    {
        // Arrange
        var dbName = $"SessionRepoDb_{nameof(CleanUpExpiredSessionsAsync_DeletesExpiredOrRevoked)}_{Guid.NewGuid()}";
        using var dbContext = CreateDbContext(dbName);
        var repo = new UserSessionRepository(dbContext);
        dbContext.UserSessions.AddRange(
            new UserSession { Id = 1, UserId = 1, IsRevoked = false, ExpiresAt = DateTime.UtcNow.AddMinutes(10), RefreshTokenHash = "h", RefreshTokenSalt = "s" },
            new UserSession { Id = 2, UserId = 1, IsRevoked = true, ExpiresAt = DateTime.UtcNow.AddMinutes(10), RefreshTokenHash = "h", RefreshTokenSalt = "s" },
            new UserSession { Id = 3, UserId = 1, IsRevoked = false, ExpiresAt = DateTime.UtcNow.AddMinutes(-10), RefreshTokenHash = "h", RefreshTokenSalt = "s" }
        );
        await dbContext.SaveChangesAsync();

        // Act
        var deletedCount = await repo.CleanUpExpiredSessionsAsync();

        // Assert
        Assert.Equal(2, deletedCount);
        Assert.Single(dbContext.UserSessions);
        Assert.False(dbContext.UserSessions.First().IsRevoked);
        Assert.True(dbContext.UserSessions.First().ExpiresAt > DateTime.UtcNow);
    }
}
