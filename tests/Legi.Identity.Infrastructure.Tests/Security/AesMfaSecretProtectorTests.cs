using System.Security.Cryptography;
using Legi.Identity.Application.Common.Models;
using Legi.Identity.Infrastructure.Security;

namespace Legi.Identity.Infrastructure.Tests.Security;

public class AesMfaSecretProtectorTests
{
    private static AesMfaSecretProtector CreateProtector()
        => new(new MfaSettings { EncryptionKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) });

    [Fact]
    public void ProtectThenUnprotect_RoundTrips()
    {
        var sut = CreateProtector();
        const string secret = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ";

        var roundTripped = sut.Unprotect(sut.Protect(secret));

        Assert.Equal(secret, roundTripped);
    }

    [Fact]
    public void Protect_ProducesDifferentCiphertextEachTime()
    {
        var sut = CreateProtector();

        // Random nonce per call -> ciphertext must differ even for identical input.
        Assert.NotEqual(sut.Protect("same-secret"), sut.Protect("same-secret"));
    }

    [Fact]
    public void Unprotect_ThrowsOnTamperedCiphertext()
    {
        var sut = CreateProtector();
        var protectedValue = sut.Protect("GEZDGNBVGY3TQOJQ");

        var bytes = Convert.FromBase64String(protectedValue);
        bytes[^1] ^= 0xFF; // flip a ciphertext byte
        var tampered = Convert.ToBase64String(bytes);

        // AES-GCM throws AuthenticationTagMismatchException (a CryptographicException subtype).
        Assert.ThrowsAny<CryptographicException>(() => sut.Unprotect(tampered));
    }

    [Fact]
    public void Unprotect_FailsWithDifferentKey()
    {
        var protectedValue = CreateProtector().Protect("GEZDGNBVGY3TQOJQ");

        // A protector with a different key must not be able to decrypt it.
        Assert.ThrowsAny<CryptographicException>(() => CreateProtector().Unprotect(protectedValue));
    }

    [Theory]
    [InlineData("")]
    [InlineData("dG9vLXNob3J0")] // base64 of "too-short" -> not 32 bytes
    public void Constructor_RejectsInvalidKey(string badKey)
    {
        Assert.Throws<InvalidOperationException>(() =>
            new AesMfaSecretProtector(new MfaSettings { EncryptionKey = badKey }));
    }
}
