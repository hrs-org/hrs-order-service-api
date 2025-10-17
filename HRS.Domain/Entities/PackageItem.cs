using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRS.Domain.Entities;

[Table("PackageItems")]
public class PackageItem
{
    [Key] public int Id { get; set; }

    [Required] public int PackageId { get; set; }

    [ForeignKey(nameof(PackageId))] public Package? Package { get; set; }

    [Required] public int ItemId { get; set; }

    [ForeignKey(nameof(ItemId))] public Item? Item { get; set; }

    [Required] public int Quantity { get; set; } = 1;
}
