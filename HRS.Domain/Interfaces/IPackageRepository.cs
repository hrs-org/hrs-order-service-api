using HRS.Domain.Entities;

namespace HRS.Domain.Interfaces;

public interface IPackageRepository : ICrudRepository<Package>
{
    Task<IEnumerable<Package>> GetAllWithDetailsAsync();
    Task<Package?> GetByIdWithDetailsAsync(int id);
    Task<Package?> GetByIdWithItemsAsync(int id);
}
