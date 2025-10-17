using HRS.Domain.Entities;
using HRS.Domain.Enums;
using HRS.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRS.Infrastructure.Repositories;

public class RentalOrderPackageItemRepository : CrudRepository<RentalOrderPackageItem>, IRentalOrderPackageItemRepository
{
    public RentalOrderPackageItemRepository(AppDbContext db) : base(db)
    {
    }

    public async Task<int> GetReservedQuantityFromPackagesAsync(int itemId, DateTime startDate, DateTime endDate)
    {
        return await _db.RentalOrderPackageItems
            .Include(rpi => rpi.RentalOrderPackage)
            .ThenInclude(rp => rp!.RentalOrder)
            .Where(rpi => rpi.ItemId == itemId &&
                          (rpi.RentalOrderPackage!.RentalOrder!.Status == RentalStatus.Booked ||
                           rpi.RentalOrderPackage.RentalOrder.Status == RentalStatus.Rented) &&
                          rpi.RentalOrderPackage.RentalOrder.StartDate <= endDate &&
                          rpi.RentalOrderPackage.RentalOrder.EndDate >= startDate)
            .SumAsync(rpi => (int?)rpi.QuantityPerPackageSnapshot *
                rpi.RentalOrderPackage!.Quantity ?? 0);
    }
}
