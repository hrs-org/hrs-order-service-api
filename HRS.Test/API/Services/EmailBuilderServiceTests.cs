using FluentAssertions;
using HRS.API.Models;
using HRS.API.Services;
using HRS.API.Services.Interfaces;
using NSubstitute;

namespace HRS.Test.API.Services;

public class EmailBuilderServiceTests
{
    private readonly IAppConfiguration _appConfig;
    private readonly EmailBuilderService _emailBuilderService;

    public EmailBuilderServiceTests()
    {
        _appConfig = Substitute.For<IAppConfiguration>();
        _emailBuilderService = new EmailBuilderService(_appConfig);
    }

    [Fact]
    public void BuildVerificationEmailTemplate_WithValidInputs_ReturnsCorrectTemplate()
    {
        // Arrange
        var email = "test@example.com";
        var verificationToken = "token123";
        var firstName = "John";

        _appConfig.FrontendUrl.Returns("http://localhost:4200");

        // Act
        var result = _email_builder_service_Build(email, verificationToken, firstName);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Welcome to Hiking Rental Store, John!");
        result.ButtonUrl.Should().NotBeNull();
        result.ButtonUrl!.ToString().Should().Contain("http://localhost:4200/verify-email");
    }

    [Fact]
    public void BuildPasswordResetEmailTemplate_WithValidInputs_ReturnsCorrectTemplate()
    {
        // Arrange
        var email = "test@example.com";
        var resetToken = "reset-token-123";
        var firstName = "John";

        _appConfig.FrontendUrl.Returns("http://localhost:4200");

        // Act
        var result = _emailBuilderService.BuildPasswordResetEmailTemplate(email, resetToken, firstName);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Password Reset Request for Hiking Rental Store, John");
        result.Content.Should().Contain("reset your password");
        result.ButtonText.Should().Be("Reset Password");
        result.ButtonUrl.Should().NotBeNull();
        result.ButtonUrl!.ToString().Should().Contain("http://localhost:4200/reset-password");
        result.ButtonUrl.ToString().Should().Contain($"email={Uri.EscapeDataString(email)}");
        result.ButtonUrl.ToString().Should().Contain($"token={Uri.EscapeDataString(resetToken)}");
        result.AdditionalInfo.Should().Contain("1 hour");
        result.FooterText.Should().Contain("didn't request a password reset");
    }

    [Fact]
    public void BuildEmployeeWelcomeEmailTemplate_WithValidInputs_ReturnsCorrectTemplate()
    {
        // Arrange
        var email = "employee@example.com";
        var password = "abc123de";
        var firstName = "Jane";

        _appConfig.FrontendUrl.Returns("http://localhost:4200");

        // Act
        var result = _emailBuilderService.BuildEmployeeWelcomeEmailTemplate(email, password, firstName);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Welcome to Hiking Rental Store, Jane!");
        result.Content.Should().Contain(email);
        result.Content.Should().Contain(password);
        result.ButtonText.Should().Be("Login Now");
        result.ButtonUrl.Should().NotBeNull();
        result.ButtonUrl!.ToString().Should().Be("http://localhost:4200/login");
        result.AdditionalInfo.Should().Contain("security reasons");
        result.FooterText.Should().Contain("administrator");
    }

    [Fact]
    public void BuildPasswordResetEmailTemplate_WithTrailingSlashInUrl_TrimsSlash()
    {
        // Arrange
        var email = "user@example.com";
        var resetToken = "reset-token";
        var firstName = "Jane";

        _appConfig.FrontendUrl.Returns("http://localhost:4200/");

        // Act
        var result = _emailBuilderService.BuildPasswordResetEmailTemplate(email, resetToken, firstName);

        // Assert
        result.ButtonUrl.Should().NotBeNull();
        result.ButtonUrl!.ToString().Should().NotContain("//reset-password");
        result.ButtonUrl.ToString().Should().Contain("http://localhost:4200/reset-password");
    }

    [Fact]
    public void BuildPasswordResetEmailTemplate_WithSpecialCharactersInEmail_EncodesCorrectly()
    {
        // Arrange
        var email = "user+test@example.com";
        var resetToken = "token@#$%";
        var firstName = "Bob";

        _appConfig.FrontendUrl.Returns("http://localhost:4200");

        // Act
        var result = _emailBuilderService.BuildPasswordResetEmailTemplate(email, resetToken, firstName);

        // Assert
        result.ButtonUrl.Should().NotBeNull();
        result.ButtonUrl!.ToString().Should().Contain(Uri.EscapeDataString(email));
        result.ButtonUrl.ToString().Should().Contain(Uri.EscapeDataString(resetToken));

    }

    [Fact]
    public void GenerateEmailBody_WithButtonText_IncludesButton()
    {
        // Arrange
        var template = new EmailTemplate
        {
            Title = "Test Title",
            Content = "Test content",
            ButtonText = "Click Here",
            ButtonUrl = new Uri("https://example.com/verify"),
            AdditionalInfo = "Additional info",
            FooterText = "Footer text"
        };

        // Act
        var result = _emailBuilderService.GenerateEmailBody(template);

        // Assert
        result.Should().Contain("Click Here");
        result.Should().Contain("https://example.com/verify");
    }

    [Fact]
    public void GenerateEmailBody_WithEmptyButtonText_DoesNotIncludeButton()
    {
        // Arrange
        var template = new EmailTemplate
        {
            Title = "Test Title",
            Content = "Test content",
            ButtonText = "",
            ButtonUrl = new Uri("https://example.com/verify"),
            AdditionalInfo = "Additional info",
            FooterText = "Footer text"
        };

        // Act
        var result = _emailBuilderService.GenerateEmailBody(template);

        // Assert
        result.Should().NotContain("Click Here");
        result.Should().NotContain("https://example.com/verify");
    }

    [Fact]
    public void GenerateEmailBody_WithEmptyAdditionalInfo_DoesNotIncludeAdditionalInfo()
    {
        // Arrange
        var template = new EmailTemplate
        {
            Title = "Test Title",
            Content = "Test content",
            ButtonText = "Click Here",
            ButtonUrl = new Uri("https://example.com/verify"),
            AdditionalInfo = "",
            FooterText = "Footer text"
        };

        // Act
        var result = _email_builder_service_Generate(template);

        // Assert
        result.Should().NotContain("<strong></strong>");
    }

    // helpers
    private EmailTemplate _email_builder_service_Build(string email, string token, string name)
        => _emailBuilderService.BuildVerificationEmailTemplate(email, token, name);

    private string _email_builder_service_Generate(EmailTemplate t)
        => _emailBuilderService.GenerateEmailBody(t);
}
