using System.Security;
using HRS.API.Services;
using HRS.API.Services.Interfaces;
using HRS.Domain.Entities;
using HRS.Domain.Interfaces;
using NSubstitute;

namespace HRS.Test.API.Services;

public class UserSessionServiceTests
{
    private readonly IJwtService _jwtService;
    private readonly UserSessionService _service;
    private readonly IUserSessionRepository _userSessionRepository;

    public UserSessionServiceTests()
    {
        _userSessionRepository = Substitute.For<IUserSessionRepository>();
        _jwtService = Substitute.For<IJwtService>();
        _service = new UserSessionService(_userSessionRepository, _jwtService);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnTokens()
    {
        // Arrange
        _userSessionRepository.AddAsync(Arg.Any<UserSession>()).Returns(Task.CompletedTask);
        _jwtService.GenerateAccessToken(Arg.Any<int>()).Returns(Task.FromResult("access-token"));

        // Act
        var (accessToken, refreshToken) = await _service.CreateAsync(1);

        // Assert
        Assert.Equal("access-token", accessToken);
        Assert.False(string.IsNullOrEmpty(refreshToken));
    }

    [Fact]
    public async Task RefreshAsync_ShouldThrow_WhenSessionNotFound()
    {
        // Arrange
        _userSessionRepository.RefreshSessionCandidateAsync().Returns([]);
        _userSessionRepository.GetRevokedSessionsAsync().Returns([]);

        // Act & Assert
        await Assert.ThrowsAsync<SecurityException>(() => _service.RefreshAsync("invalid-token"));
    }

    [Fact]
    public async Task RevokeAsync_ShouldSetIsRevoked()
    {
        // Arrange
        var session = new UserSession { Id = 1, IsRevoked = false };
        _userSessionRepository.GetByIdAsync(1).Returns(session);

        // Act
        await _service.RevokeAsync(1, "reason");

        // Assert
        Assert.True(session.IsRevoked);
        Assert.Equal("reason", session.RevokedReason);
    }

    [Fact]
    public async Task CleanupExpiredAsync_ShouldCallRepository()
    {
        // Arrange
        _userSessionRepository.CleanUpExpiredSessionsAsync().Returns(5);

        // Act
        var result = await _service.CleanupExpiredAsync();

        // Assert
        Assert.Equal(5, result);
    }
}
