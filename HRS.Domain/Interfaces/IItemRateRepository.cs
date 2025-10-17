using HRS.Domain.Entities;

namespace HRS.Domain.Interfaces;

public interface IItemRateRepository : ICrudRepository<ItemRate>
{
    Task<IEnumerable<ItemRate>> GetRatesByItemIdAsync(int itemId, bool activeOnly = true);
    Task<ItemRate?> GetApplicableRateAsync(int itemId, int rentalDays);
}
