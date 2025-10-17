using HRS.Domain.Entities;

namespace HRS.Domain.Interfaces;

public interface IUserVerificationRepository : ICrudRepository<UserVerification>
{
    Task<UserVerification?> ValidateAndConsumeAsync(string token, string type);

    Task RevokeExistingAsync(int userId, string type);
}
