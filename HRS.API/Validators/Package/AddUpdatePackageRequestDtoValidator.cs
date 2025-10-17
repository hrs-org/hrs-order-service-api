using FluentValidation;
using HRS.API.Contracts.DTOs.Package;

namespace HRS.API.Validators.Package;

public class AddPackageRequestDtoValidator : PackageRequestDtoBaseValidator<AddPackageRequestDto>
{
}

public class UpdatePackageRequestDtoValidator : PackageRequestDtoBaseValidator<UpdatePackageRequestDto>
{
    public UpdatePackageRequestDtoValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Id must be greater than 0");
    }
}

public abstract class PackageRequestDtoBaseValidator<T> : AbstractValidator<T> where T : PackageRequestDto
{
    protected PackageRequestDtoBaseValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Name must be at most 100 characters");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must be at most 500 characters");

        RuleFor(x => x.BasePrice)
            .GreaterThanOrEqualTo(0).WithMessage("Base price must be non-negative");

        RuleForEach(x => x.Items)
            .SetValidator(new PackageItemRequestDtoValidator());

        RuleForEach(x => x.Rates)
            .SetValidator(new PackageRateRequestDtoValidator());
    }
}

public class PackageItemRequestDtoValidator : AbstractValidator<PackageItemRequestDto>
{
    public PackageItemRequestDtoValidator()
    {
        RuleFor(x => x.ItemId)
            .GreaterThan(0).WithMessage("ItemId must be greater than 0");
        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than 0");
    }
}

public class PackageRateRequestDtoValidator : AbstractValidator<PackageRateRequestDto>
{
    public PackageRateRequestDtoValidator()
    {
        RuleFor(x => x.MinDays)
            .NotEmpty().WithMessage("MinDays is required")
            .GreaterThan(0).WithMessage("MinDays must be greater than 0");
        RuleFor(x => x.DailyRate)
            .NotEmpty().WithMessage("DailyRate is required")
            .GreaterThan(0).WithMessage("DailyRate must be positive");
    }
}
