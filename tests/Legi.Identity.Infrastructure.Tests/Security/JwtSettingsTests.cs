using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Legi.Identity.Infrastructure.Security;
using Microsoft.IdentityModel.Tokens;

namespace Legi.Identity.Infrastructure.Tests.Security;

public class JwtSettingsTests
{
    [Fact]
    public void ValidateAccessTokenLifetime_ShouldAllowDefaultLifetime()
    {
        var settings = new JwtSettings();

        var exception = Record.Exception(settings.ValidateAccessTokenLifetime);

        Assert.Null(exception);
    }

    [Fact]
    public void ValidateAccessTokenLifetime_ShouldRejectMultiDayLifetime()
    {
        var settings = new JwtSettings
        {
            AccessTokenExpirationMinutes = 50_000
        };

        var exception = Assert.Throws<InvalidOperationException>(
            settings.ValidateAccessTokenLifetime);

        Assert.Equal(JwtSettings.AccessTokenExpirationValidationMessage, exception.Message);
    }

    [Fact]
    public void Validate_ShouldAllowStrongSettings()
    {
        var settings = CreateValidSettings();

        var exception = Record.Exception(settings.Validate);

        Assert.Null(exception);
    }

    [Fact]
    public void Validate_ShouldRejectMissingPrivateKey()
    {
        var settings = CreateValidSettings();
        settings.PrivateKey = string.Empty;

        var exception = Assert.Throws<InvalidOperationException>(settings.Validate);

        Assert.Equal(JwtSettings.SigningValidationMessage, exception.Message);
    }

    [Fact]
    public void Validate_ShouldRejectPublicKeyUsedAsPrivateKey()
    {
        // A public-only PEM must not satisfy the private-key requirement (it cannot sign).
        var settings = CreateValidSettings();
        settings.PrivateKey = settings.PublicKey;

        var exception = Assert.Throws<InvalidOperationException>(settings.Validate);

        Assert.Equal(JwtSettings.SigningValidationMessage, exception.Message);
    }

    [Fact]
    public void Validate_ShouldRejectMissingPublicKey()
    {
        var settings = CreateValidSettings();
        settings.PublicKey = string.Empty;

        var exception = Assert.Throws<InvalidOperationException>(settings.Validate);

        Assert.Equal(JwtSettings.SigningValidationMessage, exception.Message);
    }

    [Fact]
    public void Validate_ShouldRejectMissingIssuer()
    {
        var settings = CreateValidSettings();
        settings.Issuer = string.Empty;

        var exception = Assert.Throws<InvalidOperationException>(settings.Validate);

        Assert.Equal(JwtSettings.SigningValidationMessage, exception.Message);
    }

    [Fact]
    public void Validate_ShouldRejectInvalidRefreshTokenLifetime()
    {
        var settings = CreateValidSettings();
        settings.RefreshTokenExpirationDays = 365;

        var exception = Assert.Throws<InvalidOperationException>(settings.Validate);

        Assert.Equal(JwtSettings.SigningValidationMessage, exception.Message);
    }

    [Fact]
    public void HasValidSettings_ShouldAcceptPublicKeyOnly()
    {
        // Token-validating services (Catalog/Library/Social) only need the public key.
        var settings = CreateValidSettings();
        settings.PrivateKey = string.Empty;

        Assert.True(JwtSettings.HasValidSettings(settings));
        Assert.False(JwtSettings.HasValidSigningSettings(settings));
    }

    [Fact]
    public void CreatePublicSigningKey_ShouldAcceptBase64EncodedPem()
    {
        // Production supplies keys as base64-encoded PEM (single-line, env-var friendly).
        var settings = CreateValidSettings();
        settings.PublicKey = Convert.ToBase64String(Encoding.UTF8.GetBytes(settings.PublicKey));

        var exception = Record.Exception(() => settings.CreatePublicSigningKey());

        Assert.Null(exception);
    }

    [Fact]
    public void SigningCredentials_AndPublicKey_ShouldRoundTripRs256()
    {
        // End-to-end: a token signed with the private key validates with the public key,
        // and the algorithm is RS256 — the whole point of the asymmetric model.
        var settings = CreateValidSettings();

        var token = new JwtSecurityToken(
            issuer: settings.Issuer,
            audience: settings.Audience,
            claims: [new Claim("sub", "user-123")],
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: settings.CreateSigningCredentials());
        var jwt = new JwtSecurityTokenHandler().WriteToken(token);

        // Keep the raw "sub" claim name (the handler otherwise remaps it to NameIdentifier).
        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        var principal = handler.ValidateToken(jwt, new TokenValidationParameters
        {
            ValidIssuer = settings.Issuer,
            ValidAudience = settings.Audience,
            IssuerSigningKey = settings.CreatePublicSigningKey(),
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
            ClockSkew = TimeSpan.Zero
        }, out var validated);

        Assert.Equal("user-123", principal.FindFirst("sub")?.Value);
        Assert.Equal(SecurityAlgorithms.RsaSha256, ((JwtSecurityToken)validated).Header.Alg);
    }

    private static JwtSettings CreateValidSettings()
    {
        // Fresh keypair per test run — nothing to embed or keep in sync.
        using var rsa = RSA.Create(2048);
        return new JwtSettings
        {
            PublicKey = rsa.ExportSubjectPublicKeyInfoPem(),
            PrivateKey = rsa.ExportPkcs8PrivateKeyPem(),
            Issuer = "Legi.Identity.Tests",
            Audience = "Legi.Tests",
            AccessTokenExpirationMinutes = 60,
            RefreshTokenExpirationDays = 7
        };
    }
}
