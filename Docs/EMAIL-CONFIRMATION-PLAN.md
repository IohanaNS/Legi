# Email Confirmation For BukiHub Registration

## Summary
Add email confirmation to Identity so registration creates an account but does not create an auth session. The user sees “account created, check your email,” clicks a frontend confirmation link, and can log in only after confirmation.

Confirmed choices:
- Keep `UserRegistered`/Social profile creation on registration.
- Backfill all existing users as confirmed.
- Include a resend-confirmation flow.

## Key Changes
- Add `EmailConfirmedAt`/`IsEmailConfirmed` to `User`, plus one-time hashed email confirmation tokens stored in a new `email_confirmation_tokens` table.
    - Mirror the existing password-reset token shape: `EmailConfirmationToken : BaseEntity` with `TokenHash`/`ExpiresAt`/`CreatedAt`/`UsedAt` and `IsExpired`/`IsUsed`/`IsActive`, an `internal` constructor + `MarkUsed()`, owned by the `User` aggregate via `AddEmailConfirmationToken(...)` (invalidates prior active tokens, identical to `AddPasswordResetToken`) and `ConfirmEmail(tokenHash, utcNow)` (mirrors `RedeemPasswordReset`, sets `EmailConfirmedAt` + marks token used).
- Add an EF migration that:
    - Adds `users.email_confirmed_at` (nullable).
    - Backfills existing users with `created_at` (so existing accounts are confirmed).
    - Creates `email_confirmation_tokens` with unique `token_hash`, `expires_at`, `created_at`, `used_at`, and `user_id`, plus the matching `EmailConfirmationTokenConfiguration` and `DbSet`.
- Repository / infrastructure:
    - Add `GetByEmailOrUsernameWithEmailConfirmationTokensAsync(...)` to `IUserRepository` (analog of `GetByEmailWithPasswordResetTokensAsync`) for resend/login/confirm flows, and a `ConfirmEmailAsync(tokenHash, utcNow, ct)` repo method mirroring `RedeemPasswordResetTokenAsync` (returns `false` for invalid/expired/used so the handler can map to 404).
    - Generalize the token factory rather than duplicating SHA-256 logic: reuse `IPasswordResetTokenFactory`'s `(Token, Hash) Create()` / `Hash(token)` by renaming it to a neutral `ISecureTokenFactory` (or add a sibling registration), so confirmation tokens share one audited implementation.
- Change registration:
    - `POST /identity/auth/register` still returns `201`, but no longer returns JWT/refresh token or sets the refresh cookie. The controller stops calling `RefreshTokenCookie.Append` on register and returns a new response DTO instead of `AuthSessionResponse`.
    - Register accepts `language?` (add to `RegisterRequest`, `RegisterCommand`, and the controller mapping) so the first confirmation email uses the same localization behavior as password reset.
    - Response becomes a registration-created shape like `userId`, `email`, `username`, `emailConfirmationRequired: true` (new `RegisterResponse`/`RegistrationCreatedResponse` DTO; `RegisterCommandHandler` drops the `IJwtTokenService`/refresh-token wiring).
    - Register command creates a confirmation token, saves the account, sends the confirmation email, and logs email-send failures without issuing a session (account is still created even if the email send fails — the user can recover via resend).
- Add confirmation/resend APIs:
    - `POST /identity/auth/confirm-email` with `{ token }` returns `204`; invalid/expired/used token returns `404`.
    - `POST /identity/auth/resend-confirmation` with `{ emailOrUsername, language?, turnstileToken? }` always returns `204` for anti-enumeration; it only sends when the account exists and is unconfirmed.
    - `POST /identity/auth/resend-confirmation` enforces a backend 3-minute cooldown per unconfirmed account. If the most recent confirmation token/email was created less than 3 minutes ago, return the same anti-enumeration response and do not send another email.
    - Resend uses Turnstile/rate-limit protection like other unauthenticated email-sending flows.
