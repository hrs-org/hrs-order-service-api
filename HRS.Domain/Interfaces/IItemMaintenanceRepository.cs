using HRS.Domain.Entities;

namespace HRS.Domain.Interfaces;

public interface IItemMaintenanceRepository : ICrudRepository<ItemMaintenance>
{
    Task<int> GetRepairingQuantityAsync(int itemId);
}
