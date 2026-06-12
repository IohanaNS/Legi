namespace Legi.Identity.Application.Auth.Commands.Login;

public class LoginLockoutSettings
{
    public const string SectionName = "LoginLockout";
    public const string ValidationMessage =
        "LoginLockout settings must be positive values.";

    public int MaxFailedAttempts { get; init; } = 5;
    public int FailureWindowMinutes { get; init; } = 15;
    public int LockoutDurationMinutes { get; init; } = 15;

    public TimeSpan FailureWindow => TimeSpan.FromMinutes(FailureWindowMinutes);
    public TimeSpan LockoutDuration => TimeSpan.FromMinutes(LockoutDurationMinutes);

    public static bool HasValidSettings(LoginLockoutSettings settings)
    {
        return settings.MaxFailedAttempts > 0 &&
               settings.FailureWindowMinutes > 0 &&
               settings.LockoutDurationMinutes > 0;
    }
}
