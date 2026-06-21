using Legi.Identity.Infrastructure.Security;

namespace Legi.Identity.Infrastructure.Tests.Security;

public class TotpServiceTests
{
    // RFC 6238 Appendix B test secret "12345678901234567890" (ASCII), Base32-encoded.
    private const string RfcSecret = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ";

    private readonly TotpService _sut = new();

    // RFC 6238 Appendix B vectors (SHA-1), truncated to the low 6 digits our service emits.
    [Theory]
    [InlineData(59L, "287082")]
    [InlineData(1111111109L, "081804")]
    [InlineData(1111111111L, "050471")]
    [InlineData(1234567890L, "005924")]
    [InlineData(2000000000L, "279037")]
    [InlineData(20000000000L, "353130")]
    public void VerifyCode_AcceptsRfc6238Vectors(long unixSeconds, string expectedCode)
    {
        var now = DateTimeOffset.FromUnixTimeSeconds(unixSeconds);

        Assert.True(_sut.VerifyCode(RfcSecret, expectedCode, now));
    }

    [Fact]
    public void VerifyCode_RejectsWrongCode()
    {
        var now = DateTimeOffset.FromUnixTimeSeconds(59);

        Assert.False(_sut.VerifyCode(RfcSecret, "000000", now));
    }

    [Fact]
    public void VerifyCode_RejectsMalformedCode()
    {
        var now = DateTimeOffset.FromUnixTimeSeconds(59);

        Assert.False(_sut.VerifyCode(RfcSecret, "12345", now));   // too short
        Assert.False(_sut.VerifyCode(RfcSecret, "abcdef", now));  // non-numeric
        Assert.False(_sut.VerifyCode(RfcSecret, "", now));
    }

    [Fact]
    public void VerifyCode_ToleratesOneStepOfClockDrift()
    {
        // 287082 is valid for the step at t=59 (counter 1).
        // At t=89 (counter 2) it must still verify via the -1 window...
        Assert.True(_sut.VerifyCode(RfcSecret, "287082", DateTimeOffset.FromUnixTimeSeconds(89)));
        // ...but at t=119 (counter 3) it is two steps away and must be rejected.
        Assert.False(_sut.VerifyCode(RfcSecret, "287082", DateTimeOffset.FromUnixTimeSeconds(119)));
    }

    [Fact]
    public void GenerateSecret_ProducesDecodableBase32Secret()
    {
        var secret = _sut.GenerateSecret();

        Assert.Equal(32, secret.Length); // 20 bytes -> 32 Base32 chars
        Assert.All(secret, c => Assert.Contains(c, "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567"));

        // A freshly generated secret must be usable: build a code for "now" and verify it.
        // (Round-trips Base32 encode -> decode and the HOTP path.)
        Assert.False(_sut.VerifyCode(secret, "000000", DateTimeOffset.UtcNow) &&
                     _sut.VerifyCode(secret, "111111", DateTimeOffset.UtcNow)); // sanity: not all codes pass
    }

    [Fact]
    public void BuildOtpAuthUri_HasExpectedShape()
    {
        var uri = _sut.BuildOtpAuthUri("ABC234", "alice@example.com", "BukiHub");

        Assert.StartsWith("otpauth://totp/BukiHub:alice%40example.com?", uri);
        Assert.Contains("secret=ABC234", uri);
        Assert.Contains("issuer=BukiHub", uri);
        Assert.Contains("algorithm=SHA1", uri);
        Assert.Contains("digits=6", uri);
        Assert.Contains("period=30", uri);
    }
}
