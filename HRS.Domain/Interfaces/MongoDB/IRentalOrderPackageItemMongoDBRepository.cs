using HRS.Domain.Entities;

namespace HRS.Domain.Interfaces;

public interface IRentalOrderPackageItemMongoDBRepository : ICrudMongoDBRepository<RentalOrderPackageItemMongoDB>
{
    Task<int> GetReservedQuantityFromPackagesAsync(string itemId, DateTime startDate, DateTime endDate);
}


