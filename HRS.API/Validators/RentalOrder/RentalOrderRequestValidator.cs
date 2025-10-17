using FluentValidation;
using HRS.API.Contracts.DTOs.RentalOrder;

namespace HRS.API.Validators.Rental;

public class RentalOrderRequestValidator : AbstractValidator<CreateRentalOrderRequestDto>
{
    public RentalOrderRequestValidator()
    {
        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Start date is required.");

        RuleFor(x => x.EndDate)
            .NotEmpty().WithMessage("End date is required.")
            .GreaterThan(x => x.StartDate)
            .WithMessage("End date must be after start date.");

        RuleFor(x => x)
            .Must(HaveCustomerOrGuest)
            .WithMessage("Either CustomerId or Guest information must be provided.");

        RuleFor(x => x.GuestName)
            .MaximumLength(150);

        RuleFor(x => x.GuestEmail)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.GuestEmail))
            .WithMessage("Invalid guest email format.");

        RuleFor(x => x.GuestPhone)
            .MaximumLength(50);

        RuleFor(x => x)
            .Must(HaveItemsOrPackages)
            .WithMessage("Order must contain at least one item or package.");

        RuleForEach(x => x.Items)
            .ChildRules(items =>
            {
                items.RuleFor(i => i.ItemId)
                    .NotNull().WithMessage("ItemId is required for each item.");
                items.RuleFor(i => i.Quantity)
                    .GreaterThan(0).WithMessage("Quantity must be greater than zero.");
            });

        RuleForEach(x => x.Packages)
            .ChildRules(packages =>
            {
                packages.RuleFor(p => p.PackageId)
                    .NotNull().WithMessage("PackageId is required for each package.");
                packages.RuleFor(p => p.Quantity)
                    .GreaterThan(0).WithMessage("Quantity must be greater than zero.");
            });
    }

    private bool HaveCustomerOrGuest(CreateRentalOrderRequestDto dto)
    {
        return dto.CustomerId.HasValue ||
               (!string.IsNullOrWhiteSpace(dto.GuestName) &&
                !string.IsNullOrWhiteSpace(dto.GuestPhone));
    }

    private bool HaveItemsOrPackages(CreateRentalOrderRequestDto dto)
    {
        var hasItems = dto.Items != null && dto.Items.Count != 0;
        var hasPackages = dto.Packages != null && dto.Packages.Count != 0;
        return hasItems || hasPackages;
    }
}
