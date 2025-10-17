using HRS.Domain.Entities;
using HRS.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRS.Infrastructure.Repositories;

public class PaymentRepository : CrudRepository<Payment>, IPaymentRepository
{
    public PaymentRepository(AppDbContext db) : base(db)
    {
    }

    public async Task<Payment?> GetByRentalOrderIdAsync(int rentalOrderId)
    {
        return await _db.Payments
            .Where(p => p.RentalOrderId == rentalOrderId)
            .FirstOrDefaultAsync();
    }
}
