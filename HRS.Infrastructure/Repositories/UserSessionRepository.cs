using HRS.Domain.Entities;
using HRS.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRS.Infrastructure.Repositories;

public class UserSessionRepository : CrudRepository<UserSession>, IUserSessionRepository
{
    public UserSessionRepository(AppDbContext db) : base(db)
    {
    }

    public Task<List<UserSession>> RefreshSessionCandidateAsync()
    {
        return _db.UserSessions
            .Where(s => !s.IsRevoked && s.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(s => s.Id)
            .ToListAsync();
    }

    public Task<List<UserSession>> GetRevokedSessionsAsync()
    {
        return _db.UserSessions
            .Where(s => s.IsRevoked)
            .ToListAsync();
    }

    public Task RevokeAllSessionsAsync(int userId, string reason)
    {
        var sessions = _db.UserSessions
            .Where(s => s.UserId == userId && !s.IsRevoked)
            .ToList();

        foreach (var session in sessions)
        {
            session.IsRevoked = true;
            session.RevokedReason = reason;
        }

        return _db.SaveChangesAsync();
    }

    public async Task<int> CleanUpExpiredSessionsAsync()
    {
        var now = DateTime.UtcNow;
        var sessionsToDelete = await _db.UserSessions
            .Where(s => s.IsRevoked || s.ExpiresAt <= now)
            .ToListAsync();

        _db.UserSessions.RemoveRange(sessionsToDelete);
        await _db.SaveChangesAsync();
        return sessionsToDelete.Count;
    }
}
