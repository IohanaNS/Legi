using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Legi.Identity.Application.Common.Interfaces;

namespace Legi.Identity.Infrastructure.Security;

/// <summary>
/// RFC 6238 TOTP (HMAC-SHA1, 30-second step, 6 digits) — the scheme every standard
/// authenticator app (Google Authenticator, Authy, 1Password, …) implements.
/// </summary>
public sealed class TotpService : ITotpService
{
    private const int StepSeconds = 30;
    private const int Digits = 6;
    private const int SecretBytes = 20; // 160-bit, per RFC 4226 recommendation
    private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    public string GenerateSecret()
    {
        var bytes = RandomNumberGenerator.GetBytes(SecretBytes);
        return Base32Encode(bytes);
    }

    public string BuildOtpAuthUri(string base32Secret, string accountName, string issuer)
    {
        // Label is "Issuer:Account" with each part escaped but the ':' separator literal.
        var encodedIssuer = Uri.EscapeDataString(issuer);
        var label = $"{encodedIssuer}:{Uri.EscapeDataString(accountName)}";
        return $"otpauth://totp/{label}?secret={base32Secret}&issuer={encodedIssuer}" +
               $"&algorithm=SHA1&digits={Digits}&period={StepSeconds}";
    }

    public bool VerifyCode(string base32Secret, string code, DateTimeOffset? now = null)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length != Digits)
            return false;

        byte[] key;
        try
        {
            key = Base32Decode(base32Secret);
        }
        catch (FormatException)
        {
            return false;
        }

        var counter = (now ?? DateTimeOffset.UtcNow).ToUnixTimeSeconds() / StepSeconds;

        // Accept the previous, current and next step to tolerate clock drift.
        for (var offset = -1; offset <= 1; offset++)
        {
            var candidate = ComputeHotp(key, counter + offset);
            if (FixedTimeEquals(candidate, code))
                return true;
        }

        return false;
    }

    private static string ComputeHotp(byte[] key, long counter)
    {
        var counterBytes = new byte[8];
        BinaryPrimitives.WriteInt64BigEndian(counterBytes, counter);

        var hash = HMACSHA1.HashData(key, counterBytes);

        // RFC 4226 dynamic truncation.
        var offset = hash[^1] & 0x0F;
        var binary = ((hash[offset] & 0x7F) << 24)
                     | ((hash[offset + 1] & 0xFF) << 16)
                     | ((hash[offset + 2] & 0xFF) << 8)
                     | (hash[offset + 3] & 0xFF);

        var otp = binary % (int)Math.Pow(10, Digits);
        return otp.ToString().PadLeft(Digits, '0');
    }

    private static bool FixedTimeEquals(string a, string b)
        => CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(a), Encoding.ASCII.GetBytes(b));

    private static string Base32Encode(byte[] data)
    {
        var sb = new StringBuilder((data.Length + 4) / 5 * 8);
        int buffer = 0, bitsLeft = 0;

        foreach (var b in data)
        {
            buffer = (buffer << 8) | b;
            bitsLeft += 8;
            while (bitsLeft >= 5)
            {
                bitsLeft -= 5;
                sb.Append(Base32Alphabet[(buffer >> bitsLeft) & 0x1F]);
            }
        }

        if (bitsLeft > 0)
            sb.Append(Base32Alphabet[(buffer << (5 - bitsLeft)) & 0x1F]);

        return sb.ToString();
    }

    private static byte[] Base32Decode(string input)
    {
        var cleaned = input.TrimEnd('=').Replace(" ", string.Empty).ToUpperInvariant();
        var output = new List<byte>(cleaned.Length * 5 / 8);
        int buffer = 0, bitsLeft = 0;

        foreach (var c in cleaned)
        {
            var index = Base32Alphabet.IndexOf(c);
            if (index < 0)
                throw new FormatException($"Invalid Base32 character '{c}'.");

            buffer = (buffer << 5) | index;
            bitsLeft += 5;
            if (bitsLeft >= 8)
            {
                bitsLeft -= 8;
                output.Add((byte)((buffer >> bitsLeft) & 0xFF));
            }
        }

        return output.ToArray();
    }
}
