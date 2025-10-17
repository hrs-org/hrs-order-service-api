using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRS.Domain.Entities;

[Table("UserSessions")]
public class UserSession
{
    [Key] public int Id { get; set; }

    [Required] public int UserId { get; set; }

    [ForeignKey(nameof(UserId))] public virtual User User { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required] public string RefreshTokenHash { get; set; } = null!;

    [Required] public string RefreshTokenSalt { get; set; } = null!;

    [Required] public DateTime ExpiresAt { get; set; }

    public bool IsRevoked { get; set; }

    public string? RevokedReason { get; set; }
}
