namespace HRS.API.Contracts.DTOs.Auth;

public class LoginResponseDto
{
    public string Token { get; set; } = "";
    public string RefreshToken { get; set; } = "";
    public int UserId { get; set; }
}
