using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HRS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HRS.Domain.Entities;

[Table("Users")]
[Index(nameof(Email), IsUnique = true)]
public class User
{
    [Key] public int Id { get; set; }

    [Required][MaxLength(100)] public string FirstName { get; set; } = null!;

    [Required][MaxLength(100)] public string LastName { get; set; } = null!;

    [Required][MaxLength(150)] public string Email { get; set; } = null!;

    [Required] public string PasswordHash { get; set; } = null!;

    public bool IsVerified { get; set; }
    public UserRole Role { get; set; } = UserRole.Customer;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }

    [ForeignKey(nameof(CreatedBy))] public virtual User? CreatedByUser { get; set; }

    [ForeignKey(nameof(UpdatedBy))] public virtual User? UpdatedByUser { get; set; }

    // Navigation Properties
    public virtual ICollection<UserSession> Sessions { get; set; } = new List<UserSession>();
    public virtual ICollection<UserVerification> Verifications { get; set; } = new List<UserVerification>();
}
