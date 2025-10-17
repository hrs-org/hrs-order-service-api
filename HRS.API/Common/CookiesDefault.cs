namespace HRS.API.Common;

public static class CookiesDefault
{
    public static readonly CookieOptions RefreshCookieOptions = new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.None,
        Expires = DateTime.UtcNow.AddDays(30),
        Path = "/api/auth"
    };
}
