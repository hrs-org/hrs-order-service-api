using HRS.API.Models;
using HRS.API.Services.Interfaces;

namespace HRS.API.Services;

public class EmailBuilderService : IEmailBuilderService
{
    private readonly IAppConfiguration _appConfiguration;

    public EmailBuilderService(IAppConfiguration appConfiguration)
    {
        _appConfiguration = appConfiguration;
    }

    public EmailTemplate BuildVerificationEmailTemplate(string email, string verificationToken, string firstName)
    {
        var frontendUrl = _appConfiguration.FrontendUrl.TrimEnd('/');
        var verificationUrl = $"{frontendUrl}/verify-email?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(verificationToken)}";

        return new EmailTemplate
        {
            Title = $"Welcome to Hiking Rental Store, {firstName}!",
            Content =
                "Thank you for registering with Hiking Rental Store. To complete your registration, please verify your email address by clicking the button below:",
            ButtonText = "Verify Email Address",
            ButtonUrl = new Uri(verificationUrl),
            AdditionalInfo = "This link will expire in 24 hours.",
            FooterText = "If you didn't create an account with Hiking Rental Store, please ignore this email."
        };
    }

    public EmailTemplate BuildPasswordResetEmailTemplate(string email, string resetToken, string firstName)
    {
        var frontendUrl = _appConfiguration.FrontendUrl.TrimEnd('/');
        var resetUrl = $"{frontendUrl}/reset-password?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(resetToken)}";

        return new EmailTemplate
        {
            Title = $"Password Reset Request for Hiking Rental Store, {firstName}",
            Content =
                "We received a request to reset your password. Click the button below to reset it:",
            ButtonText = "Reset Password",
            ButtonUrl = new Uri(resetUrl),
            AdditionalInfo = "This link will expire in 1 hour.",
            FooterText = "If you didn't request a password reset, please ignore this email."
        };
    }

    public EmailTemplate BuildEmployeeWelcomeEmailTemplate(string email, string password, string firstName)
    {
        var frontendUrl = _appConfiguration.FrontendUrl.TrimEnd('/');
        var loginUrl = $"{frontendUrl}/login";

        return new EmailTemplate
        {
            Title = $"Welcome to Hiking Rental Store, {firstName}!",
            Content = $@"Your employee account has been created successfully.<br/><br/>
                        <strong>Your login credentials:</strong><br/>
                        Email: {email}<br/>
                        Temporary Password: <strong>{password}</strong><br/><br/>
                        Please use the button below to log in and change your password immediately:",
            ButtonText = "Login Now",
            ButtonUrl = new Uri(loginUrl),
            AdditionalInfo = "For security reasons, please change your password after your first login.",
            FooterText = "If you didn't expect this email, please contact your administrator."
        };
    }

    public string GenerateEmailBody(EmailTemplate emailTemplate)
    {
        return $@"
            <html>
            <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <h2 style='color: #2c3e50;'>{emailTemplate.Title}</h2>
                    <p>{emailTemplate.Content}</p>

                    {(string.IsNullOrEmpty(emailTemplate.ButtonText) ? "" : $@"
                    <div style='text-align: center; margin: 30px 0;'>
                        <a href='{emailTemplate.ButtonUrl}'
                           style='background-color: #3498db; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; display: inline-block;'>
                            {emailTemplate.ButtonText}
                        </a>
                    </div>

                    <p>If the button doesn't work, you can copy and paste this link into your browser:</p>
                    <p style='word-break: break-all; color: #7f8c8d;'>{emailTemplate.ButtonUrl}</p>")}

                    {(string.IsNullOrEmpty(emailTemplate.AdditionalInfo) ? "" : $"<p><strong>{emailTemplate.AdditionalInfo}</strong></p>")}

                    <hr style='border: none; border-top: 1px solid #ecf0f1; margin: 30px 0;'>
                    <p style='font-size: 12px; color: #7f8c8d;'>
                        {emailTemplate.FooterText}
                    </p>
                </div>
            </body>
            </html>";
    }
}
