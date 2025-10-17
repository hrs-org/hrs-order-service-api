using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using HRS.API.Services.Interfaces;
using HRS.Domain.Interfaces;
using Microsoft.IdentityModel.Tokens;

namespace HRS.API.Services;

public class JwtService : IJwtService
{
    private readonly IAppConfiguration _appConfiguration;
    private readonly IUserRepository _userRepository;

    public JwtService(IAppConfiguration appConfiguration, IUserRepository userRepository)
    {
        _appConfiguration = appConfiguration;
        _userRepository = userRepository;
    }

    public async Task<string> GenerateAccessToken(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null) throw new KeyNotFoundException("User not found");

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_appConfiguration.JwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            _appConfiguration.JwtIssuer,
            _appConfiguration.JwtAudience,
            claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
}
