namespace HRS.API.Contracts.DTOs.Auth;

public class EmailVerificationRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string VerificationToken { get; set; } = string.Empty;
}
