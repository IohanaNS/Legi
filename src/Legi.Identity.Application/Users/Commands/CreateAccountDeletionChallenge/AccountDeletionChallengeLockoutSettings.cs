namespace Legi.Identity.Application.Users.Commands.CreateAccountDeletionChallenge;

public class AccountDeletionChallengeLockoutSettings
{
    public const string SectionName = "AccountDeletionChallengeLockout";
    public const string ValidationMessage =
        "AccountDeletionChallengeLockout settings must be positive values.";

    public int MaxFailedAttempts { get; init; } = 5;
    public int FailureWindowMinutes { get; init; } = 15;
    public int LockoutDurationMinutes { get; init; } = 15;

    public TimeSpan FailureWindow => TimeSpan.FromMinutes(FailureWindowMinutes);
    public TimeSpan LockoutDuration => TimeSpan.FromMinutes(LockoutDurationMinutes);

    public static bool HasValidSettings(AccountDeletionChallengeLockoutSettings settings)
    {
        return settings.MaxFailedAttempts > 0 &&
               settings.FailureWindowMinutes > 0 &&
               settings.LockoutDurationMinutes > 0;
    }
}
