using Legi.Identity.Domain.Enums;
using Legi.Identity.Domain.Tests.Factories;
using Legi.SharedKernel;

namespace Legi.Identity.Domain.Tests.Entities;

public class UserMfaTests
{
    private static readonly string[] RecoveryHashes = ["hash-1", "hash-2", "hash-3"];

    [Fact]
    public void StartMfaEnrollment_StoresSecret_ButLeavesMfaDisabled()
    {
        var user = UserFactory.Create();

        user.StartMfaEnrollment("encrypted-secret", DateTime.UtcNow);

        Assert.False(user.MfaEnabled);
        Assert.Equal("encrypted-secret", user.TotpSecret);
        Assert.Null(user.MfaEnabledAt);
    }

    [Fact]
    public void StartMfaEnrollment_Throws_WhenSecretEmpty()
    {
        var user = UserFactory.Create();

        Assert.Throws<DomainException>(() => user.StartMfaEnrollment("  ", DateTime.UtcNow));
    }

    [Fact]
    public void ConfirmMfaEnrollment_EnablesMfa_AndStoresRecoveryCodes()
    {
        var user = UserFactory.Create();
        user.StartMfaEnrollment("encrypted-secret", DateTime.UtcNow);

        user.ConfirmMfaEnrollment(RecoveryHashes, DateTime.UtcNow);

        Assert.True(user.MfaEnabled);
        Assert.Equal(MfaMethod.Totp, user.MfaMethod);
        Assert.NotNull(user.MfaEnabledAt);
        Assert.Equal(3, user.MfaRecoveryCodes.Count);
        Assert.All(user.MfaRecoveryCodes, c => Assert.False(c.IsUsed));
    }

    [Fact]
    public void EnableEmailMfa_EnablesEmailMethod_WithoutSecret_AndStoresRecoveryCodes()
    {
        var user = UserFactory.Create();

        user.EnableEmailMfa(RecoveryHashes, DateTime.UtcNow);

        Assert.True(user.MfaEnabled);
        Assert.Equal(MfaMethod.Email, user.MfaMethod);
        Assert.Null(user.TotpSecret);
        Assert.NotNull(user.MfaEnabledAt);
        Assert.Equal(3, user.MfaRecoveryCodes.Count);
    }

    [Fact]
    public void EnableEmailMfa_Throws_WhenAlreadyEnabled()
    {
        var user = UserFactory.Create();
        user.EnableEmailMfa(RecoveryHashes, DateTime.UtcNow);

        Assert.Throws<DomainException>(() => user.EnableEmailMfa(RecoveryHashes, DateTime.UtcNow));
    }

    [Fact]
    public void DisableMfa_ResetsMethodToNone_ForEmailMethod()
    {
        var user = UserFactory.Create();
        user.EnableEmailMfa(RecoveryHashes, DateTime.UtcNow);

        user.DisableMfa(DateTime.UtcNow);

        Assert.False(user.MfaEnabled);
        Assert.Equal(MfaMethod.None, user.MfaMethod);
        Assert.Empty(user.MfaRecoveryCodes);
    }

    [Fact]
    public void ConfirmMfaEnrollment_Throws_WhenNoEnrollmentInProgress()
    {
        var user = UserFactory.Create();

        Assert.Throws<DomainException>(() => user.ConfirmMfaEnrollment(RecoveryHashes, DateTime.UtcNow));
    }

    [Fact]
    public void StartMfaEnrollment_Throws_WhenAlreadyEnabled()
    {
        var user = UserFactory.Create();
        user.StartMfaEnrollment("encrypted-secret", DateTime.UtcNow);
        user.ConfirmMfaEnrollment(RecoveryHashes, DateTime.UtcNow);

        Assert.Throws<DomainException>(() => user.StartMfaEnrollment("another", DateTime.UtcNow));
    }

    [Fact]
    public void DisableMfa_ClearsSecretAndRecoveryCodes()
    {
        var user = UserFactory.Create();
        user.StartMfaEnrollment("encrypted-secret", DateTime.UtcNow);
        user.ConfirmMfaEnrollment(RecoveryHashes, DateTime.UtcNow);

        user.DisableMfa(DateTime.UtcNow);

        Assert.False(user.MfaEnabled);
        Assert.Null(user.TotpSecret);
        Assert.Null(user.MfaEnabledAt);
        Assert.Empty(user.MfaRecoveryCodes);
    }

    [Fact]
    public void DisableMfa_Throws_WhenNotEnabled()
    {
        var user = UserFactory.Create();

        Assert.Throws<DomainException>(() => user.DisableMfa(DateTime.UtcNow));
    }

    [Fact]
    public void TryConsumeRecoveryCode_MarksUsed_AndRejectsReuseOrUnknown()
    {
        var user = UserFactory.Create();
        user.StartMfaEnrollment("encrypted-secret", DateTime.UtcNow);
        user.ConfirmMfaEnrollment(RecoveryHashes, DateTime.UtcNow);

        Assert.True(user.TryConsumeRecoveryCode("hash-2", DateTime.UtcNow));   // consumed
        Assert.False(user.TryConsumeRecoveryCode("hash-2", DateTime.UtcNow));  // already used
        Assert.False(user.TryConsumeRecoveryCode("unknown", DateTime.UtcNow)); // not a code
        Assert.Single(user.MfaRecoveryCodes, c => c.IsUsed);
    }
}
