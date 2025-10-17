using System.Security.Claims;
using AutoMapper;
using HRS.API.Contracts.DTOs.User;
using HRS.API.Services;
using HRS.Domain.Entities;
using HRS.Domain.Enums;
using HRS.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace HRS.Test.API.Services;

public class UserContextServiceTests
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IMapper _mapper;
    private readonly UserContextService _mockContextService;
    private readonly IUserRepository _userRepository;

    public UserContextServiceTests()
    {
        _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        _userRepository = Substitute.For<IUserRepository>();
        _mapper = Substitute.For<IMapper>();
        _mockContextService = new UserContextService(_httpContextAccessor, _userRepository, _mapper);
    }

    [Fact]
    public async Task GetUserAsync_ReturnsUser_WhenAuthenticated()
    {
        // Arrange
        var userId = 1;
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = Substitute.For<ClaimsIdentity>();
        identity.IsAuthenticated.Returns(true);
        identity.FindFirst(ClaimTypes.NameIdentifier).Returns(claims[0]);
        var principal = new ClaimsPrincipal(identity);
        var context = Substitute.For<HttpContext>();
        context.User.Returns(principal);
        _httpContextAccessor.HttpContext.Returns(context);

        var user = new User
        {
            Id = userId,
            FirstName = "Test",
            LastName = "User",
            Email = "test@hrs.com",
            PasswordHash = "hash",
            IsVerified = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _userRepository.GetByIdAsync(userId).Returns(user);

        // Act
        var result = await _mockContextService.GetUserAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
        Assert.Equal("Test", result.FirstName);
        Assert.Equal("User", result.LastName);
    }

    [Fact]
    public async Task GetUserIdAsync_ReturnsUserId_WhenAuthenticated()
    {
        // Arrange
        var userId = 1;
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = Substitute.For<ClaimsIdentity>();
        identity.IsAuthenticated.Returns(true);
        identity.FindFirst(ClaimTypes.NameIdentifier).Returns(claims[0]);
        var principal = new ClaimsPrincipal(identity);
        var context = Substitute.For<HttpContext>();
        context.User.Returns(principal);
        _httpContextAccessor.HttpContext.Returns(context);

        var user = new User
        {
            Id = userId,
            FirstName = "Test",
            LastName = "User",
            Email = "test@hrs.com",
            PasswordHash = "hash",
            IsVerified = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _userRepository.GetByIdAsync(userId).Returns(user);

        // Act
        var result = await _mockContextService.GetUserIdAsync();

        // Assert
        Assert.Equal(userId, result);
    }

    [Fact]
    public async Task GetUserDtoAsync_ReturnsUserDto_WhenAuthenticated()
    {
        // Arrange
        var userId = 1;
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = Substitute.For<ClaimsIdentity>();
        identity.IsAuthenticated.Returns(true);
        identity.FindFirst(ClaimTypes.NameIdentifier).Returns(claims[0]);
        var principal = new ClaimsPrincipal(identity);
        var context = Substitute.For<HttpContext>();
        context.User.Returns(principal);
        _httpContextAccessor.HttpContext.Returns(context);

        var user = new User
        {
            Id = userId,
            FirstName = "Test",
            LastName = "User",
            Email = "test@hrs.com",
            Role = UserRole.Admin,
            PasswordHash = "hash",
            IsVerified = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var userDto = new UserDto
        {
            Id = userId,
            FirstName = "Test",
            LastName = "User",
            Email = "test@hrs.com",
            Role = nameof(UserRole.Admin)
        };
        _userRepository.GetByIdAsync(userId).Returns(user);
        _mapper.Map<UserDto>(user).Returns(userDto);

        // Act
        var result = await _mockContextService.GetUserDtoAsync();

        // Assert
        Assert.Equal(result, userDto);
    }

    [Fact]
    public async Task GetUserAsync_Throws_WhenNotAuthenticated()
    {
        // Arrange
        var identity = Substitute.For<ClaimsIdentity>();
        identity.IsAuthenticated.Returns(false);
        var principal = new ClaimsPrincipal(identity);
        var context = Substitute.For<HttpContext>();
        context.User.Returns(principal);
        _httpContextAccessor.HttpContext.Returns(context);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _mockContextService.GetUserAsync());
    }

    [Fact]
    public async Task GetUserAsync_Throws_WhenUserIdClaimMissing()
    {
        // Arrange
        var identity = Substitute.For<ClaimsIdentity>();
        identity.IsAuthenticated.Returns(true);
        identity.FindFirst(ClaimTypes.NameIdentifier).Returns((Claim?)null!);
        var principal = new ClaimsPrincipal(identity);
        var context = Substitute.For<HttpContext>();
        context.User.Returns(principal);
        _httpContextAccessor.HttpContext.Returns(context);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _mockContextService.GetUserAsync());
    }

    [Fact]
    public async Task GetUserAsync_Throws_WhenUserNotFound()
    {
        // Arrange
        var userId = 2;
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = Substitute.For<ClaimsIdentity>();
        identity.IsAuthenticated.Returns(true);
        identity.FindFirst(ClaimTypes.NameIdentifier).Returns(claims[0]);
        var principal = new ClaimsPrincipal(identity);
        var context = Substitute.For<HttpContext>();
        context.User.Returns(principal);
        _httpContextAccessor.HttpContext.Returns(context);

        _userRepository.GetByIdAsync(userId).Returns((User?)null!);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _mockContextService.GetUserAsync());
    }
}
