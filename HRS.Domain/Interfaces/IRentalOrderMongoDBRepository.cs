using HRS.Domain.Entities;
using HRS.Domain.Enums;
using MongoDB.Driver;

namespace HRS.Domain.Interfaces;

public interface IRentalOrderMongoDBRepository : ICrudMongoDBRepository<RentalOrderMongoDB>
{

    Task<int> GetReservedQuantityAsync(string itemId, DateTime startDate, DateTime endDate);

    Task<IEnumerable<RentalOrderMongoDB>> GetByStatusesWithDetailsAsync(RentalStatus[] statuses);
    Task<IClientSessionHandle> BeginTransactionAsync();
    Task<RentalOrderMongoDB?> GetByStripeSessionIdAsync(string sessionId);
    Task<IEnumerable<RentalOrderMongoDB>> GetByStatusesAndStoreId(RentalStatus[] statuses, string storeId);
}
