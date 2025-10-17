using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRS.Domain.Entities;

[Table("RentalOrderPackageItems")]
public class RentalOrderPackageItem
{
    [Key] public int Id { get; set; }

    [Required] public int RentalOrderPackageId { get; set; }

    [ForeignKey(nameof(RentalOrderPackageId))]
    public RentalOrderPackage? RentalOrderPackage { get; set; }

    public int? ItemId { get; set; } // nullable FK
    [ForeignKey(nameof(ItemId))] public Item? Item { get; set; }

    [Required][MaxLength(150)] public string ItemNameSnapshot { get; set; } = null!;
    [Required] public int QuantityPerPackageSnapshot { get; set; }

    public int GoodQty { get; set; }
    public int RepairQty { get; set; }
    public int DamagedQty { get; set; }
    public int LostQty { get; set; }

    [NotMapped] public bool HasIssues => RepairQty + DamagedQty + LostQty > 0;
}
