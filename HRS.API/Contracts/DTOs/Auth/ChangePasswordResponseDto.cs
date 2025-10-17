namespace HRS.API.Contracts.DTOs.Auth;

public class ChangePasswordResponseDto
{
    public int UserId { get; set; }
    public DateTime PasswordChangedAtUtc { get; set; }
}