- Login/refresh security:
    - Login verifies password first; only then returns `403` with `emailConfirmationRequired: true` if the account is unconfirmed. Verifying password first prevents the 403 from being used as an account-existence/confirmation oracle.
    - Add a new `EmailConfirmationRequiredException` in the Identity Application exceptions and a corresponding case in `ExceptionHandlingMiddleware` that emits `403` ProblemDetails with `emailConfirmationRequired: true` in `Extensions` (the frontend keys off this extension to distinguish it from Turnstile `403` and invalid-credentials `401`). This is a required, explicit work item — the existing middleware only maps `HumanVerificationRequiredException` to `403`.
    - On the correct-password-but-unconfirmed path, still clear the failed-attempt/lockout counter (`ClearAsync` + `RecordSuccessfulLogin` semantics) before throwing — the credentials were valid, so a legit unconfirmed user must not get locked out by repeated attempts — but do **not** issue tokens.
    - Wrong passwords still return the normal invalid-credentials path and count toward lockout.
    - Refresh rejects unconfirmed users defensively and does not issue new tokens. Note this is defense-in-depth only: since login never issues a refresh token to an unconfirmed user, this path is not normally reachable. Confirm `RotateRefreshTokenAsync` (or an added read) actually exposes `IsEmailConfirmed`.
- Email layout:
    - Extract the existing password-reset email layout into a shared BukiHub action-email builder.
    - Keep password reset visually unchanged.
    - Add an email-confirmation template using the same logo, table layout, inline styles, HTML body, plain-text body, localization, and expiry note.
    - Add an `EmailConfirmationSettings` options class mirroring `PasswordResetSettings`: `FrontendBaseUrl` (no default, validated absolute http/https — needed to build `/confirm-email?token=...`) and `TokenLifetimeMinutes` defaulting to `1440`, bound from the `EmailConfirmation` config section and validated on startup like `PasswordReset`.

## Frontend Changes
- Update auth types/API so registration no longer persists a session.
- Register page shows a success state instead of navigating to `/feed`.
- Add `/confirm-email?token=...` page that calls the confirm endpoint and shows success, expired/invalid, and generic-error states.
- Login page detects `emailConfirmationRequired` separately from Turnstile and invalid credentials, then offers resend confirmation.
- Add English and Portuguese translations for registration success, confirm-email page states, unconfirmed-login message, and resend states.

## Tests
- Domain tests:
    - New users are unconfirmed by default.
    - Confirmation token creation invalidates prior active confirmation tokens.
    - Valid confirmation sets `EmailConfirmedAt` and marks the token used.
    - Expired/used/unknown confirmation tokens fail.
- Application tests:
    - Register creates user + confirmation token, sends email, and returns no auth tokens.
    - Login succeeds only for confirmed users.
    - Login with correct password but unconfirmed email returns the email-confirmation-required error.
    - Login with wrong password for an unconfirmed user still returns invalid credentials.
    - Resend is anti-enumeration and only sends for existing unconfirmed accounts.
    - Confirm-email handler hashes the raw token and maps invalid redemption to not found.
    - Resend within the 3-minute cooldown returns success but does not create a token or send email.
    - Resend after the cooldown invalidates prior active confirmation tokens and sends a new email.
- Email tests:
    - Confirmation email supports English/Portuguese fallback behavior.
    - Includes confirmation URL and expiry in HTML/text.
    - Escapes username and embeds the same inline logo.
    - After extracting the shared action-email builder, a regression assertion that the password-reset email output is unchanged (subject + key markup), so the refactor can't silently alter it.
- Update existing tests (these break with the behavior change, not optional):
    - Register handler/controller tests that assert JWT/refresh issuance and the `/feed` redirect — update to the new no-session 201 shape.
    - Login handler tests — confirmed users still log in; add the unconfirmed branches below.
- Verification commands:
    - `dotnet test Legi.sln --settings tests/.runsettings`
    - `yarn lint`
    - `yarn build`

## Assumptions / accepted tradeoffs
- Existing users remain usable by marking them confirmed in the migration.
- Unconfirmed users may already have Social profiles because `UserRegistered` continues to mean “account created.”
- **Accepted tradeoff (username squatting):** because `UserRegistered` fires before confirmation, an unconfirmed registration permanently reserves the email/username and creates a public Social profile. This is a known low-severity abuse vector and a minor enumeration surface (the public profile becomes queryable). Accepted for now to keep `UserRegistered` = “account created”; revisit only if abuse appears (e.g. a janitor that prunes long-unconfirmed accounts).
- Confirmation links go to the frontend first, matching the reset-password flow: `/confirm-email?token=...`.
- Tokens are never stored raw; only SHA-256 hashes are persisted.
