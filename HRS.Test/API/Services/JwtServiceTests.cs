using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using HRS.API.Services;
using HRS.API.Services.Interfaces;
using HRS.Domain.Entities;
using HRS.Domain.Enums;
using HRS.Domain.Interfaces;
using NSubstitute;

namespace HRS.Test.API.Services;

public class JwtServiceTests
{
    private readonly IAppConfiguration _appConfiguration;
    private readonly JwtService _service;
    private readonly IUserRepository _userRepository;

    public JwtServiceTests()
    {
        _userRepository = Substitute.For<IUserRepository>();
        _appConfiguration = Substitute.For<IAppConfiguration>();
        _service = new JwtService(_appConfiguration, _userRepository);
    }

    [Fact]
    public async Task GenerateAccessToken_ReturnsTokenWithClaims()
    {
        // Arrange
        var user = new User { Id = 1, Email = "admin@hrs.com", Role = UserRole.Admin };
        _userRepository.GetByIdAsync(1).Returns(user);
        _appConfiguration.JwtKey.Returns("longKeyForJwtKeyMustBe256Bit123123123123123123");
        _appConfiguration.JwtIssuer.Returns("Issuer");
        _appConfiguration.JwtAudience.Returns("Audience");

        // Act
        var tokenString = await _service.GenerateAccessToken(1);

        // Assert
        Assert.False(string.IsNullOrEmpty(tokenString));
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(tokenString);
        Assert.Equal(user.Id.ToString(), token.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
        Assert.Equal(user.Role.ToString(), token.Claims.First(c => c.Type == ClaimTypes.Role).Value);
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsBase64String()
    {
        // Act
        var refreshToken = _service.GenerateRefreshToken();

        // Assert
        Assert.False(string.IsNullOrEmpty(refreshToken));
        var bytes = Convert.FromBase64String(refreshToken);
        Assert.True(bytes.Length >= 32);
    }
}
