using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRS.Domain.Entities;

[Table("UserVerifications")]
[Index(nameof(UserId))]
[Index(nameof(Token), IsUnique = true)]
public class UserVerification
{
    [Key] public int Id { get; set; }

    [Required] public int UserId { get; set; }

    [ForeignKey(nameof(UserId))] public virtual User User { get; set; } = null!;

    [Required][MaxLength(100)] public string Token { get; set; } = null!;

    [Required][MaxLength(50)] public string Type { get; set; } = "Email";

    [Required] public DateTime Expiry { get; set; }

    [Required] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ConsumedAt { get; set; }

    public bool IsUsed { get; set; }
}
