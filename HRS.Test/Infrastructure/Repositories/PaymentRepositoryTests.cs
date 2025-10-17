using System.Threading.Tasks;
using HRS.Domain.Entities;
using HRS.Infrastructure;
using HRS.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HRS.Test.Infrastructure.Repositories;

public class PaymentRepositoryTests
{
    private static AppDbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetByRentalOrderIdAsync_ReturnsPaymentIfExists()
    {
        // Arrange
        var dbName = $"PaymentRepo_{System.Guid.NewGuid()}";
        using var dbContext = CreateDbContext(dbName);
        var repo = new PaymentRepository(dbContext);
        var payment = new Payment { Id = 1, RentalOrderId = 123, Amount = 100 };
        dbContext.Payments.Add(payment);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await repo.GetByRentalOrderIdAsync(123);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal(123, result.RentalOrderId);
    }

    [Fact]
    public async Task GetByRentalOrderIdAsync_ReturnsNullIfNotFound()
    {
        // Arrange
        var dbName = $"PaymentRepo_{System.Guid.NewGuid()}";
        using var dbContext = CreateDbContext(dbName);
        var repo = new PaymentRepository(dbContext);

        // Act & Assert
        var result = await repo.GetByRentalOrderIdAsync(999);
        Assert.Null(result);
    }
}
