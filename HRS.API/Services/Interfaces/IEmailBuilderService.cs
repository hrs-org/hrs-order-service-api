using HRS.API.Models;

namespace HRS.API.Services.Interfaces;

public interface IEmailBuilderService
{
    EmailTemplate BuildVerificationEmailTemplate(string email, string verificationToken, string firstName);
    EmailTemplate BuildPasswordResetEmailTemplate(string email, string resetToken, string firstName);
    EmailTemplate BuildEmployeeWelcomeEmailTemplate(string email, string password, string firstName);
    string GenerateEmailBody(EmailTemplate emailTemplate);
}
