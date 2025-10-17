using HRS.API.Contracts.DTOs;
using HRS.API.Contracts.DTOs.User;
using HRS.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRS.API.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<UserDto>>> GetUsersAsync()
    {
        var users = await _userService.GetUsers();
        return Ok(ApiResponse<List<UserDto>>.OkResponse(users.ToList()));
    }

    [HttpGet("{id:int}")]
    [Authorize]
    public async Task<ActionResult<UserDto>> GetUserAsync(int id)
    {
        var user = await _userService.GetUserById(id);
        return Ok(ApiResponse<UserDto>.OkResponse(user));
    }

    [HttpPost("register")]
    public async Task<ActionResult<UserDto>> Register([FromBody] RegisterDto dto)
    {
        var res = await _userService.Register(dto);
        return Ok(ApiResponse<bool>.OkResponse(res, "Registration successful"));
    }

    [HttpGet("employees")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<ActionResult<List<UserDto>>> GetEmployees()
    {
        var employeeList = await _userService.GetEmployees();
        return Ok(ApiResponse<List<UserDto>>.OkResponse(employeeList));
    }

    [HttpPut("employees")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<ActionResult<UserDto>> UpdateEmployee([FromBody] UpdateEmployeeDto dto)
    {
        var updatedEmployee = await _userService.UpdateEmployee(dto);
        if (updatedEmployee == null) return NotFound();
        return Ok(ApiResponse<UserDto>.OkResponse(updatedEmployee));
    }

    [HttpDelete("employees/{id:int}")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> DeleteEmployee(int id)
    {
        var success = await _userService.DeleteEmployee(id);
        if (!success) return NotFound();
        return Ok(ApiResponse<bool>.OkResponse(true, "Employee deleted successfully"));
    }

    [HttpPost("employees/add")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<ActionResult<UserDto>> CreateNewEmployee([FromBody] RegisterEmployeeDetailDto dto)
    {
        var createdUser = await _userService.CreateNewEmployee(dto);
        return Ok(ApiResponse<UserDto>.OkResponse(createdUser, "Employee Created successfully"));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var success = await _userService.DeleteUser(id);
        if (!success) return NotFound();
        return NoContent();
    }
}
