using System.Security.Cryptography;
using System.Text;
using Legi.Identity.Application.Common.Interfaces;

namespace Legi.Identity.Infrastructure.Security;

public class SecureTokenFactory : ISecureTokenFactory
{
    private const int TokenByteLength = 32;

    public (string Token, string Hash) Create()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(TokenByteLength);
        var token = Base64UrlEncode(randomBytes);
        return (token, Hash(token));
    }

    public string Hash(string token)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(hashBytes);
    }

    private static string Base64UrlEncode(byte[] bytes)
        => Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
}
