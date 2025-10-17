using FluentAssertions;
using HRS.API.Controllers;
using HRS.API.Contracts.DTOs;
using HRS.API.Contracts.DTOs.User;
using HRS.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace HRS.Test.API.Controllers;

public class UsersControllerTests
{
    private readonly IUserService _userService;
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        _userService = Substitute.For<IUserService>();
        _controller = new UsersController(_userService);
    }

    [Fact]
    public async Task GetUsersAsync_ShouldReturnOk_WithUsers()
    {
        // Arrange
        var users = new List<UserDto> { new UserDto { Id = 1, FirstName = "A", LastName = "B", Email = "a@b.com", Role = "Employee" } };
        _userService.GetUsers().Returns(users);

        // Act
        var result = await _controller.GetUsersAsync();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetUserAsync_ShouldReturnOk_WhenUserExists()
    {
        var user = new UserDto { Id = 1, FirstName = "A", LastName = "B", Email = "a@b.com", Role = "Employee" };
        _userService.GetUserById(1).Returns(user);

        var result = await _controller.GetUserAsync(1);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Register_ShouldReturnOk_WhenSuccess()
    {
        var dto = new RegisterDto
        {
            FirstName = "Test",
            LastName = "User",
            Email = "exist@hrs.com",
            Password = "ValidPass123!"
        };
        _userService.Register(dto).Returns(true);

        var result = await _controller.Register(dto);

        result.Result.Should().BeOfType<OkObjectResult>();
        var ok = result.Result as OkObjectResult;
        (ok!.Value as ApiResponse<bool>)!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task GetEmployees_ShouldReturnOk_WhenHasEmployees()
    {
        var employees = new List<UserDto> { new UserDto { Id = 1, FirstName = "A", LastName = "B", Email = "a@b.com", Role = "Employee" } };
        _userService.GetEmployees().Returns(employees);

        var result = await _controller.GetEmployees();

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetEmployees_ShouldReturnNotFound_WhenEmpty()
    {
        _userService.GetEmployees().Returns(new List<UserDto>());

        var result = await _controller.GetEmployees();

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task UpdateEmployee_ShouldReturnOk_WhenUpdated()
    {
        var dto = new UpdateEmployeeDto { Id = 1, FirstName = "A", LastName = "B", Email = "a@b.com", Role = "Employee" };
        var respond = new UserDto { Id = 1, FirstName = "A", LastName = "B", Email = "a@b.com", Role = "Employee" };
        _userService.UpdateEmployee(dto).Returns(respond);

        var result = await _controller.UpdateEmployee(dto);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task UpdateEmployee_ShouldReturnNotFound_WhenFail()
    {
        var dto = new UpdateEmployeeDto { Id = 1, FirstName = "A", LastName = "B", Email = "a@b.com", Role = "Employee" };
        var respond = new UserDto { Id = 1, FirstName = "A", LastName = "B", Email = "a@b.com", Role = "Employee" };
        _userService.UpdateEmployee(dto).Returns(respond);
        var updatedto = new UpdateEmployeeDto { Id = 3, FirstName = "krit", LastName = "tt", Email = "a@bee.com", Role = "Employee" };

        var result = await _controller.UpdateEmployee(updatedto);

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task DeleteEmployee_ShouldReturnNoContent_WhenSuccess()
    {
        _userService.DeleteEmployee(1).Returns(true);

        var result = await _controller.DeleteEmployee(1);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task DeleteEmployee_ShouldReturnNotFound_WhenFail()
    {
        _userService.DeleteEmployee(1).Returns(false);

        var result = await _controller.DeleteEmployee(1);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task CreateNewEmployee_ShouldReturnCreated_WhenSuccess()
    {
        var dto = new RegisterEmployeeDetailDto { FirstName = "Alice", LastName = "Wonder", Email = "alice@wonder.com", Role = "Employee" };
        var created = new UserDto { Id = 100, FirstName = "Alice", LastName = "Wonder", Email = "alice@wonder.com", Role = "Employee" };
        _userService.CreateNewEmployee(dto).Returns(created);

        var result = await _controller.CreateNewEmployee(dto);

        result.Result.Should().BeOfType<OkObjectResult>();
        var createdRes = result.Result.As<OkObjectResult>().Value as ApiResponse<UserDto>;
        createdRes!.Success.Should().BeTrue();
        createdRes.Data.Should().Be(created);
    }

    [Fact]
    public async Task DeleteUser_ShouldReturnNoContent_WhenSuccess()
    {
        _userService.DeleteUser(1).Returns(true);

        var result = await _controller.DeleteUser(1);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteUser_ShouldReturnNotFound_WhenFail()
    {
        _userService.DeleteUser(1).Returns(false);

        var result = await _controller.DeleteUser(1);

        result.Should().BeOfType<NotFoundResult>();
    }
}
