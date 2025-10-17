using FluentValidation;
using HRS.API.Contracts.DTOs.Payment;

namespace HRS.API.Validators.Payment;

public class PaymentRequestDtoValidator : AbstractValidator<PaymentRequestDto>
{
    public PaymentRequestDtoValidator()
    {
        RuleFor(x => x.OrderId)
            .GreaterThan(0).WithMessage("OrderId is required and must be greater than zero.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than zero.");
    }
}

public class VerifyPaymentRequestDtoValidator : AbstractValidator<VerifyPaymentRequestDto>
{
    public VerifyPaymentRequestDtoValidator()
    {
        RuleFor(x => x.SecretKey)
            .NotEmpty().WithMessage("SecretKey is required.");
    }
}
