using FluentValidation.TestHelper;
using HRS.API.Contracts.DTOs.RentalOrder;
using HRS.API.Validators.Rental;

namespace HRS.Test.API.Validators.Rental;

public class RentalOrderRequestValidatorTests
{
    private readonly RentalOrderRequestValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Model_Is_Not_Valid()
    {
        var model = new CreateRentalOrderRequestDto
        {
            StartDate = default,
            EndDate = default,
            CustomerId = null,
            GuestName = null,
            GuestPhone = null,
            Items = [],
            Packages = []
        };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.StartDate);
        result.ShouldHaveValidationErrorFor(x => x.EndDate);
    }

    [Fact]
    public void Should_Have_Error_When_EndDate_Before_StartDate()
    {
        var model = new CreateRentalOrderRequestDto
        {
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(-1),
            CustomerId = 1,
            Items = new List<RentalOrderItemRequestDto> { new() { ItemId = 1, Quantity = 1 } }
        };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.EndDate);
    }

    [Fact]
    public void Should_Have_Error_When_Items_Or_Packages_Invalid()
    {
        var model = new CreateRentalOrderRequestDto
        {
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(1),
            CustomerId = 1,
            Items = new List<RentalOrderItemRequestDto> { new() { Quantity = 0 } },
            Packages = new List<RentalOrderPackageRequestDto> { new() { Quantity = 0 } }
        };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor("Items[0].Quantity");
        result.ShouldHaveValidationErrorFor("Packages[0].Quantity");
    }

    [Fact]
    public void Should_Not_Have_Error_When_Model_Is_Valid_With_Customer()
    {
        var model = new CreateRentalOrderRequestDto
        {
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(1),
            CustomerId = 1,
            Items = new List<RentalOrderItemRequestDto> { new() { ItemId = 1, Quantity = 1 } },
            Packages = new List<RentalOrderPackageRequestDto> { new() { PackageId = 1, Quantity = 1 } }
        };
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Not_Have_Error_When_Model_Is_Valid_With_Guest()
    {
        var model = new CreateRentalOrderRequestDto
        {
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(1),
            CustomerId = null,
            GuestName = "Guest Name",
            GuestPhone = "1234567890",
            Items = new List<RentalOrderItemRequestDto> { new() { ItemId = 1, Quantity = 1 } }
        };
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
