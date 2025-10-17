using HRS.Domain.Entities;

namespace HRS.Domain.Interfaces;

public interface IItemRepository : ICrudRepository<Item>
{
    Task<Item?> GetByIdWithParentAsync(int id);
    Task<IEnumerable<Item>> GetRootItemsAsync();
    Task<Item?> GetByIdWithChildrenAsync(int id);
    Task RemoveItem(Item entity);
    Task<IEnumerable<Item>> SearchAsync(string? keyword);
}
