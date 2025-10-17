using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRS.Domain.Entities;

[Table("PackageRates")]
public class PackageRate
{
    [Key] public int Id { get; set; }

    [Required] public int PackageId { get; set; }

    [ForeignKey(nameof(PackageId))] public Package? Package { get; set; }

    [Required][Range(1, int.MaxValue)] public int MinDays { get; set; }

    [Required]
    [Range(0.0, double.MaxValue)]
    public decimal DailyRate { get; set; }

    [Required] public bool IsActive { get; set; } = true;

    public int CreatedById { get; set; }

    [ForeignKey(nameof(CreatedById))] public User? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int? UpdatedById { get; set; }

    [ForeignKey(nameof(UpdatedById))] public User? UpdatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
