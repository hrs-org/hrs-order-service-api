using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRS.Domain.Entities;

[Table("Items")]
public class Item
{
    [Key] public int Id { get; set; }

    [Required][MaxLength(150)] public string Name { get; set; } = null!;

    [MaxLength(500)] public string Description { get; set; } = string.Empty;

    [Required] public int Quantity { get; set; }

    [Column(TypeName = "decimal(10,2)")] public decimal Price { get; set; }

    public int? ParentId { get; set; }

    [ForeignKey(nameof(ParentId))] public Item? Parent { get; set; }

    public ICollection<Item> Children { get; set; } = [];

    public ICollection<ItemRate> Rates { get; set; } = [];

    public int CreatedById { get; set; }

    [ForeignKey(nameof(CreatedById))] public User? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int? UpdatedById { get; set; }

    [ForeignKey(nameof(UpdatedById))] public User? UpdatedBy { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
