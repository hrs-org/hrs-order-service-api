namespace HRS.API.Services.Interfaces;

public interface IEmailSenderService
{
    Task<bool> SendEmailAsync(string recipient, string subject, string body, bool isHtml = true);
}
