using FluentValidation.TestHelper;
using HRS.API.Contracts.DTOs.RentalOrder;
using HRS.API.Validators.Rental;

namespace HRS.Test.API.Validators.Rental;

public class ReturnRentalOrderRequestValidatorTests
{
    private readonly ReturnRentalOrderRequestValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Model_Is_Not_Valid()
    {
        var model = new ReturnRentalOrderRequestDto
        {
            Items = new List<ReturnItemConditionDto>(),
            Packages = new List<ReturnPackageConditionDto>(),
            Remarks = new string('a', 501)
        };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Remarks);
    }

    [Fact]
    public void Should_Have_Error_When_Item_Or_Package_Fields_Invalid()
    {
        var item = new ReturnItemConditionDto
        {
            RentalOrderItemId = 0,
            GoodQty = 0,
            RepairQty = 0,
            DamagedQty = 0,
            LostQty = 0
        };
        var pkgItem = new ReturnPackageItemConditionDto
        {
            RentalOrderPackageItemId = 0,
            GoodQty = 0,
            RepairQty = 0,
            DamagedQty = 0,
            LostQty = 0
        };
        var pkg = new ReturnPackageConditionDto
        {
            RentalOrderPackageId = 0,
            PackageItems = new List<ReturnPackageItemConditionDto> { pkgItem }
        };
        var model = new ReturnRentalOrderRequestDto
        {
            Items = new List<ReturnItemConditionDto> { item },
            Packages = new List<ReturnPackageConditionDto> { pkg }
        };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor("Items[0].RentalOrderItemId");
        result.ShouldHaveValidationErrorFor("Items[0]");
        result.ShouldHaveValidationErrorFor("Packages[0].RentalOrderPackageId");
        result.ShouldHaveValidationErrorFor("Packages[0].PackageItems[0].RentalOrderPackageItemId");
        result.ShouldHaveValidationErrorFor("Packages[0].PackageItems[0]");
    }

    [Fact]
    public void Should_Not_Have_Error_When_Model_Is_Valid()
    {
        var item = new ReturnItemConditionDto
        {
            RentalOrderItemId = 1,
            GoodQty = 1,
            RepairQty = 0,
            DamagedQty = 0,
            LostQty = 0
        };
        var pkgItem = new ReturnPackageItemConditionDto
        {
            RentalOrderPackageItemId = 1,
            GoodQty = 0,
            RepairQty = 1,
            DamagedQty = 0,
            LostQty = 0
        };
        var pkg = new ReturnPackageConditionDto
        {
            RentalOrderPackageId = 1,
            PackageItems = new List<ReturnPackageItemConditionDto> { pkgItem }
        };
        var model = new ReturnRentalOrderRequestDto
        {
            Items = new List<ReturnItemConditionDto> { item },
            Packages = new List<ReturnPackageConditionDto> { pkg },
            Remarks = "Valid remarks"
        };
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Not_Have_Error_When_Remarks_Is_Null_Or_Empty()
    {
        var item = new ReturnItemConditionDto
        {
            RentalOrderItemId = 1,
            GoodQty = 1,
            RepairQty = 0,
            DamagedQty = 0,
            LostQty = 0
        };
        var model1 = new ReturnRentalOrderRequestDto
        {
            Items = new List<ReturnItemConditionDto> { item },
            Remarks = null
        };
        var model2 = new ReturnRentalOrderRequestDto
        {
            Items = new List<ReturnItemConditionDto> { item },
            Remarks = string.Empty
        };
        var result1 = _validator.TestValidate(model1);
        var result2 = _validator.TestValidate(model2);
        result1.ShouldNotHaveValidationErrorFor(x => x.Remarks);
        result2.ShouldNotHaveValidationErrorFor(x => x.Remarks);
    }
}
