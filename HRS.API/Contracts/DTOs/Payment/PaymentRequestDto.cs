using System.ComponentModel.DataAnnotations;

namespace HRS.API.Contracts.DTOs.Payment;

public class PaymentRequestDto
{
    [Required] public int OrderId { get; set; }
    [Required] public double Amount { get; set; }
}

public class VerifyPaymentRequestDto
{
    [Required] public string SecretKey { get; set; } = string.Empty;
}
