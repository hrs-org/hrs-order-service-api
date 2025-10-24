using System.Security.Claims;
using FluentAssertions;
using HRS.API.Services;
using HRS.Shared.Core.Dtos;
using HRS.Shared.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Xunit;
namespace HRS.Test.API.Services;

public class UserContextServiceTests
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserContextService _service;

    public UserContextServiceTests()
    {
        _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        _service = new UserContextService(_httpContextAccessor);
    }

    [Fact]
    public async Task GetUserAsync_ReturnsUserFromClaims()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "42"),
            new Claim(ClaimTypes.Email, "test@example.com"),
            new Claim(ClaimTypes.GivenName, "John"),
            new Claim(ClaimTypes.Surname, "Doe"),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim("storeId", "7")
        };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);
        var context = new DefaultHttpContext { User = principal };
        _httpContextAccessor.HttpContext.Returns(context);

        // Act
        var result = await _service.GetUserAsync();

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(42);
        result.Email.Should().Be("test@example.com");
        result.FirstName.Should().Be("John");
        result.LastName.Should().Be("Doe");
        result.Role.Should().Be("Admin");
    }

    [Fact]
    public void GetUserId_ReturnsZero_WhenNoClaim()
    {
        _httpContextAccessor.HttpContext.Returns(new DefaultHttpContext());

        var userId = _service.GetUserId();
        userId.Should().Be(0);
    }

    [Fact]
    public void GetStoreId_ReturnsStoreIdFromClaim()
    {
        var claims = new[] { new Claim("storeId", "123") };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);
        var context = new DefaultHttpContext { User = principal };
        _httpContextAccessor.HttpContext.Returns(context);

        var storeId = _service.GetStoreId();
        storeId.Should().Be(123);
    }

    [Fact]
    public void GetStoreId_ReturnsZero_WhenClaimMissing()
    {
        _httpContextAccessor.HttpContext.Returns(new DefaultHttpContext());
        var storeId = _service.GetStoreId();
        storeId.Should().Be(0);
    }
    [Fact]
    public async Task GetUserAsync_UsesFallbackClaims_WhenStandardClaimsMissing()
    {
        // Arrange: no standard claims, only fallback ones
        var claims = new[]
        {
        new Claim("sub", "99"),
        new Claim("firstName", "FallbackFirst"),
        new Claim("lastName", "FallbackLast"),
        new Claim("role", "FallbackRole")
    };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);
        var context = new DefaultHttpContext { User = principal };
        _httpContextAccessor.HttpContext.Returns(context);

        // Act
        var user = await _service.GetUserAsync();

        // Assert
        user.Id.Should().Be(99);                        // fallback "sub" claim used
        user.FirstName.Should().Be("FallbackFirst");    // fallback firstName claim used
        user.LastName.Should().Be("FallbackLast");      // fallback lastName claim used
        user.Role.Should().Be("FallbackRole");          // fallback role claim used
        user.Email.Should().Be("unknown@example.com");  // no email claim, default value
    }

    [Fact]
    public async Task GetUserAsync_UsesDefaultValues_WhenNoClaimsPresent()
    {
        _httpContextAccessor.HttpContext.Returns(new DefaultHttpContext());

        var user = await _service.GetUserAsync();

        user.Id.Should().Be(0);                         // no ID claim
        user.Email.Should().Be("unknown@example.com");  // no email claim
        user.FirstName.Should().Be("Unknown");          // default
        user.LastName.Should().Be("User");              // default
        user.Role.Should().Be("User");                  // default
    }


}
