using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Legi.Identity.Application.Common.Interfaces;
using Legi.Identity.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Legi.Identity.Infrastructure.Security;

public class JwtTokenService(IOptions<JwtSettings> jwtSettings) : IJwtTokenService
{
    private const int MfaChallengeExpirationMinutes = 5;

    private readonly JwtSettings _jwtSettings = jwtSettings.Value;

    // Built once, lazily: importing the RSA key is comparatively expensive, and callers
    // that only hash/expire refresh tokens must not require the private key to be present.
    private readonly Lazy<SigningCredentials> _signingCredentials =
        new(jwtSettings.Value.CreateSigningCredentials);

    private readonly Lazy<RsaSecurityKey> _publicKey =
        new(jwtSettings.Value.CreatePublicSigningKey);

    // Distinct from the access-token audience so a challenge token is rejected by the
    // resource APIs (which require the access-token audience).
    private string MfaChallengeAudience => $"{_jwtSettings.Audience}:mfa";

    public (string Token, DateTime ExpiresAt) GenerateAccessToken(User user)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email.Value),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: _signingCredentials.Value
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return (tokenString, expiresAt);
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public string HashRefreshToken(string refreshToken)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToBase64String(hashBytes);
    }

    public DateTime GetRefreshTokenExpiresAt()
    {
        return DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);
    }

    public string GenerateMfaChallengeToken(User user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: MfaChallengeAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(MfaChallengeExpirationMinutes),
            signingCredentials: _signingCredentials.Value);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public Guid? ValidateMfaChallengeToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        try
        {
            var principal = new JwtSecurityTokenHandler().ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = MfaChallengeAudience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = _publicKey.Value,
                ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
                ClockSkew = TimeSpan.Zero
            }, out _);

            var sub = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return Guid.TryParse(sub, out var userId) ? userId : null;
        }
        catch
        {
            // Any validation failure (bad signature/audience/expiry) → not a valid challenge.
            return null;
        }
    }
}
