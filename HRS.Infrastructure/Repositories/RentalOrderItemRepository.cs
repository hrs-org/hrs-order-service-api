using HRS.Domain.Entities;
using HRS.Domain.Enums;
using HRS.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRS.Infrastructure.Repositories;

public class RentalOrderItemRepository : CrudRepository<RentalOrderItem>, IRentalOrderItemRepository
{
    public RentalOrderItemRepository(AppDbContext db) : base(db)
    {
    }

    public async Task<int> GetReservedQuantityAsync(int itemId, DateTime startDate, DateTime endDate)
    {
        return await _db.RentalOrderItems
            .Include(ri => ri.RentalOrder)
            .Where(ri => ri.ItemId == itemId &&
                         ri.RentalOrder != null &&
                         (ri.RentalOrder.Status == RentalStatus.Booked ||
                          ri.RentalOrder.Status == RentalStatus.Rented) &&
                         ri.RentalOrder.StartDate <= endDate &&
                         ri.RentalOrder.EndDate >= startDate)
            .SumAsync(ri => (int?)ri.Quantity ?? 0);
    }
}
