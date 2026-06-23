using System.Security.Cryptography;

namespace Legi.Identity.Application.Common;

/// <summary>
/// Generates and normalizes the 6-digit one-time codes emailed as a second factor.
/// The code is short on purpose (easy to type); its safety comes from short expiry,
/// the per-code attempt cap, and single use — not from length.
/// </summary>
public static class MfaEmailCodeGenerator
{
    public const int Digits = 6;

    public static string Generate()
        => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

    /// <summary>Strips everything but digits so a pasted "123 456" still matches.</summary>
    public static string Normalize(string code)
        => new(code.Where(char.IsDigit).ToArray());
}
