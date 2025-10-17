using HRS.Domain.Entities;
using HRS.Domain.Enums;
using HRS.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRS.Infrastructure.Repositories;

public class ItemMaintenanceRepository : CrudRepository<ItemMaintenance>, IItemMaintenanceRepository
{
    public ItemMaintenanceRepository(AppDbContext db) : base(db)
    {
    }

    public async Task<int> GetRepairingQuantityAsync(int itemId)
    {
        return await _db.ItemMaintenances
            .Where(m => m.ItemId == itemId && m.Type == ItemMaintenanceType.Repair)
            .SumAsync(m => m.Quantity - (m.QuantityFixed ?? 0));
    }
}
