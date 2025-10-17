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

public class PackageRateRepositoryTests
{
    private static AppDbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetRatesByPackageIdAsync_ReturnsActiveRatesOrderedByMinDays()
    {
        // Arrange
        var dbName = $"PackageRateRepo_GetRates_{Guid.NewGuid()}";
        using var dbContext = CreateDbContext(dbName);
        var repo = new PackageRateRepository(dbContext);
        dbContext.PackageRates.AddRange(
            new PackageRate { Id = 1, PackageId = 1, MinDays = 1, IsActive = true },
            new PackageRate { Id = 2, PackageId = 1, MinDays = 3, IsActive = true },
            new PackageRate { Id = 3, PackageId = 1, MinDays = 2, IsActive = false },
            new PackageRate { Id = 4, PackageId = 2, MinDays = 1, IsActive = true }
        );
        await dbContext.SaveChangesAsync();

        // Act
        var result = (await repo.GetRatesByPackageIdAsync(1)).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(1, result[0].MinDays);
        Assert.Equal(3, result[1].MinDays);
    }

    [Theory]
    [InlineData(1, 1, 1)]
    [InlineData(1, 2, 1)]
    [InlineData(1, 3, 2)]
    [InlineData(1, 4, 2)]
    [InlineData(1, 0, null)]
    public async Task GetApplicableRateAsync_ReturnsCorrectRate(int packageId, int rentalDays, int? expectedId)
    {
        var dbName = $"PackageRateRepo_GetApplicable_{Guid.NewGuid()}";
        using var dbContext = CreateDbContext(dbName);
        var repo = new PackageRateRepository(dbContext);
        dbContext.PackageRates.AddRange(
            new PackageRate { Id = 1, PackageId = 1, MinDays = 1, IsActive = true },
            new PackageRate { Id = 2, PackageId = 1, MinDays = 3, IsActive = true },
            new PackageRate { Id = 3, PackageId = 1, MinDays = 5, IsActive = false },
            new PackageRate { Id = 4, PackageId = 2, MinDays = 1, IsActive = true }
        );
        await dbContext.SaveChangesAsync();

        var result = await repo.GetApplicableRateAsync(packageId, rentalDays);
        if (expectedId == null)
            Assert.Null(result);
        else
            Assert.Equal(expectedId, result!.Id);
    }
}
