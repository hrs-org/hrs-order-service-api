using HRS.Domain.Entities;

namespace HRS.Domain.Interfaces;

public interface IUserRepository : ICrudRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
    Task UpdateUserAsync(User user);

    Task<List<User>> GetAllEmployee(bool includeManagers = false);
    Task<bool> IsEmailUniqueAsync(string email);
    Task<bool> IsIdUniqueAsync(int id);
}
