using HRS.Domain.Entities;

namespace HRS.Domain.Interfaces;

public interface IRentalOrderPackageItemRepository : ICrudRepository<RentalOrderPackageItem>
{
    Task<int> GetReservedQuantityFromPackagesAsync(int itemId, DateTime startDate, DateTime endDate);
}
