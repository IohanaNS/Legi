namespace Legi.Contracts.Identity;

/// <summary>
/// Published when a new user completes registration in the Identity service.
/// 
/// Consumers (Library, Social) use this to create their local UserProfile
/// snapshots with the username and email captured at registration time.
/// 
/// See MESSAGING-ARCHITECTURE-decisions.md, section 6.2.
/// </summary>
/// <param name="UserId">Identity's user identifier; same UUID used everywhere.</param>
/// <param name="Username">The username chosen at registration.</param>
/// <param name="Email">User's email at registration time.</param>
/// <param name="RegisteredAt">UTC timestamp of the registration.</param>
public sealed record UserRegisteredIntegrationEvent(
    Guid UserId,
    string Username,
    string Email,
    DateTime RegisteredAt
) : IIntegrationEvent;