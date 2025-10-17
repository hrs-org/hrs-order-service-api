using HRS.Domain.Entities;
using HRS.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRS.Infrastructure.Repositories;

public class PackageRepository : CrudRepository<Package>, IPackageRepository
{
    public PackageRepository(AppDbContext db) : base(db)
    {
    }

    public async Task<IEnumerable<Package>> GetAllWithDetailsAsync()
    {
        return await _db.Packages
            .AsNoTracking()
            .Include(p => p.PackageItems)
            .ThenInclude(pi => pi.Item)
            .Include(p => p.PackageRates)
            .ToListAsync();
    }

    public async Task<Package?> GetByIdWithDetailsAsync(int id)
    {
        return await _db.Packages
            .Include(p => p.PackageItems)
            .ThenInclude(pi => pi.Item)
            .Include(p => p.PackageRates)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Package?> GetByIdWithItemsAsync(int id)
    {
        return await _db.Packages
            .Include(p => p.PackageItems)
            .ThenInclude(pi => pi.Item)
            .FirstOrDefaultAsync(p => p.Id == id);
    }
}
