using FluentValidation.TestHelper;
using HRS.API.Contracts.DTOs.Maintenance;
using HRS.API.Validators.Maintenance;
using Xunit;

namespace HRS.Test.API.Validators.Maintenance;

public class ItemMaintenanceRequestDtoValidatorTests
{
    private readonly ItemMaintenanceRequestDtoValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Model_Is_Not_Valid()
    {
        var model = new ItemMaintenanceRequestDto { Id = 0, QuantityFixed = 0, Remarks = new string('a', 501) };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Id);
        result.ShouldHaveValidationErrorFor(x => x.QuantityFixed);
        result.ShouldHaveValidationErrorFor(x => x.Remarks);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Model_Is_Valid()
    {
        var model = new ItemMaintenanceRequestDto { Id = 1, QuantityFixed = 2, Remarks = "All good" };
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Not_Have_Error_When_Remarks_Is_Null_Or_Empty()
    {
        var model1 = new ItemMaintenanceRequestDto { Id = 1, QuantityFixed = 2, Remarks = null };
        var model2 = new ItemMaintenanceRequestDto { Id = 1, QuantityFixed = 2, Remarks = string.Empty };
        var result1 = _validator.TestValidate(model1);
        var result2 = _validator.TestValidate(model2);
        result1.ShouldNotHaveValidationErrorFor(x => x.Remarks);
        result2.ShouldNotHaveValidationErrorFor(x => x.Remarks);
    }
}
