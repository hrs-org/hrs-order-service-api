namespace HRS.API.Services.Interfaces;

public interface IUserSessionService
{
    Task<(string accessToken, string refreshToken)> CreateAsync(int userId);

    Task<(string accessToken, string refreshToken)> RefreshAsync(string refreshToken);

    Task RevokeAsync(int sessionId, string reason);

    Task RevokeAllForUserAsync(int userId, string reason);

    Task<int> CleanupExpiredAsync();
}
