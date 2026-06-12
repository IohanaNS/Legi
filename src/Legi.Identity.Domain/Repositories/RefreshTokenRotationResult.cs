using Legi.Identity.Domain.Entities;

namespace Legi.Identity.Domain.Repositories;

public sealed record RefreshTokenRotationResult(
    RefreshTokenRotationStatus Status,
    User? User)
{
    public static RefreshTokenRotationResult Success(User user) =>
        new(RefreshTokenRotationStatus.Success, user);

    public static RefreshTokenRotationResult Invalid() =>
        new(RefreshTokenRotationStatus.Invalid, null);

    public static RefreshTokenRotationResult ReplayDetected() =>
        new(RefreshTokenRotationStatus.ReplayDetected, null);
}

public enum RefreshTokenRotationStatus
{
    Success,
    Invalid,
    ReplayDetected
}
