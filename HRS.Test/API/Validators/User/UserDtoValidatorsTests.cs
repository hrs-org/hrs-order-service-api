using FluentAssertions;
using FluentValidation.TestHelper;
using HRS.API.Contracts.DTOs.User;
using HRS.API.Validators.User;
using HRS.Domain.Interfaces;
using NSubstitute;
using Xunit;

namespace HRS.Test.API.Validators;

public class UserDtoValidatorsTests
{
    private readonly IUserRepository _userRepository;
    private readonly UserDtoValidators _validator;

    public UserDtoValidatorsTests()
    {
        _userRepository = Substitute.For<IUserRepository>();
        _validator = new UserDtoValidators(_userRepository);
    }

    [Fact]
    public async Task Should_HaveError_When_FirstName_IsEmpty()
    {
        var dto = new UserDto
        {
            FirstName = "",
            LastName = "Smith",
            Email = "test@mail.com",
            Role = "Admin"
        };

        var result = await _validator.TestValidateAsync(dto);

        result.ShouldHaveValidationErrorFor(x => x.FirstName);
    }

    [Fact]
    public async Task Should_HaveError_When_LastName_IsEmpty()
    {
        var dto = new UserDto
        {
            FirstName = "John",
            LastName = "",
            Email = "test@mail.com",
            Role = "Admin"
        };

        var result = await _validator.TestValidateAsync(dto);

        result.ShouldHaveValidationErrorFor(x => x.LastName);
    }

    [Fact]
    public async Task Should_HaveError_When_Email_IsEmptyOrInvalid()
    {
        var dto1 = new UserDto { FirstName = "John", LastName = "Smith", Email = "", Role = "Admin" };
        var dto2 = new UserDto { FirstName = "John", LastName = "Smith", Email = "invalid-email", Role = "Admin" };

        var result1 = await _validator.TestValidateAsync(dto1);
        var result2 = await _validator.TestValidateAsync(dto2);

        result1.ShouldHaveValidationErrorFor(x => x.Email);
        result2.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public async Task Should_HaveError_When_Email_IsNotUnique()
    {
        var dto = new UserDto
        {
            FirstName = "John",
            LastName = "Smith",
            Email = "duplicate@mail.com",
            Role = "Admin"
        };
        _userRepository.IsEmailUniqueAsync("duplicate@mail.com").Returns(false);

        var result = await _validator.TestValidateAsync(dto);

        result.ShouldHaveValidationErrorFor(x => x.Email)
              .WithErrorMessage("Email is already in use.");
    }

    [Fact]
    public async Task Should_Pass_When_Email_IsUnique()
    {
        var dto = new UserDto
        {
            FirstName = "John",
            LastName = "Smith",
            Email = "unique@mail.com",
            Role = "Admin"
        };
        _userRepository.IsEmailUniqueAsync("unique@mail.com").Returns(true);

        var result = await _validator.TestValidateAsync(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData("Employee")]
    [InlineData("Admin")]
    [InlineData("Manager")]
    public async Task Should_Pass_When_Role_IsValid(string role)
    {
        var dto = new UserDto
        {
            FirstName = "John",
            LastName = "Smith",
            Email = "test@mail.com",
            Role = role
        };

        var result = await _validator.TestValidateAsync(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Role);
    }

    [Fact]
    public async Task Should_HaveError_When_Role_IsEmpty()
    {
        var dto = new UserDto
        {
            FirstName = "John",
            LastName = "Smith",
            Email = "test@mail.com",
            Role = ""
        };

        var result = await _validator.TestValidateAsync(dto);

        result.ShouldHaveValidationErrorFor(x => x.Role)
              .WithErrorMessage("Role must be Assigned");
    }
}

