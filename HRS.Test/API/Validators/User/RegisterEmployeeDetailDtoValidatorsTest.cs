using FluentAssertions;
using FluentValidation.TestHelper;
using HRS.API.Contracts.DTOs.User;
using HRS.API.Validators.User;
using HRS.Domain.Interfaces;
using NSubstitute;

namespace HRS.Test.API.Validators;

public class RegisterEmployeeDetailDtoValidatorsTests
{
    private readonly IUserRepository _userRepository;
    private readonly RegisterEmployeeDetailDtoValidators _validator;

    public RegisterEmployeeDetailDtoValidatorsTests()
    {
        _userRepository = Substitute.For<IUserRepository>();
        _validator = new RegisterEmployeeDetailDtoValidators(_userRepository);
    }

    [Fact]
    public async Task Should_HaveError_When_FirstName_IsEmpty()
    {
        var dto = new RegisterEmployeeDetailDto
        {
            FirstName = "",
            LastName = "KRIT)_((*))",
            Email = "LISDAS@wonder.com",
            Role = "Employee"
        };

        var result = await _validator.TestValidateAsync(dto);

        result.ShouldHaveValidationErrorFor(x => x.FirstName);
    }

    [Fact]
    public async Task Should_HaveError_When_LastName_IsEmpty()
    {
        var dto = new RegisterEmployeeDetailDto
        {
            FirstName = "KRIT",
            LastName = "",
            Email = "LISDAS@wonder.com",
            Role = "Employee"
        };

        var result = await _validator.TestValidateAsync(dto);

        result.ShouldHaveValidationErrorFor(x => x.LastName);
    }

    [Fact]
    public async Task Should_HaveError_When_Email_IsInvalid()
    {
        var dto = new RegisterEmployeeDetailDto
        {
            FirstName = "KRIT",
            LastName = "",
            Email = "invalid-email",
            Role = "Employee"
        };

        var result = await _validator.TestValidateAsync(dto);

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public async Task Should_HaveError_When_Email_IsNotUnique()
    {
        var dto = new RegisterEmployeeDetailDto
        {
            FirstName = "KRIT",
            LastName = "",
            Email = "used@mail.com",
            Role = "Employee"
        };
        _userRepository.IsEmailUniqueAsync("used@mail.com").Returns(false);

        var result = await _validator.TestValidateAsync(dto);

        result.ShouldHaveValidationErrorFor(x => x.Email)
              .WithErrorMessage("Email is already in use.");
    }

    [Fact]
    public async Task Should_Pass_When_Email_IsUnique()
    {
        var dto = new RegisterEmployeeDetailDto
        {
            FirstName = "KRIT",
            LastName = "",
            Email = "unique@mail.com",
            Role = "Employee"
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
        var dto = new RegisterEmployeeDetailDto
        {
            FirstName = "KRIT",
            LastName = "FERI",
            Email = "asd2222@mail.com",
            Role = role
        };

        var result = await _validator.TestValidateAsync(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Role);
    }

    [Fact]
    public async Task Should_HaveError_When_Role_IsInvalid()
    {
        var dto = new RegisterEmployeeDetailDto
        {
            FirstName = "KRIT",
            LastName = "FERI",
            Email = "asd2222@mail.com",
            Role = "InvalidRole"
        };

        var result = await _validator.TestValidateAsync(dto);

        result.ShouldHaveValidationErrorFor(x => x.Role)
              .WithErrorMessage("Role must be Employee or Admin or Manager");
    }
}
