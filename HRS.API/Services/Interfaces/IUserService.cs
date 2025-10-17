using HRS.API.Contracts.DTOs.User;

namespace HRS.API.Services.Interfaces;

public interface IUserService
{
    Task<IEnumerable<UserDto>> GetUsers();
    Task<UserDto> GetUserById(int id);
    Task<bool> Register(RegisterDto dto);
    Task<bool> DeleteUser(int id);
    Task<List<UserDto>> GetEmployees();
    Task<UserDto?> UpdateEmployee(UpdateEmployeeDto dto);
    Task<bool> DeleteEmployee(int id);
    Task<UserDto> CreateNewEmployee(RegisterEmployeeDetailDto dto);
}
