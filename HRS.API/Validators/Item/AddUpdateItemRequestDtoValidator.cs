using FluentValidation;
using HRS.API.Contracts.DTOs.Item;

namespace HRS.API.Validators.Item;

public class AddItemRequestDtoValidator : AbstractValidator<AddItemRequestDto>
{
    public AddItemRequestDtoValidator()
    {
        ItemRequestValidatorHelper.AddCommonRules(this);
        ParentItemRequestValidatorHelper.AddCommonRules(this);

        RuleForEach(x => x.Children)
            .SetValidator(new ItemChildRequestDtoValidator());
    }
}

public class UpdateItemRequestDtoValidator : AbstractValidator<UpdateItemRequestDto>
{
    public UpdateItemRequestDtoValidator()
    {
        ItemRequestValidatorHelper.AddCommonRules(this);
        ParentItemRequestValidatorHelper.AddCommonRules(this);

        RuleFor(x => x.Id)
            .NotNull().WithMessage("Item Id is required")
            .GreaterThan(0).WithMessage("Item Id must be greater than 0");

        RuleForEach(x => x.Children)
            .SetValidator(new ItemChildRequestDtoValidator());
    }
}

public static class ItemRequestValidatorHelper
{
    public static void AddCommonRules<T>(AbstractValidator<T> validator) where T : ItemRequestDto
    {
        validator.RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Item name is required");

        validator.RuleFor(x => x.Quantity)
            .NotNull().WithMessage("Item Quantity is required")
            .GreaterThanOrEqualTo(0).WithMessage("Item Quantity cannot be negative");

        validator.RuleFor(x => x.Price)
            .NotNull().WithMessage("Item Price is required")
            .GreaterThanOrEqualTo(0).WithMessage("Item Price cannot be negative");
    }
}

public static class ParentItemRequestValidatorHelper
{
    public static void AddCommonRules<T>(AbstractValidator<T> validator) where T : ParentItemRequestDto
    {
        validator.RuleForEach(x => x.Rates)
            .SetValidator(new ItemRateRequestDtoValidator());
    }
}

public class ItemChildRequestDtoValidator : AbstractValidator<ItemRequestDto>
{
    public ItemChildRequestDtoValidator()
    {
        ItemRequestValidatorHelper.AddCommonRules(this);
    }
}

public class ItemRateRequestDtoValidator : AbstractValidator<ItemRateRequestDto>
{
    public ItemRateRequestDtoValidator()
    {
        RuleFor(x => x.MinDays)
            .NotEmpty().WithMessage("MinDays is required")
            .GreaterThan(0).WithMessage("MinDays must be greater than 0");
        RuleFor(x => x.DailyRate)
            .NotEmpty().WithMessage("DailyRate is required")
            .GreaterThan(0).WithMessage("DailyRate must be non-negative");
    }
}
