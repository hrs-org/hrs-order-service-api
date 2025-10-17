using FluentAssertions;
using HRS.API.Controllers;
using HRS.API.Contracts.DTOs;
using HRS.API.Contracts.DTOs.Health;
using Microsoft.AspNetCore.Mvc;

namespace HRS.Test.API;

public class HealthControllerTests
{
    private readonly HealthController _controller;

    public HealthControllerTests()
    {
        _controller = new HealthController();
    }

    [Fact]
    public void GetHealth_ShouldReturnOkResult()
    {
        // Act
        var result = _controller.GetHealth();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public void GetHealth_ShouldReturnApiResponseWithHealthCheckDto()
    {
        // Act
        var result = _controller.GetHealth() as OkObjectResult;

        // Assert
        result.Should().NotBeNull();
        result!.Value.Should().BeOfType<ApiResponse<HealthCheckDto>>();

        var apiResponse = result.Value as ApiResponse<HealthCheckDto>;
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Message.Should().Be("Success");
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Errors.Should().BeNull();
    }

    [Fact]
    public void GetHealth_ShouldReturnHealthyStatus()
    {
        // Act
        var result = _controller.GetHealth() as OkObjectResult;
        var apiResponse = result!.Value as ApiResponse<HealthCheckDto>;

        // Assert
        apiResponse!.Data!.Status.Should().Be("Healthy");
    }

    [Fact]
    public void GetHealth_ShouldReturnCurrentTimestamp()
    {
        // Arrange
        var beforeCall = DateTime.UtcNow;

        // Act
        var result = _controller.GetHealth() as OkObjectResult;
        var apiResponse = result!.Value as ApiResponse<HealthCheckDto>;
        var afterCall = DateTime.UtcNow;

        // Assert
        apiResponse!.Data!.Timestamp.Should().BeOnOrAfter(beforeCall);
        apiResponse.Data.Timestamp.Should().BeOnOrBefore(afterCall);
        apiResponse.Data.Timestamp.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void GetHealth_ShouldReturnEnvironmentFromEnvironmentVariable()
    {
        // Arrange
        var originalEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        var testEnvironment = "TestEnvironment";

        try
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", testEnvironment);

            // Act
            var result = _controller.GetHealth() as OkObjectResult;
            var apiResponse = result!.Value as ApiResponse<HealthCheckDto>;

            // Assert
            apiResponse!.Data!.Environment.Should().Be(testEnvironment);
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", originalEnv);
        }
    }

    [Fact]
    public void GetHealth_WhenEnvironmentVariableIsNull_ShouldReturnUnknown()
    {
        // Arrange
        var originalEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        try
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);

            // Act
            var result = _controller.GetHealth() as OkObjectResult;
            var apiResponse = result!.Value as ApiResponse<HealthCheckDto>;

            // Assert
            apiResponse!.Data!.Environment.Should().Be("Unknown");
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", originalEnv);
        }
    }

    [Fact]
    public void GetHealth_WhenEnvironmentVariableIsEmpty_ShouldReturnUnknown()
    {
        // Arrange
        var originalEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        try
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", string.Empty);

            // Act
            var result = _controller.GetHealth() as OkObjectResult;
            var apiResponse = result!.Value as ApiResponse<HealthCheckDto>;

            // Assert
            apiResponse!.Data!.Environment.Should().Be("Unknown");
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", originalEnv);
        }
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Staging")]
    [InlineData("Production")]
    [InlineData("Testing")]
    public void GetHealth_WithDifferentEnvironments_ShouldReturnCorrectEnvironment(string environment)
    {
        // Arrange
        var originalEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        try
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", environment);

            // Act
            var result = _controller.GetHealth() as OkObjectResult;
            var apiResponse = result!.Value as ApiResponse<HealthCheckDto>;

            // Assert
            apiResponse!.Data!.Environment.Should().Be(environment);
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", originalEnv);
        }
    }

    [Fact]
    public void GetHealth_ShouldHaveCorrectHttpGetAttribute()
    {
        // Arrange
        var methodInfo = typeof(HealthController).GetMethod(nameof(HealthController.GetHealth));

        // Assert
        methodInfo.Should().NotBeNull();
        var httpGetAttribute = methodInfo!.GetCustomAttributes(typeof(HttpGetAttribute), false);
        httpGetAttribute.Should().HaveCount(1);
    }

    [Fact]
    public void HealthController_ShouldHaveCorrectAttributes()
    {
        // Arrange
        var controllerType = typeof(HealthController);

        // Assert
        var apiControllerAttribute = controllerType.GetCustomAttributes(typeof(ApiControllerAttribute), false);
        apiControllerAttribute.Should().HaveCount(1);

        var routeAttribute = controllerType.GetCustomAttributes(typeof(RouteAttribute), false);
        routeAttribute.Should().HaveCount(1);

        var route = routeAttribute.First() as RouteAttribute;
        route!.Template.Should().Be("api/health");
    }
}
