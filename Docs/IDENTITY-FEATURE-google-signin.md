# Identity Feature — Sign in with Google

Status: **Design** (reviewed against code 2026-06-19)
Bounded context: **Identity**

Lets a user **register or log in with their Google account** alongside the existing
email/password flow. A single Google button on both the Login and Register pages
returns a Google ID token; the backend validates it and either logs in the matching
user, links Google to an existing account, or creates a new account.

## Decisions (locked)

| Topic | Decision | Rationale |
|-------|----------|-----------|
| OAuth flow | **Google Identity Services (GIS) button → ID token → backend validation** | No client secret, no redirect dance; the modern SPA approach. Frontend gets a signed ID token and POSTs it to `/auth/google`. |
| Account linking | **Auto-link by verified email** | Google emails are verified, so attach the Google login to an existing account and sign them in. One account, two sign-in methods. |
| Username for new Google users | **Auto-generate** (sanitized from email/name, uniqueness suffix), editable later in profile | Zero-friction signup; the `Username` VO is strict so it can't come straight from Google. |
| Account model | **`ExternalLogin` child entity** on the `User` aggregate (not a single `GoogleId` column) | Extensible to other providers; mirrors existing child-collection patterns (`RefreshToken`, `PasswordResetToken`). |

## Existing-code constraints discovered during review

These shaped the plan and **must** be respected:

