using HRS.API.Services.Interfaces;
using HRS.Domain.Entities;
using HRS.Domain.Interfaces;

namespace HRS.API.Services;

public class UserVerificationService : IUserVerificationService
{
    private readonly IUserVerificationRepository _userVerificationRepository;

    public UserVerificationService(IUserVerificationRepository userVerificationRepository)
    {
        _userVerificationRepository = userVerificationRepository;
    }

    public async Task<UserVerification> CreateAsync(int userId, string type, TimeSpan ttl)
    {
        await RevokeExistingAsync(userId, type);

        var token = Guid.NewGuid().ToString("N");
        var entity = new UserVerification
        {
            UserId = userId,
            Token = token,
            Type = type,
            Expiry = DateTime.UtcNow.Add(ttl),
            CreatedAt = DateTime.UtcNow
        };

        await _userVerificationRepository.AddAsync(entity);
        await _userVerificationRepository.SaveChangesAsync();

        return entity;
    }

    public async Task<UserVerification?> ValidateAndConsumeAsync(string token, string type)
    {
        var verification = await _userVerificationRepository.ValidateAndConsumeAsync(token, type);

        return verification;
    }

    public async Task RevokeExistingAsync(int userId, string type) => await _userVerificationRepository.RevokeExistingAsync(userId, type);
}
