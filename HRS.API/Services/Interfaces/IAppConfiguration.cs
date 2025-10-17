namespace HRS.API.Services.Interfaces;

public interface IAppConfiguration
{
    string SmtpHost { get; set; }
    int SmtpPort { get; set; }
    string SmtpUsername { get; set; }
    string SmtpPassword { get; set; }
    string FromEmail { get; set; }
    string FromName { get; set; }
    string FrontendUrl { get; set; }
    public string JwtKey { get; set; }
    public string JwtIssuer { get; set; }
    public string JwtAudience { get; set; }
    public string StripeApiKey { get; set; }
    public string PaymentReturnPath { get; set; }
}
