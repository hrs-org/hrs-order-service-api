using FluentValidation.TestHelper;
using HRS.API.Contracts.DTOs.Package;
using HRS.API.Validators.Package;

namespace HRS.Test.API.Validators.Package;

public class AddPackageRequestDtoValidatorTests
{
    private readonly AddPackageRequestDtoValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Model_Is_Not_Valid()
    {
        var model = new AddPackageRequestDto { Name = "", Description = "", BasePrice = -1 };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Name);
        result.ShouldHaveValidationErrorFor(x => x.BasePrice);
    }

    [Fact]
    public void Should_Have_Error_When_Item_Has_Invalid_Fields()
    {
        var item = new PackageItemRequestDto { ItemId = 0, Quantity = 0 };
        var model = new AddPackageRequestDto
        {
            Name = "Test",
            Description = "Desc",
            BasePrice = 10,
            Items = new List<PackageItemRequestDto> { item }
        };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor("Items[0].ItemId");
        result.ShouldHaveValidationErrorFor("Items[0].Quantity");
    }

    [Fact]
    public void Should_Have_Error_When_Rate_Has_Invalid_Fields()
    {
        var rate = new PackageRateRequestDto { MinDays = 0, DailyRate = -1 };
        var model = new AddPackageRequestDto
        {
            Name = "Test",
            Description = "Desc",
            BasePrice = 10,
            Rates = new List<PackageRateRequestDto> { rate }
        };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor("Rates[0].MinDays");
        result.ShouldHaveValidationErrorFor("Rates[0].DailyRate");
    }

    [Fact]
    public void Should_Not_Have_Error_When_Model_Is_Valid()
    {
        var item = new PackageItemRequestDto { ItemId = 1, Quantity = 2 };
        var rate = new PackageRateRequestDto { MinDays = 1, DailyRate = 10 };
        var model = new AddPackageRequestDto
        {
            Name = "Test",
            Description = "Desc",
            BasePrice = 10,
            Items = new List<PackageItemRequestDto> { item },
            Rates = new List<PackageRateRequestDto> { rate }
        };
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
