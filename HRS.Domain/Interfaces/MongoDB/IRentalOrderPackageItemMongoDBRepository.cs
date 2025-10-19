using HRS.Domain.Entities;

namespace HRS.Domain.Interfaces;

public interface IRentalOrderPackageItemMongoDBRepository : ICrudMongoDBRepository<RentalOrderPackageItemMongoDB>
{
    Task<int> GetReservedQuantityFromPackagesAsync(int itemId, DateTime startDate, DateTime endDate);
}


