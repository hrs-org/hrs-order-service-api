using HRS.API.Contracts.DTOs.User;
using HRS.Domain.Entities;

namespace HRS.API.Services.Interfaces;

public interface IUserContextService
{
    Task<User> GetUserAsync();

    Task<UserDto> GetUserDtoAsync();

    Task<int> GetUserIdAsync();
}
