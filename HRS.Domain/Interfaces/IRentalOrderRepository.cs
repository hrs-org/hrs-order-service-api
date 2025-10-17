using HRS.Domain.Entities;
using HRS.Domain.Enums;

namespace HRS.Domain.Interfaces;

public interface IRentalOrderRepository : ICrudRepository<RentalOrder>
{
    Task<RentalOrder?> GetByIdWithDetailsAsync(int id);
    Task<ICollection<RentalOrder>> GetByStatusesWithDetailsAsync(RentalStatus[] statuses);
    Task<RentalOrder?> GetByStripeSessionIdAsync(string sessionId);
}
