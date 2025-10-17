namespace HRS.API.Services.Interfaces;

public interface IJwtService
{
    Task<string> GenerateAccessToken(int userId);
    string GenerateRefreshToken();
}
