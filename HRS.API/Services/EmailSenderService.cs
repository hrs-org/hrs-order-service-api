using System.Net;
using System.Net.Mail;
using HRS.API.Services.Interfaces;

namespace HRS.API.Services;

public class EmailSenderService : IEmailSenderService
{
    private readonly IAppConfiguration _appConfiguration;
    private readonly ILogger<EmailSenderService> _logger;

    public EmailSenderService(ILogger<EmailSenderService> logger, IAppConfiguration appConfiguration)
    {
        _logger = logger;
        _appConfiguration = appConfiguration;
    }

    public async Task<bool> SendEmailAsync(string recipient, string subject, string body, bool isHtml = true)
    {
        try
        {
            using var client = new SmtpClient(_appConfiguration.SmtpHost, _appConfiguration.SmtpPort);
            client.EnableSsl = true;
            client.Credentials = new NetworkCredential(_appConfiguration.SmtpUsername, _appConfiguration.SmtpPassword);

            using var message = new MailMessage();
            message.From = new MailAddress(_appConfiguration.FromEmail, _appConfiguration.FromName);
            message.To.Add(recipient);
            message.Subject = subject;
            message.Body = body;
            message.IsBodyHtml = isHtml;

            await client.SendMailAsync(message);

            _logger.LogInformation("Sending verification email to {Email}", recipient);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to send email to {Email}, with exception {Error}", recipient, ex);
            return false;
        }
    }
}
