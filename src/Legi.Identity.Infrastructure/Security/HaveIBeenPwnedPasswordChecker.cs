using System.Security.Cryptography;
using System.Text;
using Legi.Identity.Application.Common.Interfaces;
using Legi.Identity.Application.Common.Models;
using Microsoft.Extensions.Logging;

namespace Legi.Identity.Infrastructure.Security;

/// <summary>
/// Checks a password against the Have I Been Pwned "Pwned Passwords" range API using
/// k-anonymity: only the first 5 hex characters of the password's SHA-1 hash ever leave
/// this process — never the password, and never its full hash. Fails OPEN (returns
/// not-breached) on any transport error so an HIBP outage cannot block sign-up or reset.
/// </summary>
/// <remarks>
/// SHA-1 is used here because the HIBP API is keyed by SHA-1; it is NOT used as a
/// password hash (passwords are hashed with BCrypt elsewhere).
/// </remarks>
public sealed class HaveIBeenPwnedPasswordChecker(
    HttpClient httpClient,
    BreachedPasswordSettings settings,
    ILogger<HaveIBeenPwnedPasswordChecker> logger) : IBreachedPasswordChecker
{
    public async Task<bool> IsBreachedAsync(string password, CancellationToken cancellationToken = default)
    {
        if (!settings.Enabled || string.IsNullOrEmpty(password))
            return false;

        var (prefix, suffix) = HashPrefixSuffix(password);

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"range/{prefix}");
            // Padded responses hide how many real hashes share the prefix (extra privacy).
            request.Headers.Add("Add-Padding", "true");

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return false;

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            return ContainsBreachedSuffix(body, suffix);
        }
        catch (Exception ex) when (
            ex is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            // Fail open: availability over strictness. Logged so persistent outages are visible.
            logger.LogWarning(ex, "Breached-password check failed; allowing the password (fail-open).");
            return false;
        }
    }

    private static (string Prefix, string Suffix) HashPrefixSuffix(string password)
    {
        var hash = Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(password))); // uppercase hex
        return (hash[..5], hash[5..]);
    }

    private static bool ContainsBreachedSuffix(string responseBody, string suffix)
    {
        foreach (var line in responseBody.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var separator = line.IndexOf(':');
            if (separator < 0)
                continue;

            if (!line.AsSpan(0, separator).Equals(suffix, StringComparison.OrdinalIgnoreCase))
                continue;

            // With Add-Padding, padded (fake) entries have a count of 0; real breaches are > 0.
            return long.TryParse(line.AsSpan(separator + 1), out var count) && count > 0;
        }

        return false;
    }
}
