using HRS.Domain.Entities;
using HRS.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRS.Infrastructure.Repositories;

public class UserVerificationRepository : CrudRepository<UserVerification>, IUserVerificationRepository
{
    public UserVerificationRepository(AppDbContext db) : base(db)
    {
    }

    public async Task<UserVerification?> ValidateAndConsumeAsync(string token, string type)
    {
        var verification = await _db.UserVerifications
            .FirstOrDefaultAsync(x =>
                x.Token == token &&
                x.Type == type &&
                x.ConsumedAt == null &&
                x.Expiry > DateTime.UtcNow);

        if (verification == null) return null;

        verification.ConsumedAt = DateTime.UtcNow;
        verification.IsUsed = true;
        await _db.SaveChangesAsync();
        return verification;
    }

    public Task RevokeExistingAsync(int userId, string type)
    {
        var now = DateTime.UtcNow;
        var verifications = _db.UserVerifications
            .Where(x => x.UserId == userId && x.Type == type && x.ConsumedAt == null && x.Expiry > now)
            .ToList();

        foreach (var v in verifications)
        {
            v.ConsumedAt = now;
        }

        return _db.SaveChangesAsync();
    }
}