- **`LoginCommandHandler` will 500 on a null password hash.**
  [`LoginCommandHandler.cs:52`](../src/Legi.Identity.Application/Auth/Commands/Login/LoginCommandHandler.cs#L52)
  passes `user.PasswordHash` directly into `passwordHasher.Verify(...)`. With a
  Google-only (passwordless) user this throws inside BCrypt. **Must guard before the verify call.**
- **Refresh-token rotation hard-requires a confirmed email.**
  [`UserRepository.cs:224`](../src/Legi.Identity.Infrastructure/Persistence/Repositories/UserRepository.cs#L224)
  returns `Invalid()` when `!user.IsEmailConfirmed`. So the **link branch must set
  `EmailConfirmedAt`** when an existing-but-unconfirmed account signs in with Google,
  otherwise the user is silently logged out on their next refresh (~60 min).
- **Repository loads only include `RefreshTokens`.**
  `GetByIdAsync` / `GetByEmailAsync` ([`UserRepository.cs`](../src/Legi.Identity.Infrastructure/Persistence/Repositories/UserRepository.cs))
  `.Include(u => u.RefreshTokens)` only. Any write-path load that touches external
  logins must include them, and the new `GetByExternalLoginAsync` must include them.
- **Repository lookups already normalize** email/username (`Trim().ToLowerInvariant()`),
  so passing the raw Google email to the repo is fine — but the **stored** value must
  still go through `Email.Create(...)` for format validation.
- **No code change needed for delete/forgot/reset-password:**
  - `DeleteAccountCommandHandler` does **not** require a password — passwordless users can delete fine.
  - `UpdatePassword` / forgot-password / reset-password key off tokens, not password
    state. Leaving them as-is means a Google-only user *can* use "forgot password" to
    **add** a password — this is intended behavior, not a bug.
- **CLAUDE.md drift (informational):** docs claim the access token has a `name` claim,
  but [`JwtTokenService.cs:20`](../src/Legi.Identity.Infrastructure/Security/JwtTokenService.cs#L20)
  emits only `sub/email/jti/iat`. Don't rely on a `name` claim.

---

## 1. Domain (`Legi.Identity.Domain`)

### New child entity `ExternalLogin`
Owned by the `User` aggregate (like `RefreshToken`).
- `Provider` (string, e.g. `"google"`)
- `ProviderKey` (Google `sub` claim — stable, immutable user id)
- `CreatedAt`
- Natural uniqueness: `(Provider, ProviderKey)` across all users.

### `User` changes
- `PasswordHash` → **nullable** (`string?`). Google-only users have no password.
- New `_externalLogins` collection + read-only exposure (mirror `RefreshTokens`).
- New factory **`User.CreateFromExternalLogin(email, username, provider, providerKey, emailConfirmedAtUtc)`**:
  - sets `EmailConfirmedAt` immediately (Google `email_verified`),
  - no password,
  - adds the external login,
  - raises `UserRegisteredDomainEvent` (unchanged → Social still creates `UserProfile`).
- **`AddExternalLogin(provider, providerKey)`** — guards duplicates.
- **`ConfirmEmailFromExternalProvider(utcNow)`** (or reuse a setter) — sets
  `EmailConfirmedAt` if null. Used by the **link** branch. *(Mandatory — see constraints.)*

### Domain tests
- `CreateFromExternalLogin`: no password, email pre-confirmed, `UserRegisteredDomainEvent` raised.
- `AddExternalLogin`: duplicate `(provider, providerKey)` guard.
- Confirm-on-link sets `EmailConfirmedAt` only when null (idempotent).

---

## 2. Application (`Legi.Identity.Application`)

### New interface `IGoogleTokenValidator` (`Common/Interfaces`)
```csharp
Task<GoogleUserInfo?> ValidateAsync(string idToken, CancellationToken ct);
```
`GoogleUserInfo` carries `Sub`, `Email`, `EmailVerified`, `Name`, `Picture`
(picture for future avatar use). Returns `null` on invalid token.

### New command `Auth/Commands/GoogleSignIn/`
`GoogleSignInCommand(string IdToken, string? RemoteIpAddress) : IRequest<LoginResponse>`
(reuses the existing `LoginResponse`; running through the pipeline gives validation/logging).

Handler flow:
1. **Validate** the ID token → `GoogleUserInfo`. Reject if invalid **or** `EmailVerified == false`.
2. Look up by **`GetByExternalLoginAsync("google", sub)`** → if found, that's the user.
3. Else look up by **email** (`GetByEmailAsync`):
   - **Found → auto-link:** `AddExternalLogin("google", sub)` **and** confirm email if not already.
   - **Not found → new user:** generate a unique username (§5),
     `User.CreateFromExternalLogin(...)`, persist.
4. `RecordSuccessfulLogin`, mint access token + refresh token, persist — identical tail to `LoginCommandHandler`.
5. **No email-confirmation gate** (Google email is verified). No Turnstile (Google gates bots).
6. **Creation race:** wrap the new-user `AddAsync` in a catch for unique-constraint
   violation (`DbUpdateException`) → re-query by external login and continue. Guards
   against double-submit (One Tap + button firing concurrently for a first-time user).

### Repository contract
Add to `IUserRepository`:
`Task<User?> GetByExternalLoginAsync(string provider, string providerKey, CancellationToken ct)`
— must `.Include(u => u.ExternalLogins)`.

### Application tests
`GoogleSignInCommandHandler`: new user; link-by-email (asserts email gets confirmed);
existing external-login; invalid token rejected; unverified email rejected; username
collision → suffixed; concurrent-create unique-violation handled. Mock `IGoogleTokenValidator` + repos.

---

## 3. Infrastructure (`Legi.Identity.Infrastructure`)

- **`GoogleTokenValidator : IGoogleTokenValidator`** using `Google.Apis.Auth`
  (`GoogleJsonWebSignature.ValidateAsync` with `Audience = [ClientId]`). The library
  verifies signature, `iss ∈ {accounts.google.com, https://accounts.google.com}`,
  `aud`, and `exp`. Add the **`Google.Apis.Auth`** NuGet package.
- **`GoogleAuthSettings`** (Options): `ClientId`. Bind from config; register in DI next to `JwtSettings`.
- **`ExternalLoginConfiguration`** — table `external_logins`, FK to user (cascade delete,
  matching `RefreshToken`/`PasswordResetToken`), unique index on `(provider, provider_key)`,
  navigation `PropertyAccessMode.Field`.
- **`UserConfiguration`** — make `password_hash` **nullable**; add `HasMany(u => u.ExternalLogins)`.
- **`UserRepository`** — implement `GetByExternalLoginAsync`; add `.Include(u => u.ExternalLogins)`
  to write-path loads that touch external logins.
- **Migration `AddExternalLogins`** — create `external_logins`; alter `password_hash` → nullable.
  (APIs run `Database.Migrate()` on startup; existing rows keep their hashes — safe.)
- Register `IGoogleTokenValidator` + `GoogleAuthSettings` in Infrastructure `DependencyInjection`.

---

## 4. API (`Legi.Identity.Api`)

- **`POST /api/v1/identity/auth/google`** in `AuthController`:
  - Body `GoogleSignInRequest(string IdToken)`.
  - Sends `GoogleSignInCommand`, then `RefreshTokenCookie.Append(...)`, returns
    `AuthSessionResponse` — identical tail to the `Login` endpoint. Same call handles
    register **and** login (the handler decides new-vs-existing).
- Expose the Google **ClientId** to the frontend via build-time env var
  `VITE_GOOGLE_CLIENT_ID` (consistent with how Turnstile config is handled), not a runtime endpoint.

---

## 5. Username auto-generation

Small helper (`IUsernameGenerator` or inline in the handler):
- Seed from email local-part (fallback: Google `name`), lowercase, strip to `[a-z0-9_]`.
- Ensure it **starts with a letter** (prefix a letter if not); clamp to 3–30 chars;
  pad if shorter than 3; fallback seed (e.g. `reader`) if empty after sanitizing.
- Loop: `GetByUsernameAsync`; on collision append a short numeric/random suffix until
  unique (cap attempts).
- Unit-test sanitization + collision suffixing.

---

## 6. Frontend (`web/legi-web`)

- Add `@react-oauth/google` (or load the GIS script directly). Wrap the app (or auth
  pages) in `GoogleOAuthProvider` with `VITE_GOOGLE_CLIENT_ID`.
- **`LoginPage.tsx` + `RegisterPage.tsx`:** Google button under the form with an "or"
  divider — same UI on both, since the backend endpoint is shared. On the credential
  callback, POST the ID token via `authApi.googleSignIn(idToken)`.
- **`api.ts`:** `googleSignIn: (idToken) => http.post<AuthResponse>("/identity/auth/google", { idToken }).then(r => r.data)`.
- **`AuthContext`:** add `loginWithGoogle(idToken)` that persists the `AuthResponse` exactly like `login`.
- **i18n:** add `auth.continueWithGoogle`, `auth.orDivider` to all locale files.

---

## 7. Config / ops

- Create a Google Cloud OAuth **Web** client ID; authorized JS origins:
  `http://localhost:3000` + prod domain.
- `.env` / `.env.example`: `GoogleAuth__ClientId` (backend), `VITE_GOOGLE_CLIENT_ID` (frontend).
- Document both in CLAUDE.md.

---

## 8. Optional / future (not v1)

- **Nonce** (client-generated, server-verified) for ID-token replay hardening — small
  window since tokens are short-lived; TODO.
- **Avatar from Google `picture`** — carry it in `GoogleUserInfo` now; wire to the
  existing Social MinIO avatar storage later.
- **"Link Google" from settings** for already-logged-in password users (the auto-link
  branch already covers the sign-in case).

---

## Suggested sequencing

1. Domain (`ExternalLogin`, nullable password, factory, confirm-on-link) + Domain tests.
2. Infrastructure (validator, settings, config, migration).
3. Application (command/handler, username generator, repo method) + tests.
4. API endpoint.
5. Frontend button + wiring.
6. Manual end-to-end with a real Google client ID.

## Risk notes

- **Nullable `PasswordHash`** is the widest blast radius. Confirmed sole sensitive
  read site is `LoginCommandHandler.cs:52` (needs the null guard); `User.Create`
  (password path) still requires a password.
- `UserRegisteredDomainEvent` / integration event are **unchanged**, so Social and
  other contexts need no changes — the auto-generated username flows through as the snapshot.
- Email-confirmation, password-reset, and account-deletion flows all no-op gracefully
  for passwordless users (verified — no changes needed).
</content>
</invoke>
