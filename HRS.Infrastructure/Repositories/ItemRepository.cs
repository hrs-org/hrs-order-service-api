using HRS.Domain.Entities;
using HRS.Domain.Enums;
using HRS.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRS.Infrastructure.Repositories;

public class ItemRepository : CrudRepository<Item>, IItemRepository
{
    public ItemRepository(AppDbContext db) : base(db)
    {
    }

    public async Task<Item?> GetByIdWithParentAsync(int id)
    {
        return await _db.Items
            .Include(i => i.Parent)
            .Include(i => i.Rates)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<IEnumerable<Item>> GetRootItemsAsync()
    {
        return await _db.Items
            .Where(i => i.ParentId == null)
            .Include(i => i.Children)
            .Include(i => i.Rates)
            .ToListAsync();
    }

    public async Task<Item?> GetByIdWithChildrenAsync(int id)
    {
        return await _db.Items
            .Include(i => i.Children)
            .Include(i => i.Rates)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task RemoveItem(Item entity)
    {
        var hasActiveOrders = await _db.RentalOrderItems
            .AnyAsync(x => x.ItemId == entity.Id &&
                           x.RentalOrder != null &&
                           x.RentalOrder.Status != RentalStatus.Completed &&
                           x.RentalOrder.Status != RentalStatus.Cancelled);

        if (hasActiveOrders)
            throw new InvalidOperationException("Cannot delete item with active orders.");

        _db.Items.Remove(entity);
        await _db.SaveChangesAsync();
    }

    public async Task<IEnumerable<Item>> SearchAsync(string? keyword)
    {
        return await _db.Items
            .Where(i => string.IsNullOrWhiteSpace(keyword) || EF.Functions.Like(i.Name, $"%{keyword}%"))
            .ToListAsync();
    }
}
