using HRS.API.Services;
using Microsoft.Extensions.Configuration;
using NSubstitute;

namespace HRS.Test.API.Services;

public class AppConfigurationTests
{
    [Fact]
    public void Setup_ShouldReadConfigurationValues()
    {
        // Arrange
        var config = Substitute.For<IConfiguration>();
        config["Email:SmtpHost"].Returns("smtp.test.com");
        config["Email:SmtpPort"].Returns("587");
        config["Email:SmtpUsername"].Returns("user");
        config["Email:SmtpPassword"].Returns("pass");
        config["Email:FromEmail"].Returns("from@test.com");
        config["FrontendUrl"].Returns("http://frontend");
        config["Jwt:Key"].Returns("jwtkeyjwtkeyjwtkeyjwtkeyjwtkeyjwtkeyjwtkeyjwtkey");
        config["Jwt:Issuer"].Returns("issuer");
        config["Jwt:Audience"].Returns("audience");

        // Act
        var appConfig = new AppConfiguration(config);

        // Assert
        Assert.Equal("smtp.test.com", appConfig.SmtpHost);
        Assert.Equal(587, appConfig.SmtpPort);
        Assert.Equal("user", appConfig.SmtpUsername);
        Assert.Equal("pass", appConfig.SmtpPassword);
        Assert.Equal("from@test.com", appConfig.FromEmail);
        Assert.Equal("http://frontend", appConfig.FrontendUrl);
        Assert.Equal("jwtkeyjwtkeyjwtkeyjwtkeyjwtkeyjwtkeyjwtkeyjwtkey", appConfig.JwtKey);
        Assert.Equal("issuer", appConfig.JwtIssuer);
        Assert.Equal("audience", appConfig.JwtAudience);
    }
}
