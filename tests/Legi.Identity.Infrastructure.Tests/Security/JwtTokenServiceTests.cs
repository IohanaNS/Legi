using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Legi.Identity.Domain.Entities;
using Legi.Identity.Domain.Enums;
using Legi.Identity.Domain.ValueObjects;
using Legi.Identity.Infrastructure.Security;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Legi.Identity.Infrastructure.Tests.Security;

public class JwtTokenServiceTests
{
    private static JwtSettings SettingsWithKeys()
    {
        using var rsa = RSA.Create(2048);
        return new JwtSettings
        {
            PublicKey = rsa.ExportSubjectPublicKeyInfoPem(),
            PrivateKey = rsa.ExportPkcs8PrivateKeyPem(),
            Issuer = "Legi.Identity",
            Audience = "Legi",
            AccessTokenExpirationMinutes = 15,
            RefreshTokenExpirationDays = 7
        };
    }

    private static User TestUser()
        => User.Create(
            Legi.Identity.Domain.ValueObjects.Email.Create("mfa@example.com"),
            Username.Create("mfa_user"),
            "hash");

    private static JwtSecurityToken ReadJwt(string token)
    {
        return new JwtSecurityTokenHandler().ReadJwtToken(token);
    }

    private static bool HasRoleClaim(JwtSecurityToken jwt, UserRole role)
    {
        return jwt.Claims.Any(claim =>
            (claim.Type == ClaimTypes.Role || claim.Type == "role") &&
            claim.Value == role.ToString());
    }

    [Fact]
    public void MfaChallengeToken_RoundTrips_ToUserId()
    {
        var service = new JwtTokenService(Options.Create(SettingsWithKeys()));
        var user = TestUser();

        var token = service.GenerateMfaChallengeToken(user);

        Assert.Equal(user.Id, service.ValidateMfaChallengeToken(token));
    }

    [Fact]
    public void MfaChallengeToken_IsRejected_WhenValidatedAsAnAccessToken()
    {
        // Security guarantee: a challenge token must not be usable against the resource
        // APIs, which validate the access-token audience.
        var settings = SettingsWithKeys();
        var service = new JwtTokenService(Options.Create(settings));
        var token = service.GenerateMfaChallengeToken(TestUser());

        var accessTokenValidation = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = settings.Issuer,
            ValidateAudience = true,
            ValidAudience = settings.Audience, // the access-token audience
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = settings.CreatePublicSigningKey(),
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
            ClockSkew = TimeSpan.Zero
        };

        Assert.ThrowsAny<SecurityTokenException>(() =>
            new JwtSecurityTokenHandler().ValidateToken(token, accessTokenValidation, out _));
    }

    [Fact]
    public void ValidateMfaChallengeToken_ReturnsNull_ForInvalidToken()
    {
        var service = new JwtTokenService(Options.Create(SettingsWithKeys()));

        Assert.Null(service.ValidateMfaChallengeToken("not-a-real-token"));
    }

    [Fact]
    public void GenerateAccessToken_ShouldIncludeUserRoleClaim_ForRegularUser()
    {
        var service = new JwtTokenService(Options.Create(SettingsWithKeys()));

        var (token, _) = service.GenerateAccessToken(TestUser());

        var jwt = ReadJwt(token);
        Assert.True(HasRoleClaim(jwt, UserRole.User));
    }

    [Fact]
    public void GenerateAccessToken_ShouldIncludeAdminRoleClaim_ForAdminUser()
    {
        var user = TestUser();
        user.AssignRole(UserRole.Admin);
        var service = new JwtTokenService(Options.Create(SettingsWithKeys()));

        var (token, _) = service.GenerateAccessToken(user);

        var jwt = ReadJwt(token);
        Assert.True(HasRoleClaim(jwt, UserRole.Admin));
        Assert.False(HasRoleClaim(jwt, UserRole.User));
    }

    [Fact]
    public void HashRefreshToken_ShouldReturnSha256DigestAndNotRawToken()
    {
        // Arrange
        var service = new JwtTokenService(Options.Create(new JwtSettings()));
        const string refreshToken = "raw_refresh_token";
        var expectedHash = Convert.ToBase64String(
            SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));

        // Act
        var result = service.HashRefreshToken(refreshToken);

        // Assert
        Assert.Equal(expectedHash, result);
        Assert.NotEqual(refreshToken, result);
    }

    [Fact]
    public void GetRefreshTokenExpiresAt_ShouldUseConfiguredLifetime()
    {
        // Arrange
        var service = new JwtTokenService(Options.Create(new JwtSettings
        {
            RefreshTokenExpirationDays = 14
        }));
        var before = DateTime.UtcNow.AddDays(14);

        // Act
        var result = service.GetRefreshTokenExpiresAt();

        // Assert
        var after = DateTime.UtcNow.AddDays(14);
        Assert.InRange(result, before, after);
    }
}
