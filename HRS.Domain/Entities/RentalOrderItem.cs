using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRS.Domain.Entities;

[Table("RentalOrderItems")]
public class RentalOrderItem
{
    [Key] public int Id { get; set; }

    [Required] public int RentalOrderId { get; set; }
    [ForeignKey(nameof(RentalOrderId))] public RentalOrder? RentalOrder { get; set; }

    public int? ItemId { get; set; }
    [ForeignKey(nameof(ItemId))] public Item? Item { get; set; }

    public int? ItemRateId { get; set; }
    [ForeignKey(nameof(ItemRateId))] public ItemRate? ItemRate { get; set; }

    [Required][MaxLength(150)] public string ItemNameSnapshot { get; set; } = null!;
    [Column(TypeName = "decimal(10,2)")] public decimal DailyRateSnapshot { get; set; }
    [Required] public int Quantity { get; set; }

    public int GoodQty { get; set; }
    public int RepairQty { get; set; }
    public int DamagedQty { get; set; }
    public int LostQty { get; set; }

    public string? ConditionRemarks { get; set; }

    [NotMapped] public bool HasIssues => RepairQty + DamagedQty + LostQty > 0;

    public void SetReturnConditions(int good, int repair, int damaged, int lost)
    {
        if (good + repair + damaged + lost != Quantity)
            throw new InvalidOperationException("Condition totals must match quantity.");
        GoodQty = good;
        RepairQty = repair;
        DamagedQty = damaged;
        LostQty = lost;
    }
}
