using HRS.Domain.Entities;
using HRS.Domain.Enums;
using HRS.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRS.Infrastructure.Repositories;

public class RentalOrderRepository : CrudRepository<RentalOrder>, IRentalOrderRepository
{
    public RentalOrderRepository(AppDbContext db) : base(db)
    {
    }

    public async Task<RentalOrder?> GetByIdWithDetailsAsync(int id)
    {
        return await _dbSet
            .Include(o => o.RentalOrderItems)
            .ThenInclude(i => i.Item)
            .Include(o => o.RentalOrderItems)
            .ThenInclude(i => i.ItemRate)
            .Include(o => o.RentalOrderPackages)
            .ThenInclude(p => p.Items)
            .ThenInclude(pi => pi.Item)
            .Include(o => o.RentalOrderPackages)
            .ThenInclude(p => p.Package)
            .Include(o => o.RentalOrderPackages)
            .ThenInclude(p => p.PackageRate)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<ICollection<RentalOrder>> GetByStatusesWithDetailsAsync(RentalStatus[] statuses)
    {
        return await _dbSet
            .Where(ro => statuses.Contains(ro.Status))
            .Include(o => o.Customer)
            .Include(o => o.RentalOrderItems)
            .ThenInclude(i => i.Item)
            .Include(o => o.RentalOrderItems)
            .ThenInclude(i => i.ItemRate)
            .Include(o => o.RentalOrderPackages)
            .ThenInclude(p => p.Items)
            .ThenInclude(pi => pi.Item)
            .Include(o => o.RentalOrderPackages)
            .ThenInclude(p => p.Package)
            .Include(o => o.RentalOrderPackages)
            .ThenInclude(p => p.PackageRate)
            .ToListAsync();
    }

    public async Task<RentalOrder?> GetByStripeSessionIdAsync(string sessionId) =>
        await _db.RentalOrders.FirstOrDefaultAsync(ro => ro.StripeSessionId == sessionId);
}
