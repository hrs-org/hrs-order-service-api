using HRS.API.Common;
using HRS.API.Contracts.DTOs;
using HRS.API.Contracts.DTOs.Auth;
using HRS.API.Contracts.DTOs.User;
using HRS.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRS.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IUserContextService _userContextService;

    public AuthController(IAuthService authService, IUserContextService userContextService)
    {
        _authService = authService;
        _userContextService = userContextService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> LoginAsync([FromBody] LoginRequestDto requestDto)
    {
        var res = await _authService.LoginAsync(requestDto);
        Response.Cookies.Append(
            "refresh_token",
            res.RefreshToken,
            CookiesDefault.RefreshCookieOptions);

        return Ok(ApiResponse<LoginResponseDto>.OkResponse(new LoginResponseDto { UserId = res.UserId, Token = res.Token }, "Login successful"));
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshTokenAsync()
    {
        var refreshToken = Request.Cookies["refresh_token"];
        if (string.IsNullOrEmpty(refreshToken))
            return Unauthorized(ApiResponse<object>.FailResponse("Missing refresh token"));

        var res = await _authService.RefreshTokenAsync(refreshToken);

        Response.Cookies.Append(
            "refresh_token",
            res.RefreshToken,
            CookiesDefault.RefreshCookieOptions);
        return Ok(ApiResponse<LoginResponseDto>.OkResponse(res, "Token refreshed successfully"));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> LogoutAsync()
    {
        var res = await _authService.LogoutAsync();
        Response.Cookies.Delete("refresh_token", new CookieOptions
        {
            Path = "/api/auth"
        });
        return Ok(ApiResponse<LogoutResponseDto>.OkResponse(res, "logout successful"));
    }

    [HttpGet("active-user")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUserAsync()
    {
        var res = await _userContextService.GetUserDtoAsync();
        return Ok(ApiResponse<UserDto>.OkResponse(res, "Get current user successful"));
    }

    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmailAsync([FromBody] EmailVerificationRequestDto requestDto)
    {
        var res = await _authService.VerifyEmailAsync(requestDto);
        return Ok(ApiResponse<EmailVerificationResponseDto>.OkResponse(res, "Email verification completed"));
    }

    [HttpPost("resend-verification")]
    public async Task<IActionResult> ResendVerificationAsync([FromBody] ResendVerificationRequestDto requestDto)
    {
        var res = await _authService.ResendVerificationEmailAsync(requestDto);
        return Ok(ApiResponse<bool>.OkResponse(res, "If your email exists, a verification link has been sent."));
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePasswordAsync(
        [FromBody] ChangePasswordRequestDto requestDto)
    {
        var res = await _authService.ChangePasswordAsync(requestDto);
        return Ok(ApiResponse<ChangePasswordResponseDto>.OkResponse(res, "Password changed successfully"));
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPasswordAsync([FromBody] ForgotPasswordRequestDto requestDto)
    {
        await _authService.ForgotPasswordAsync(requestDto);
        return Ok(ApiResponse<string>.OkResponse(null, "If the email is registered, a password reset link will be sent."));
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPasswordAsync([FromBody] ResetPasswordRequestDto requestDto)
    {
        await _authService.ResetPasswordAsync(requestDto);
        return Ok(ApiResponse<string>.OkResponse(null, "Password has been reset successfully"));
    }
}
