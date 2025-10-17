using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HRS.Domain.Enums;

namespace HRS.Domain.Entities;

[Table("ItemMaintenances")]
public class ItemMaintenance
{
    [Key] public int Id { get; set; }

    [Required] public int ItemId { get; set; }
    [ForeignKey(nameof(ItemId))] public Item Item { get; set; } = null!;

    public int? RentalOrderId { get; set; }
    [ForeignKey(nameof(RentalOrderId))] public RentalOrder? RentalOrder { get; set; }

    [Required] public ItemMaintenanceType Type { get; set; } = ItemMaintenanceType.Repair;

    [Required] public int Quantity { get; set; }
    public int? QuantityFixed { get; set; }

    [MaxLength(250)] public string? Remarks { get; set; }

    public int CreatedById { get; set; }

    [ForeignKey(nameof(CreatedById))] public User? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int? UpdatedById { get; set; }

    [ForeignKey(nameof(UpdatedById))] public User? UpdatedBy { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
