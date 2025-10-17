using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRS.Domain.Entities;

[Table("RentalOrderPackages")]
public class RentalOrderPackage
{
    [Key] public int Id { get; set; }

    [Required] public int RentalOrderId { get; set; }
    [ForeignKey(nameof(RentalOrderId))] public RentalOrder? RentalOrder { get; set; }

    public int? PackageId { get; set; }
    [ForeignKey(nameof(PackageId))] public Package? Package { get; set; }

    public int? PackageRateId { get; set; }
    [ForeignKey(nameof(PackageRateId))] public PackageRate? PackageRate { get; set; }

    [Required][MaxLength(150)] public string PackageNameSnapshot { get; set; } = null!;
    [Column(TypeName = "decimal(10,2)")] public decimal DailyRateSnapshot { get; set; }

    [Required] public int Quantity { get; set; }

    public ICollection<RentalOrderPackageItem> Items { get; set; } = [];
}
