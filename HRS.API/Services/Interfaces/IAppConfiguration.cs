namespace HRS.API.Services.Interfaces;

public interface IAppConfiguration
{
    public string JwtKey { get; set; }
    public string JwtIssuer { get; set; }
    public string JwtAudience { get; set; }
}
