using HRS.Domain.Entities;
using HRS.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRS.Infrastructure.Repositories;

public class ItemRateRepository : CrudRepository<ItemRate>, IItemRateRepository
{
    public ItemRateRepository(AppDbContext db) : base(db)
    {
    }

    public async Task<IEnumerable<ItemRate>> GetRatesByItemIdAsync(int itemId, bool activeOnly = true)
    {
        return await _db.ItemRates
            .Where(r => r.ItemId == itemId && (!activeOnly || r.IsActive))
            .OrderBy(r => r.MinDays)
            .ToListAsync();
    }

    public async Task<ItemRate?> GetApplicableRateAsync(int itemId, int rentalDays)
    {
        return await _db.ItemRates
            .Where(r => r.ItemId == itemId && r.IsActive && r.MinDays <= rentalDays)
            .OrderByDescending(r => r.MinDays)
            .FirstOrDefaultAsync();
    }
}
