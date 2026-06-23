namespace Legi.Identity.Domain.Enums;

/// <summary>
/// The second-factor method a user has enrolled. A user has exactly one method at a
/// time (or <see cref="None"/> when MFA is off); recovery codes work with either method.
/// </summary>
public enum MfaMethod
{
    None = 0,

    /// <summary>RFC 6238 TOTP via an authenticator app (the strong, device-bound method).</summary>
    Totp = 1,

    /// <summary>One-time codes emailed to the account address (lower friction, weaker).</summary>
    Email = 2
}
