using HRS.Domain.Entities;

namespace HRS.Domain.Interfaces;

public interface IUserSessionRepository : ICrudRepository<UserSession>
{
    Task<List<UserSession>> RefreshSessionCandidateAsync();
    Task<List<UserSession>> GetRevokedSessionsAsync();
    Task RevokeAllSessionsAsync(int userId, string reason);
    Task<int> CleanUpExpiredSessionsAsync();
}
