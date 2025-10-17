using System.Security.Claims;
using AutoMapper;
using HRS.API.Contracts.DTOs.User;
using HRS.API.Services.Interfaces;
using HRS.Domain.Entities;
using HRS.Domain.Interfaces;

namespace HRS.API.Services;

public class UserContextService : IUserContextService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IMapper _mapper;
    private readonly IUserRepository _userRepository;

    public UserContextService(IHttpContextAccessor httpContextAccessor, IUserRepository userRepository, IMapper mapper)
    {
        _httpContextAccessor = httpContextAccessor;
        _userRepository = userRepository;
        _mapper = mapper;
    }

    public async Task<User> GetUserAsync()
    {
        var principal = _httpContextAccessor.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true) throw new UnauthorizedAccessException("User is not authenticated");

        var id = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        var userIdAvailable = int.TryParse(id, out var userId);

        if (!userIdAvailable) throw new UnauthorizedAccessException("User ID claim not found");

        var user = await _userRepository.GetByIdAsync(userId);

        return user ?? throw new UnauthorizedAccessException("User not found");
    }

    public async Task<UserDto> GetUserDtoAsync() => _mapper.Map<UserDto>(await GetUserAsync());

    public async Task<int> GetUserIdAsync() => (await GetUserAsync()).Id;
}
