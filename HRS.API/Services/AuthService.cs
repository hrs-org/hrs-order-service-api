using HRS.API.Contracts.DTOs.Auth;
using HRS.API.Services.Interfaces;
using HRS.Domain.Interfaces;

namespace HRS.API.Services;

public class AuthService : IAuthService
{
    private readonly IEmailBuilderService _emailBuilderService;
    private readonly IEmailSenderService _emailSenderService;
    private readonly IUserContextService _userContextService;
    private readonly IUserRepository _userRepository;
    private readonly IUserSessionService _userSessionService;
    private readonly IUserVerificationService _userVerificationService;

    public AuthService(
        IUserRepository userRepository,
        IUserContextService userContextService,
        IUserSessionService userSessionService,
        IUserVerificationService userVerificationService,
        IEmailBuilderService emailBuilderService,
        IEmailSenderService emailSenderService)
    {
        _userRepository = userRepository;
        _userContextService = userContextService;
        _userSessionService = userSessionService;
        _userVerificationService = userVerificationService;
        _emailBuilderService = emailBuilderService;
        _emailSenderService = emailSenderService;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto requestDto)
    {
        var user = await _userRepository.GetByEmailAsync(requestDto.Email)
                   ?? throw new UnauthorizedAccessException("Invalid email or password");

        if (!BCrypt.Net.BCrypt.Verify(requestDto.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password");

        if (!user.IsVerified)
            throw new UnauthorizedAccessException("Please verify your email before logging in");

        var (accessToken, refreshToken) = await _userSessionService.CreateAsync(user.Id);

        return new LoginResponseDto
        {
            UserId = user.Id,
            Token = accessToken,
            RefreshToken = refreshToken
        };
    }

    public async Task<LoginResponseDto> RefreshTokenAsync(string refreshToken)
    {
        var (accessToken, newRefreshToken) = await _userSessionService.RefreshAsync(refreshToken);
        var user = await _userContextService.GetUserAsync();

        return new LoginResponseDto
        {
            UserId = user.Id,
            Token = accessToken,
            RefreshToken = newRefreshToken
        };
    }

    public async Task<LogoutResponseDto> LogoutAsync()
    {
        var user = await _userContextService.GetUserAsync();
        await _userSessionService.RevokeAllForUserAsync(user.Id, "Manual logout");

        return new LogoutResponseDto { Message = "Logout successful" };
    }

    public async Task<EmailVerificationResponseDto> VerifyEmailAsync(EmailVerificationRequestDto requestDto)
    {
        var verification = await _userVerificationService.ValidateAndConsumeAsync(requestDto.VerificationToken, "Email");
        if (verification == null)
            return new EmailVerificationResponseDto
            {
                IsVerified = false,
                Message = "Invalid or expired verification token"
            };

        var user = await _userRepository.GetByIdAsync(verification.UserId);
        if (user == null)
            return new EmailVerificationResponseDto
            {
                IsVerified = false,
                Message = "User not found"
            };

        user.IsVerified = true;
        await _userRepository.UpdateUserAsync(user);

        return new EmailVerificationResponseDto
        {
            IsVerified = true,
            Message = "Email verified successfully"
        };
    }

    public async Task<bool> ResendVerificationEmailAsync(ResendVerificationRequestDto requestDto)
    {
        var user = await _userRepository.GetByEmailAsync(requestDto.Email)
                   ?? throw new InvalidOperationException("User not found");

        if (user.IsVerified)
            throw new InvalidOperationException("Email is already verified");

        var verification = await _userVerificationService.CreateAsync(user.Id, "Email", TimeSpan.FromHours(24));

        var subject = "Verify Your Email - Hiking Rental Store";
        var emailTemplate = _emailBuilderService.BuildVerificationEmailTemplate(
            user.Email, verification.Token, user.FirstName);
        var body = _emailBuilderService.GenerateEmailBody(emailTemplate);

        await _emailSenderService.SendEmailAsync(user.Email, subject, body);
        return true;
    }

    public async Task<ChangePasswordResponseDto> ChangePasswordAsync(ChangePasswordRequestDto requestDto)
    {
        var user = await _userContextService.GetUserAsync();

        if (string.IsNullOrEmpty(requestDto.CurrentPassword) ||
            !BCrypt.Net.BCrypt.Verify(requestDto.CurrentPassword, user.PasswordHash))
            throw new UnauthorizedAccessException("Current password is incorrect.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(requestDto.NewPassword);
        await _userRepository.UpdateUserAsync(user);

        return new ChangePasswordResponseDto
        {
            UserId = user.Id,
            PasswordChangedAtUtc = DateTime.UtcNow
        };
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequestDto requestDto)
    {
        var user = await _userRepository.GetByEmailAsync(requestDto.Email);
        if (user == null)
            throw new InvalidOperationException("If the email is registered, a password reset link will be sent.");
        var verification = await _userVerificationService.CreateAsync(user.Id, "PasswordReset", TimeSpan.FromHours(1));

        var subject = "Reset Your Password - Hiking Rental Store";
        var emailTemplate = _emailBuilderService.BuildPasswordResetEmailTemplate(user.Email, verification.Token, user.FirstName);
        var body = _emailBuilderService.GenerateEmailBody(emailTemplate);
        await _emailSenderService.SendEmailAsync(user.Email, subject, body);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequestDto requestDto)
    {
        var user = await _userRepository.GetByEmailAsync(requestDto.Email);
        if (user == null)
            throw new InvalidOperationException("Invalid email or token.");

        if (requestDto.NewPassword != requestDto.ConfirmNewPassword)
            throw new InvalidOperationException("New password and confirmation do not match.");

        if (BCrypt.Net.BCrypt.Verify(requestDto.NewPassword, user.PasswordHash))
            throw new InvalidOperationException("New password cannot be the same as the current password.");

        var verification = await _userVerificationService.ValidateAndConsumeAsync(requestDto.Token, "PasswordReset");
        if (verification == null || verification.UserId != user.Id)
            throw new InvalidOperationException("Invalid or expired password reset token.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(requestDto.NewPassword);
        await _userRepository.UpdateUserAsync(user);
    }
}
