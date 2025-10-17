using System.Security;
using HRS.API.Services.Helpers;
using HRS.API.Services.Interfaces;
using HRS.Domain.Entities;
using HRS.Domain.Interfaces;

namespace HRS.API.Services;

public class UserSessionService : IUserSessionService
{
    private readonly IJwtService _jwt;
    private readonly TimeSpan _refreshTtl = TimeSpan.FromDays(30);
    private readonly IUserSessionRepository _userSessionRepository;

    public UserSessionService(IUserSessionRepository userSessionRepository, IJwtService jwt)
    {
        _userSessionRepository = userSessionRepository;
        _jwt = jwt;
    }

    public async Task<(string accessToken, string refreshToken)> CreateAsync(int userId)
    {
        var refreshToken = TokenHelper.GenerateRefreshToken();
        var (hash, salt) = TokenHelper.HashToken(refreshToken);

        var session = new UserSession
        {
            UserId = userId,
            RefreshTokenHash = hash,
            RefreshTokenSalt = salt,
            ExpiresAt = DateTime.UtcNow.Add(_refreshTtl),
            CreatedAt = DateTime.UtcNow
        };
        await _userSessionRepository.AddAsync(session);
        await _userSessionRepository.SaveChangesAsync();

        var accessToken = await _jwt.GenerateAccessToken(userId);
        return (accessToken, refreshToken);
    }

    public async Task<(string accessToken, string refreshToken)> RefreshAsync(string refreshToken)
    {
        // Find candidate sessions by expiry first (index on ExpiresAt, IsRevoked)
        var candidates = await _userSessionRepository.RefreshSessionCandidateAsync();

        // verify hash
        var session = candidates.FirstOrDefault(s =>
            TokenHelper.Verify(refreshToken, s.RefreshTokenHash, s.RefreshTokenSalt));

        if (session is null)
        {
            // Possible token reuse: try find any revoked match to trigger chain revocation
            var revoked = await _userSessionRepository.GetRevokedSessionsAsync();

            var reused = revoked.FirstOrDefault(s =>
                TokenHelper.Verify(refreshToken, s.RefreshTokenHash, s.RefreshTokenSalt));

            if (reused != null)
                // Revoke all sessions for that user
                await RevokeAllForUserAsync(reused.UserId, "Refresh token reuse detected");
            throw new SecurityException("Invalid refresh token.");
        }

        // Rotate: revoke old, create new
        session.IsRevoked = true;
        session.RevokedReason = "Rotated";

        var newToken = TokenHelper.GenerateRefreshToken();
        var (hash, salt) = TokenHelper.HashToken(newToken);

        var newSession = new UserSession
        {
            UserId = session.UserId,
            RefreshTokenHash = hash,
            RefreshTokenSalt = salt,
            ExpiresAt = DateTime.UtcNow.Add(_refreshTtl),
            CreatedAt = DateTime.UtcNow
        };

        await _userSessionRepository.AddAsync(newSession);
        await _userSessionRepository.SaveChangesAsync();

        var accessToken = await _jwt.GenerateAccessToken(session.UserId);
        return (accessToken, newToken);
    }

    public async Task RevokeAsync(int sessionId, string reason)
    {
        var s = await _userSessionRepository.GetByIdAsync(sessionId)
                ?? throw new KeyNotFoundException("Session not found");
        if (!s.IsRevoked)
        {
            s.IsRevoked = true;
            s.RevokedReason = reason;
            await _userSessionRepository.SaveChangesAsync();
        }
    }

    public async Task RevokeAllForUserAsync(int userId, string reason) => await _userSessionRepository.RevokeAllSessionsAsync(userId, reason);

    public async Task<int> CleanupExpiredAsync() => await _userSessionRepository.CleanUpExpiredSessionsAsync();
}
