using FluentAssertions;
using HRS.API.Services;
using HRS.API.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Xunit;
namespace HRS.Test.API.Services;
public class AppConfigurationTests
{
    [Fact]
    public void Setup_ShouldReadJwtSettingsFromConfiguration()
    {
        // Arrange
        var config = Substitute.For<IConfiguration>();
        config["Jwt:Key"].Returns("test-key");
        config["Jwt:Issuer"].Returns("test-issuer");
        config["Jwt:Audience"].Returns("test-audience");

        // Act
        var appConfig = new AppConfiguration(config);

        // Assert
        appConfig.JwtKey.Should().Be("test-key");
        appConfig.JwtIssuer.Should().Be("test-issuer");
        appConfig.JwtAudience.Should().Be("test-audience");
    }

    [Fact]
    public void Setup_ShouldUseEmptyStringIfConfigurationIsNull()
    {
        // Arrange
        var config = Substitute.For<IConfiguration>();
        config["Jwt:Key"].Returns((string?)null);
        config["Jwt:Issuer"].Returns((string?)null);
        config["Jwt:Audience"].Returns((string?)null);

        // Act
        var appConfig = new AppConfiguration(config);

        // Assert
        appConfig.JwtKey.Should().BeEmpty();
        appConfig.JwtIssuer.Should().BeEmpty();
        appConfig.JwtAudience.Should().BeEmpty();
    }
}
