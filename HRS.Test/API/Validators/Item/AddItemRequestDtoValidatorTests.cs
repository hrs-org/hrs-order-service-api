using FluentValidation.TestHelper;
using HRS.API.Contracts.DTOs.Item;
using HRS.API.Validators.Item;
using Xunit;

namespace HRS.Test.API.Validators.Item;

public class AddItemRequestDtoValidatorTests
{
    private readonly AddItemRequestDtoValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Model_Is_Not_Valid()
    {
        var model = new AddItemRequestDto { Name = "", Description = "", Quantity = -1, Price = -1 };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Name);
        result.ShouldHaveValidationErrorFor(x => x.Quantity);
        result.ShouldHaveValidationErrorFor(x => x.Price);
    }

    [Fact]
    public void Should_Have_Error_When_Child_Has_Invalid_Fields()
    {
        var child = new AddItemRequestDto { Name = "", Description = "", Quantity = -1, Price = -1 };
        var model = new AddItemRequestDto
        {
            Name = "Parent",
            Description = "Parent Desc",
            Quantity = 1,
            Price = 1,
            Children = new List<AddItemRequestDto> { child }
        };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor("Children[0].Name");
        result.ShouldHaveValidationErrorFor("Children[0].Quantity");
        result.ShouldHaveValidationErrorFor("Children[0].Price");
    }

    [Fact]
    public void Should_Not_Have_Error_When_Model_Is_Valid()
    {
        var child = new AddItemRequestDto { Name = "Child", Description = "Child Desc", Quantity = 1, Price = 1 };
        var model = new AddItemRequestDto
        {
            Name = "Parent",
            Description = "Parent Desc",
            Quantity = 1,
            Price = 1,
            Children = new List<AddItemRequestDto> { child }
        };
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
