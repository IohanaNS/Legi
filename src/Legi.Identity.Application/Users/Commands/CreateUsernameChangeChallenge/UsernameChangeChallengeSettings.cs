namespace Legi.Identity.Application.Users.Commands.CreateUsernameChangeChallenge;

public class UsernameChangeChallengeSettings
{
    public const string SectionName = "UsernameChangeChallengeSettings";
    public const string ValidationMessage =
        "UsernameChangeChallengeSettings must have positive values.";

    public int MaxFailedAttempts { get; init; } = 5;
    public int FailureWindowMinutes { get; init; } = 15;
    public int LockoutDurationMinutes { get; init; } = 15;

    public TimeSpan FailureWindow => TimeSpan.FromMinutes(FailureWindowMinutes);
    public TimeSpan LockoutDuration => TimeSpan.FromMinutes(LockoutDurationMinutes);

    public static bool HasValidSettings(UsernameChangeChallengeSettings settings) =>
        settings.MaxFailedAttempts > 0 &&
        settings.FailureWindowMinutes > 0 &&
        settings.LockoutDurationMinutes > 0;
}
