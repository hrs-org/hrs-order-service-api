using FluentAssertions;
using HRS.API.Services;
using HRS.API.Services.Interfaces;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace HRS.Test.API.Services;

public class EmailSenderServiceTests
{
    private readonly IAppConfiguration _appConfiguration;
    private readonly EmailSenderService _emailSenderService;
    private readonly ILogger<EmailSenderService> _logger;

    public EmailSenderServiceTests()
    {
        _appConfiguration = Substitute.For<IAppConfiguration>();
        _logger = Substitute.For<ILogger<EmailSenderService>>();

        _emailSenderService = new EmailSenderService(_logger, _appConfiguration);
    }

    [Fact]
    public async Task SendEmailAsync_WithInvalidSmtpHost_ReturnsFalse()
    {
        var recipient = "test@example.com";
        var subject = "Test Subject";
        var body = "Test Body";

        var result = await _email_sender_service_Send(recipient, subject, body);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task SendEmailAsync_WithHtmlBody_ReturnsFalse()
    {
        var recipient = "test@example.com";
        var subject = "Test Subject";
        var body = "<html><body>Test HTML Body</body></html>";

        var result = await _email_sender_service_Send(recipient, subject, body);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task SendEmailAsync_WithPlainTextBody_ReturnsFalse()
    {
        var recipient = "test@example.com";
        var subject = "Test Subject";
        var body = "Plain text body";

        var result = await _email_sender_service_Send(recipient, subject, body, false);

        result.Should().BeFalse();
    }

    // helper to call the actual method (keeps tests concise)
    private Task<bool> _email_sender_service_Send(string to, string subject, string body, bool isHtml = true)
        => _emailSenderService.SendEmailAsync(to, subject, body, isHtml);
}
