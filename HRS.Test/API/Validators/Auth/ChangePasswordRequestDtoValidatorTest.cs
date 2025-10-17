using FluentValidation.TestHelper;
using HRS.API.Contracts.DTOs.Auth;
using HRS.API.Validators.Auth;

namespace HRS.Test.API.Validators.Auth;

public class ChangePasswordRequestDtoValidatorTests
{
    private readonly ChangePasswordRequestDtoValidator _validator;

    public ChangePasswordRequestDtoValidatorTests()
    {
        _validator = new ChangePasswordRequestDtoValidator();
    }

    [Fact]
    public void Should_Pass_Validation_When_AllFieldsValid()
    {
        // Arrange
        var dto = new ChangePasswordRequestDto
        {
            CurrentPassword = "OldPassword123!",
            NewPassword = "NewPassword123!",
            ConfirmNewPassword = "NewPassword123!"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_When_FieldsMissing_Or_TooShort()
    {
        // Arrange
        var dto = new ChangePasswordRequestDto
        {
            CurrentPassword = "",
            NewPassword = "short",
            ConfirmNewPassword = "short"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CurrentPassword)
            .WithErrorMessage("Current password is required");

        result.ShouldHaveValidationErrorFor(x => x.NewPassword)
            .WithErrorMessage("Password must be at least 8 characters long");
    }

    [Fact]
    public void Should_Fail_When_Confirmation_Does_Not_Match()
    {
        // Arrange
        var dto = new ChangePasswordRequestDto
        {
            CurrentPassword = "OldPassword123!",
            NewPassword = "NewPassword123!",
            ConfirmNewPassword = "MismatchPassword"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ConfirmNewPassword)
            .WithErrorMessage("New password and confirmation do not match");
    }

    [Fact]
    public void Should_Fail_When_NewPassword_Same_As_Current()
    {
        // Arrange
        var dto = new ChangePasswordRequestDto
        {
            CurrentPassword = "SamePassword123!",
            NewPassword = "SamePassword123!",
            ConfirmNewPassword = "SamePassword123!"
        };
        // Act
        var result = _validator.TestValidate(dto);
        // Assert
        result.ShouldHaveValidationErrorFor(x => x.NewPassword)
            .WithErrorMessage("New password must be different from current password");
    }
}
