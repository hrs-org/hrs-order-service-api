using HRS.API.Services;
using HRS.Domain.Entities;
using HRS.Domain.Interfaces;
using NSubstitute;

namespace HRS.Test.API.Services;

public class UserVerificationServiceTests
{
    private readonly UserVerificationService _service;
    private readonly IUserVerificationRepository _userVerificationRepository;

    public UserVerificationServiceTests()
    {
        _userVerificationRepository = Substitute.For<IUserVerificationRepository>();
        _service = new UserVerificationService(_userVerificationRepository);
    }

    [Fact]
    public async Task CreateAsync_ShouldAddAndReturnEntity()
    {
        // Arrange
        _userVerificationRepository.AddAsync(Arg.Any<UserVerification>()).Returns(Task.CompletedTask);
        _userVerificationRepository.RevokeExistingAsync(Arg.Any<int>(), Arg.Any<string>()).Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateAsync(1, "Email", TimeSpan.FromHours(1));

        // Assert
        Assert.Equal(1, result.UserId);
        Assert.Equal("Email", result.Type);
        Assert.True(result.Expiry > DateTime.UtcNow);
        Assert.False(string.IsNullOrEmpty(result.Token));
    }

    [Fact]
    public async Task ValidateAndConsumeAsync_ShouldReturnVerification()
    {
        // Arrange
        var verification = new UserVerification { UserId = 1, Token = "token", Type = "Email" };
        _userVerificationRepository.ValidateAndConsumeAsync("token", "Email").Returns(Task.FromResult<UserVerification?>(verification));

        // Act
        var result = await _service.ValidateAndConsumeAsync("token", "Email");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result!.UserId);
    }

    [Fact]
    public async Task RevokeExistingAsync_ShouldCallRepository()
    {
        // Arrange
        _userVerificationRepository.RevokeExistingAsync(1, "Email").Returns(Task.CompletedTask);

        // Act
        await _service.RevokeExistingAsync(1, "Email");

        // Assert
        await _userVerificationRepository.Received(1).RevokeExistingAsync(1, "Email");
    }
}
