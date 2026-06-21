namespace Legi.Identity.Application.Common.Models;

/// <summary>
/// Security-relevant events worth auditing for attack detection and forensics.
/// Numeric values are stable so they can be used as log EventIds and alerted on.
/// </summary>
public enum SecurityEventType
{
    LoginSucceeded = 1000,
    LoginFailed = 1001,
    LoginBlockedLockout = 1002,
    PasswordResetCompleted = 1003,
    AccountDeleted = 1004,
    ExternalLoginSucceeded = 1005,
    AccountRegistered = 1006,
    EmailConfirmed = 1007,
}

/// <summary>
/// A single audit record. <see cref="Identifier"/> carries the attempted email/username
/// when <see cref="UserId"/> is not known (e.g. a failed login for a non-existent user).
/// </summary>
public sealed record SecurityAuditEvent(
    SecurityEventType Type,
    Guid? UserId = null,
    string? Identifier = null,
    string? IpAddress = null,
    string? Detail = null);
