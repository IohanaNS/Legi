using System.Text;

namespace Legi.Identity.Application.Common;

/// <summary>
/// Derives candidate usernames that satisfy the <c>Username</c> value object rules
/// (lowercase, starts with a letter, only [a-z0-9_], 3-30 chars) from an arbitrary
/// seed such as an email local-part or a display name. Pure functions — the caller
/// is responsible for checking uniqueness and appending suffixes.
/// </summary>
public static class UsernameGenerator
{
    private const int MinLength = 3;
    private const int MaxLength = 30;
    private const string Fallback = "reader";

    /// <summary>
    /// Produces a sanitized base username from a seed.
    /// </summary>
    public static string CreateBase(string? seed)
    {
        var sanitized = Sanitize(seed);

        if (sanitized.Length < MinLength)
            sanitized = Sanitize(Fallback);

        return Truncate(sanitized, MaxLength);
    }

    /// <summary>
    /// Appends a numeric suffix to a base username, trimming the base so the result
    /// stays within the max length.
    /// </summary>
    public static string WithSuffix(string baseName, int suffix)
    {
        var suffixText = suffix.ToString();
        var room = MaxLength - suffixText.Length;
        var trimmedBase = Truncate(baseName, Math.Max(MinLength, room));
        return trimmedBase + suffixText;
    }

    private static string Sanitize(string? seed)
    {
        if (string.IsNullOrWhiteSpace(seed))
            return string.Empty;

        // Take the local part if an email was passed in.
        var atIndex = seed.IndexOf('@');
        if (atIndex > 0)
            seed = seed[..atIndex];

        var builder = new StringBuilder(seed.Length);
        foreach (var ch in seed.Trim().ToLowerInvariant())
        {
            if (ch is (>= 'a' and <= 'z') or (>= '0' and <= '9') || ch == '_')
                builder.Append(ch);
        }

        var result = builder.ToString();

        // Must start with a letter.
        var firstLetter = 0;
        while (firstLetter < result.Length && !(result[firstLetter] is >= 'a' and <= 'z'))
            firstLetter++;

        if (firstLetter == result.Length)
            return string.Empty; // no letters at all

        return result[firstLetter..];
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
