using FluentValidation.TestHelper;
using HRS.API.Contracts.DTOs.Payment;
using HRS.API.Validators.Payment;
using Xunit;

namespace HRS.Test.API.Validators.Payment;

public class PaymentRequestDtoValidatorTests
{
    private readonly PaymentRequestDtoValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Model_Is_Not_Valid()
    {
        var model = new PaymentRequestDto { OrderId = 0, Amount = 0 };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.OrderId);
        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Model_Is_Valid()
    {
        var model = new PaymentRequestDto { OrderId = 1, Amount = 100 };
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }
}

public class VerifyPaymentRequestDtoValidatorTests
{
    private readonly VerifyPaymentRequestDtoValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_SecretKey_Is_Empty()
    {
        var model = new VerifyPaymentRequestDto { SecretKey = "" };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.SecretKey);
    }

    [Fact]
    public void Should_Not_Have_Error_When_SecretKey_Is_Provided()
    {
        var model = new VerifyPaymentRequestDto { SecretKey = "abc123" };
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
