namespace Legi.Identity.Application.Common.Models;

public class BreachedPasswordSettings
{
    public const string SectionName = "BreachedPassword";

    /// <summary>When false, the breach check is skipped (passwords are never rejected for this reason).</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Base URL of the Have I Been Pwned "range" API.</summary>
    public string ApiBaseUrl { get; set; } = "https://api.pwnedpasswords.com/";

    /// <summary>HTTP timeout; on timeout the check fails open (password allowed).</summary>
    public int TimeoutSeconds { get; set; } = 5;
}
