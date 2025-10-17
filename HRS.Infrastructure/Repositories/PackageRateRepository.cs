using HRS.Domain.Entities;
using HRS.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRS.Infrastructure.Repositories;

public class PackageRateRepository : CrudRepository<PackageRate>, IPackageRateRepository
{
    public PackageRateRepository(AppDbContext db) : base(db)
    {
    }

    public async Task<IEnumerable<PackageRate>> GetRatesByPackageIdAsync(int packageId)
    {
        return await _db.PackageRates
            .Where(r => r.PackageId == packageId && r.IsActive)
            .OrderBy(r => r.MinDays)
            .ToListAsync();
    }

    public async Task<PackageRate?> GetApplicableRateAsync(int packageId, int rentalDays)
    {
        return await _db.PackageRates
            .Where(r => r.PackageId == packageId && r.IsActive && r.MinDays <= rentalDays)
            .OrderByDescending(r => r.MinDays)
            .FirstOrDefaultAsync();
    }
}
