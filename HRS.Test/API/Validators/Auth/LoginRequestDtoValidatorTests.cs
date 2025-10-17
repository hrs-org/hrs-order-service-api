using FluentValidation.TestHelper;
using HRS.API.Contracts.DTOs.Auth;
using HRS.API.Validators.Auth;

namespace HRS.Test.API.Validators.Auth;

public class LoginRequestDtoValidatorTests
{
    private readonly LoginRequestDtoValidator _validatorTests;

    public LoginRequestDtoValidatorTests()
    {
        _validatorTests = new LoginRequestDtoValidator();
    }

    [Fact]
    public void Should_Pass_Validation_When_AllFieldsValid()
    {
        // Arrange
        var dto = new LoginRequestDto
        {
            Email = "test@email.com",
            Password = "NewPassword123!"
        };

        // Act
        var result = _validatorTests.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_When_FieldsMissing_Or_TooShort()
    {
        // Arrange
        var dto = new LoginRequestDto
        {
            Email = "",
            Password = "Short"
        };

        // Act
        var result = _validatorTests.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Email is required");

        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password must be at least 8 characters long");
    }
}
