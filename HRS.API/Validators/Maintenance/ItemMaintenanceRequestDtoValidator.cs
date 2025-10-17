using FluentValidation;
using HRS.API.Contracts.DTOs.Maintenance;

namespace HRS.API.Validators.Maintenance;

public class ItemMaintenanceRequestDtoValidator : AbstractValidator<ItemMaintenanceRequestDto>
{
    public ItemMaintenanceRequestDtoValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Id is required and must be greater than zero.");

        RuleFor(x => x.QuantityFixed)
            .GreaterThan(0).WithMessage("QuantityFixed must be greater than zero.");

        RuleFor(x => x.Remarks)
            .MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.Remarks));
    }
}
