using HRS.Domain.Entities;

namespace HRS.API.Services.Interfaces;

public interface IUserVerificationService
{
    Task<UserVerification> CreateAsync(int userId, string type, TimeSpan ttl);
    Task<UserVerification?> ValidateAndConsumeAsync(string token, string type);
    Task RevokeExistingAsync(int userId, string type);
}
