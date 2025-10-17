using FluentValidation;
using HRS.API.Contracts.DTOs.RentalOrder;

namespace HRS.API.Validators.Rental;

public class ReturnRentalOrderRequestValidator : AbstractValidator<ReturnRentalOrderRequestDto>
{
    public ReturnRentalOrderRequestValidator()
    {
        RuleFor(x => x)
            .Must(HaveItemsOrPackages)
            .WithMessage("Return request must include at least one item or package.");

        RuleForEach(x => x.Items)
            .ChildRules(items =>
            {
                items.RuleFor(i => i.RentalOrderItemId)
                    .GreaterThan(0).WithMessage("RentalOrderItemId is required.");

                items.RuleFor(i => i.GoodQty)
                    .GreaterThanOrEqualTo(0);

                items.RuleFor(i => i.RepairQty)
                    .GreaterThanOrEqualTo(0);

                items.RuleFor(i => i.DamagedQty)
                    .GreaterThanOrEqualTo(0);

                items.RuleFor(i => i.LostQty)
                    .GreaterThanOrEqualTo(0);

                items.RuleFor(i => i)
                    .Must(HaveAtLeastOneQty)
                    .WithMessage("At least one quantity (good, repair, damaged, or lost) must be specified.");
            });

        RuleForEach(x => x.Packages)
            .ChildRules(packages =>
            {
                packages.RuleFor(p => p.RentalOrderPackageId)
                    .GreaterThan(0).WithMessage("RentalOrderPackageId is required.");

                packages.RuleFor(p => p.PackageItems)
                    .NotEmpty().WithMessage("Each returned package must include at least one package item.");

                packages.RuleForEach(p => p.PackageItems)
                    .ChildRules(pi =>
                    {
                        pi.RuleFor(x => x.RentalOrderPackageItemId)
                            .GreaterThan(0).WithMessage("RentalOrderPackageItemId is required.");

                        pi.RuleFor(x => x.GoodQty)
                            .GreaterThanOrEqualTo(0);

                        pi.RuleFor(x => x.RepairQty)
                            .GreaterThanOrEqualTo(0);

                        pi.RuleFor(x => x.DamagedQty)
                            .GreaterThanOrEqualTo(0);

                        pi.RuleFor(x => x.LostQty)
                            .GreaterThanOrEqualTo(0);

                        pi.RuleFor(x => x)
                            .Must(HaveAtLeastOneQty)
                            .WithMessage("At least one quantity (good, repair, damaged, or lost) must be specified.");
                    });
            });

        RuleFor(x => x.Remarks)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.Remarks));
    }

    private bool HaveItemsOrPackages(ReturnRentalOrderRequestDto dto)
    {
        var hasItems = dto.Items != null && dto.Items.Count != 0;
        var hasPackages = dto.Packages != null && dto.Packages.Count != 0;
        return hasItems || hasPackages;
    }

    private bool HaveAtLeastOneQty(object obj)
    {
        switch (obj)
        {
            case ReturnItemConditionDto item:
                return item.GoodQty + item.RepairQty + item.DamagedQty + item.LostQty > 0;
            case ReturnPackageItemConditionDto pkgItem:
                return pkgItem.GoodQty + pkgItem.RepairQty + pkgItem.DamagedQty + pkgItem.LostQty > 0;
            default:
                return false;
        }
    }
}
