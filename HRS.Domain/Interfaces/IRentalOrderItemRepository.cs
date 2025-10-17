using HRS.Domain.Entities;

namespace HRS.Domain.Interfaces;

public interface IRentalOrderItemRepository : ICrudRepository<RentalOrderItem>
{
    Task<int> GetReservedQuantityAsync(int itemId, DateTime startDate, DateTime endDate);
}
