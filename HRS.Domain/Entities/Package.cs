using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRS.Domain.Entities;

[Table("Packages")]
public class Package
{
    [Key] public int Id { get; set; }

    [Required][MaxLength(150)] public string Name { get; set; } = null!;

    [MaxLength(500)] public string? Description { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal BasePrice { get; set; }

    public ICollection<PackageItem> PackageItems { get; set; } = [];
    public ICollection<PackageRate> PackageRates { get; set; } = [];

    public int CreatedById { get; set; }

    [ForeignKey(nameof(CreatedById))] public User? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int? UpdatedById { get; set; }

    [ForeignKey(nameof(UpdatedById))] public User? UpdatedBy { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
