using HRS.Domain.Entities;

namespace HRS.Domain.Interfaces;

public interface IRentalOrderItemMongoDBRepository : ICrudMongoDBRepository<RentalOrderItemMongoDB>
{
    Task<int> GetReservedQuantityAsync(string itemId, DateTime startDate, DateTime endDate);
}
