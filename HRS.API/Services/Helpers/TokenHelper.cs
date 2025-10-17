using System.Security.Cryptography;
using System.Text;

namespace HRS.API.Services.Helpers;

public static class TokenHelper
{
    // 256-bit random token, Base64Url
    public static string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Base64UrlEncode(bytes);
    }

    public static (string Hash, string Salt) HashToken(string token)
    {
        var salt = Base64UrlEncode(RandomNumberGenerator.GetBytes(16));
        var combined = Encoding.UTF8.GetBytes(token + "." + salt);
        var hash = Base64UrlEncode(SHA256.HashData(combined));
        return (hash, salt);
    }

    public static bool Verify(string token, string hash, string salt)
    {
        var combined = Encoding.UTF8.GetBytes(token + "." + salt);
        var computed = Base64UrlEncode(SHA256.HashData(combined));
        return CryptographicOperations.FixedTimeEquals(
            Convert.FromBase64String(Pad(computed)),
            Convert.FromBase64String(Pad(hash)));
    }

    private static string Base64UrlEncode(byte[] input) =>
        Convert.ToBase64String(input).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static string Pad(string s) => s.PadRight(s.Length + (4 - s.Length % 4) % 4, '=').Replace('-', '+').Replace('_', '/');
}
