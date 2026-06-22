using System.Security.Cryptography;

namespace Legi.Identity.Application.Common;

/// <summary>
/// Generates and normalizes MFA recovery codes. Codes are displayed grouped (e.g.
/// "ABCDE-FGHJK") but hashed/compared in normalized form so users can re-enter them
/// with or without separators or casing.
/// </summary>
public static class MfaRecoveryCodeGenerator
{
    public const int Count = 10;
    private const int GroupLength = 5;
    // No ambiguous characters (0/O/1/I/L excluded) to keep codes easy to transcribe.
    private const string Alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

    public static IReadOnlyList<string> Generate()
    {
        var codes = new List<string>(Count);
        for (var i = 0; i < Count; i++)
        {
            var chars = new char[GroupLength * 2];
            for (var j = 0; j < chars.Length; j++)
                chars[j] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];

            codes.Add($"{new string(chars, 0, GroupLength)}-{new string(chars, GroupLength, GroupLength)}");
        }

        return codes;
    }

    /// <summary>Strips separators and upper-cases so hashing/comparison is format-insensitive.</summary>
    public static string Normalize(string code)
        => new(code.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());
}
