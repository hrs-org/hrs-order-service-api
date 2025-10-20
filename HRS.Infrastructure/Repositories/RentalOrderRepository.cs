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
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<ICollection<RentalOrder>> GetByStatusesWithDetailsAsync(RentalStatus[] statuses)
    {
        return await _dbSet
            .Where(ro => statuses.Contains(ro.Status))
            .ToListAsync();
    }

    public async Task<RentalOrder?> GetByStripeSessionIdAsync(string sessionId) =>
        await _db.RentalOrders.FirstOrDefaultAsync(ro => ro.StripeSessionId == sessionId);

    
}
