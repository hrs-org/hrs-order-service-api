using HRS.API.Services.Interfaces;

namespace HRS.API.Services;

public class AppConfiguration : IAppConfiguration
{
    private readonly IConfiguration _configuration;

    public AppConfiguration(IConfiguration configuration)
    {
        _configuration = configuration;
        Setup();
    }

    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; }
    public string SmtpUsername { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public string FrontendUrl { get; set; } = string.Empty;
    public string JwtKey { get; set; } = string.Empty;
    public string JwtIssuer { get; set; } = string.Empty;
    public string JwtAudience { get; set; } = string.Empty;
    public string StripeApiKey { get; set; } = string.Empty;
    public string PaymentReturnPath { get; set; } = string.Empty;

    private void Setup()
    {
        SmtpHost = _configuration["Email:SmtpHost"] ?? "";
        SmtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "0");
        SmtpUsername = _configuration["Email:SmtpUsername"] ?? "";
        SmtpPassword = _configuration["Email:SmtpPassword"] ?? "";
        FromEmail = _configuration["Email:FromEmail"] ?? "";
        FrontendUrl = _configuration["FrontendUrl"] ?? "";
        JwtKey = _configuration["Jwt:Key"] ?? "";
        JwtIssuer = _configuration["Jwt:Issuer"] ?? "";
        JwtAudience = _configuration["Jwt:Audience"] ?? "";
        StripeApiKey = _configuration["Payment:Stripe:ApiKey"] ?? "";
        PaymentReturnPath = FrontendUrl + (_configuration["Payment:ReturnPath"] ?? "");
    }
}
