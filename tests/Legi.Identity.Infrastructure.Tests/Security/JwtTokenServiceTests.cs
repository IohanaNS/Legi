using System.Security.Cryptography;
using System.Text;
using Legi.Identity.Infrastructure.Security;
using Microsoft.Extensions.Options;

namespace Legi.Identity.Infrastructure.Tests.Security;

public class JwtTokenServiceTests
{
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
