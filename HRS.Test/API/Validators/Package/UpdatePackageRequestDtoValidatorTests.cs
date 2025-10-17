using FluentValidation.TestHelper;
using HRS.API.Contracts.DTOs.Package;
using HRS.API.Validators.Package;
using Xunit;

namespace HRS.Test.API.Validators.Package;

public class UpdatePackageRequestDtoValidatorTests
{
    private readonly UpdatePackageRequestDtoValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Id_Is_Zero_Or_Negative()
    {
        var model = new UpdatePackageRequestDto { Id = 0, Name = "Test", BasePrice = 10 };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Id);

        model.Id = -5;
        result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Id_Is_Positive()
    {
        var model = new UpdatePackageRequestDto { Id = 1, Name = "Test", BasePrice = 10 };
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.Id);
    }
}
