using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HRS.Domain.Enums;

namespace HRS.Domain.Entities;

[Table("Payments")]
public class Payment
{
    [Key] public int Id { get; set; }

    [Required] public int RentalOrderId { get; set; }
    [ForeignKey(nameof(RentalOrderId))] public RentalOrder RentalOrder { get; set; } = null!;

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal Amount { get; set; }

    [Required] public PaymentType PaymentType { get; set; } = PaymentType.Stripe;

    [Required] public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    [MaxLength(200)] public string? StripeSessionId { get; set; }

    [MaxLength(200)] public string? StripePaymentIntentId { get; set; }

    [MaxLength(250)] public string? Notes { get; set; }

    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

    public int? CreatedById { get; set; }
    [ForeignKey(nameof(CreatedById))] public User? CreatedBy { get; set; }

    public int? UpdatedById { get; set; }
    [ForeignKey(nameof(UpdatedById))] public User? UpdatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
