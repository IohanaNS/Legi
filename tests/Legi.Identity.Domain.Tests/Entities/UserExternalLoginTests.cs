using Legi.Identity.Domain.Entities;
using Legi.Identity.Domain.Events;
using Legi.Identity.Domain.Tests.Factories;

namespace Legi.Identity.Domain.Tests.Entities;

public class UserExternalLoginTests
{
    private const string Provider = "google";
    private const string ProviderKey = "google-sub-123";

    [Fact]
    public void CreateFromExternalLogin_ShouldCreatePasswordlessConfirmedUser()
    {
        var email = EmailFactory.Create();
        var username = UsernameFactory.Create();
        var confirmedAt = DateTime.UtcNow;

        var user = User.CreateFromExternalLogin(email, username, Provider, ProviderKey, confirmedAt);

        Assert.Equal(email, user.Email);
        Assert.Equal(username, user.Username);
        Assert.Null(user.PasswordHash);
        Assert.True(user.IsEmailConfirmed);
        Assert.Equal(confirmedAt, user.EmailConfirmedAt);
        var login = Assert.Single(user.ExternalLogins);
        Assert.Equal(Provider, login.Provider);
        Assert.Equal(ProviderKey, login.ProviderKey);
    }

    [Fact]
    public void CreateFromExternalLogin_ShouldRaiseUserRegisteredEvent()
    {
        var user = User.CreateFromExternalLogin(
            EmailFactory.Create(),
            UsernameFactory.Create(),
            Provider,
            ProviderKey,
            DateTime.UtcNow);

        var domainEvent = Assert.IsType<UserRegisteredDomainEvent>(Assert.Single(user.DomainEvents));
        Assert.Equal(user.Id, domainEvent.UserId);
        Assert.Equal(user.Email.Value, domainEvent.Email);
        Assert.Equal(user.Username.Value, domainEvent.Username);
    }

    [Fact]
    public void AddExternalLogin_ShouldLinkProviderToExistingUser()
    {
        var user = UserFactory.Create();

        user.AddExternalLogin(Provider, ProviderKey);

        var login = Assert.Single(user.ExternalLogins);
        Assert.Equal(Provider, login.Provider);
        Assert.Equal(ProviderKey, login.ProviderKey);
    }

    [Fact]
    public void AddExternalLogin_ShouldBeIdempotentForSameProviderKey()
    {
        var user = UserFactory.Create();

        user.AddExternalLogin(Provider, ProviderKey);
        user.AddExternalLogin(Provider, ProviderKey);

        Assert.Single(user.ExternalLogins);
    }

    [Fact]
    public void ConfirmEmailFromExternalProvider_ShouldConfirmUnconfirmedEmail()
    {
        // A freshly created email/password user is unconfirmed.
        var user = UserFactory.Create();
        Assert.False(user.IsEmailConfirmed);
        var now = DateTime.UtcNow;

        user.ConfirmEmailFromExternalProvider(now);

        Assert.True(user.IsEmailConfirmed);
        Assert.Equal(now, user.EmailConfirmedAt);
    }

    [Fact]
    public void ConfirmEmailFromExternalProvider_ShouldNotOverwriteExistingConfirmation()
    {
        var user = UserFactory.Create();
        user.AddEmailConfirmationToken("token_hash", DateTime.UtcNow.AddDays(1));
        user.ConfirmEmail("token_hash", DateTime.UtcNow);
        var originalConfirmedAt = user.EmailConfirmedAt;

        user.ConfirmEmailFromExternalProvider(DateTime.UtcNow.AddDays(1));

        Assert.Equal(originalConfirmedAt, user.EmailConfirmedAt);
    }

    [Fact]
    public void LinkVerifiedExternalLogin_OnUnconfirmedAccount_RevokesPasswordAndSessions()
    {
        // Pre-hijacking guard: an unconfirmed account never proved email ownership,
        // so its credential/sessions must be revoked when the verified provider claims it.
        var user = UserFactory.Create(); // unconfirmed, has a password
        user.AddRefreshToken("existing_token_hash", DateTime.UtcNow.AddDays(7));
        Assert.NotNull(user.PasswordHash);
        Assert.Contains(user.RefreshTokens, t => t.IsActive);

        user.LinkVerifiedExternalLogin(Provider, ProviderKey, DateTime.UtcNow);

        Assert.Null(user.PasswordHash);
        Assert.DoesNotContain(user.RefreshTokens, t => t.IsActive);
        Assert.True(user.IsEmailConfirmed);
        Assert.Single(user.ExternalLogins, l => l.Provider == Provider && l.ProviderKey == ProviderKey);
    }

    [Fact]
    public void LinkVerifiedExternalLogin_OnConfirmedAccount_KeepsPasswordAndSessions()
    {
        var user = UserFactory.Create();
        user.AddEmailConfirmationToken("token_hash", DateTime.UtcNow.AddDays(1));
        user.ConfirmEmail("token_hash", DateTime.UtcNow);
        user.AddRefreshToken("existing_token_hash", DateTime.UtcNow.AddDays(7));
        var passwordBefore = user.PasswordHash;

        user.LinkVerifiedExternalLogin(Provider, ProviderKey, DateTime.UtcNow);

        Assert.Equal(passwordBefore, user.PasswordHash);
        Assert.Contains(user.RefreshTokens, t => t.IsActive);
        Assert.True(user.IsEmailConfirmed);
        Assert.Single(user.ExternalLogins, l => l.Provider == Provider && l.ProviderKey == ProviderKey);
    }
}
