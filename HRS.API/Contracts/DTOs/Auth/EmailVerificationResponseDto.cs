namespace HRS.API.Contracts.DTOs.Auth;

public class EmailVerificationResponseDto
{
    public bool IsVerified { get; set; }
    public string Message { get; set; } = string.Empty;
}
