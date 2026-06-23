# Identity Feature — Email-code MFA (second method)

**Status:** ✅ Implemented 2026-06-22 (design first parked, then built the same day). Backend + frontend + tests green; EF migration `AddEmailMfa` generated (apply to running stacks via rebuild). One deliberate limitation: an email-method user **disables** MFA with a **recovery code** (no TOTP secret to verify, and we didn't add a separate disable-code send flow) — see §7.

Add **email one-time codes** as a second two-factor method, alongside the
already-shipped **TOTP** (authenticator app). A user picks *one* method at
enrolment; recovery codes stay universal. This document captures the design so it
can be picked up later — it is intentionally additive and touches **none** of the
JWT / challenge-token layer.

> See also `Docs/DEPLOYMENT-hardening.md` §6 (MFA — implemented, TOTP) and
> `Docs/IDENTITY-FEATURE-google-signin.md` for the doc style.

---

## 1. Why (and why it's parked)

Email codes are the **weakest mainstream second factor**: the code lands in the
same inbox that already controls password reset, so an attacker who owns the email
owns both factors at once (*channel collapse*). For Google-sign-in users it's
near-worthless — same Gmail, same password. TOTP keeps an independent
device-bound secret and does not have this weakness.

**But** against the dominant consumer threat — credential stuffing / password reuse
— email codes are about as effective as TOTP (attacker has the password, not the
inbox), and they need **zero setup**, which lifts adoption from "almost nobody" to
"some people." For a book social app that's the threat that matters most.

Decision: **do not replace TOTP. Keep it as the strong option; offer email as a
lower-friction alternative.** Parked because expected adoption is low — revisit if
enrolment data shows users bouncing off the authenticator-app flow. Recovery codes
already cover the "lost my authenticator" case, so there is no urgent gap.

This is a **weaker** factor by construction; the design keeps that explicit and
contained (see the `MfaMethod` enum) rather than blurring it into the TOTP path.

## 2. Model — one method *per user*

A user is `None`, `Totp`, **or** `Email` — not both. Most users are `None`.

**Domain (`User`)** — add an explicit discriminator:

```csharp
public enum MfaMethod { None = 0, Totp = 1, Email = 2 }
public MfaMethod MfaMethod { get; private set; }   // None when MfaEnabled == false
```

- `MfaEnabled` (existing bool) stays the gate, so every existing
  `if (user.MfaEnabled)` check is untouched.
- For an Email-method user, `TotpSecret` is `null` — **no AES, no `Mfa__EncryptionKey`,
  no `TotpService`** on this path. (Email MFA needs zero crypto material — a genuine
  simplicity win over TOTP. It only needs SMTP, which prod already requires.)
- `DisableMfa()` already wipes state; it additionally resets `MfaMethod = None` and
  clears any pending email code.

> The method *could* be inferred from `TotpSecret == null`, but an explicit enum is
> clearer and leaves room for SMS/passkey later. Cheap.

New domain method `EnableEmailMfa(recoveryCodeHashes, utcNow)` — parallels the
existing `ConfirmMfaEnrollment` minus the `TotpSecret` requirement.

## 3. New transient store — `mfa_email_codes`

Modelled like the existing `LoginAttempt` (transient security state, **own table +
repo**, *not* loaded with the `User` aggregate — these codes churn):

```
mfa_email_codes:  UserId, CodeHash, ExpiresAt, AttemptCount, ConsumedAt
```

- **Hash the code** via the existing `ISecureTokenFactory.Hash` (same as recovery
  codes / refresh tokens) — never store plaintext.
- **One active code per user** — issuing a new one supersedes the old.
- Prune on `ExpiresAt` (filter on read + an occasional cleanup sweep).

`IMfaEmailCodeRepository`:
`IssueAsync(userId, codeHash, expiresAt)`, `GetActiveAsync(userId)`,
`IncrementAttemptAsync(userId)`, `ConsumeAsync(userId)`.

## 4. Flow A — enrolment (mirrors TOTP setup, simpler)

The user's email is already verified at registration (`IsEmailConfirmed`), so
enrolment just re-proves inbox control.

| Endpoint | Does |
|---|---|
| `POST /api/v1/identity/mfa/email/setup`   | Generate a 6-digit code, store its hash, send via the **existing SMTP service** (the one password-reset uses). No QR, no secret. |
| `POST /api/v1/identity/mfa/email/confirm` | Verify the code → `user.EnableEmailMfa(...)` (sets `MfaEnabled`, `MfaMethod = Email`) → returns the **same recovery-codes** payload the TOTP flow already produces. |

## 5. Flow B — login (reuses `mfa-login` almost verbatim)

The only real addition is a **send step** — unlike TOTP, the user doesn't already
hold the code.

1. `POST /auth/login` → password passes, `MfaEnabled` → returns
   `MfaRequired: true`, `MfaToken`, **and now `MfaMethod`** (one field added to
   `LoginResponse`) so the frontend knows what to render.
2. **New:** `POST /auth/mfa-email/send` — takes the challenge token, issues + emails
   a code. Kept separate from login (not folded in) because:
   - **resend** is needed anyway (emails get lost / delayed), and
   - it keeps login fast and independent of SMTP latency.
   The challenge token gates it (must have passed factor one).
3. `POST /auth/mfa-login` — **unchanged endpoint and request shape** (token + code).
   `CompleteMfaLoginCommandHandler` gains a branch on `user.MfaMethod`:

   ```csharp
   var (verified, usedRecovery) = user.MfaMethod switch
   {
       MfaMethod.Totp  => VerifyTotpOrRecoveryCode(user, code, now),
       MfaMethod.Email => await VerifyEmailCodeOrRecoveryCode(user, code, now),
       _               => (false, false)
   };
   ```

   Recovery codes remain the universal fallback in both branches. Token issuance
   below the branch is untouched.

## 6. Security controls (load-bearing — do not skip)

A 6-digit code is a 1,000,000 space; it is only safe with strict limits. These are
what make email codes acceptable:

- **Expiry:** 5–10 min on the stored code.
- **Attempt cap:** invalidate after ~5 wrong tries (forces a resend, not infinite
  guessing). *This*, not code length, is what stops brute force.
- **Resend throttle:** rate-limit `mfa-email/send` (e.g. 1 / 30s per user) **and**
  add it to `IpRateLimiting` `GeneralRules` next to the existing `mfa-login` entry.
- **Single-use:** set `ConsumedAt` on success; reject consumed/expired codes.
- **No enumeration:** `mfa-email/send` and `mfa-login` return the same generic
  failure regardless of cause — matching what `CompleteMfaLogin` already does
  (`"Invalid or expired MFA challenge."`).

## 7. The rest (small)

- **Frontend:** `MfaSection.tsx` gains a method choice (Authenticator app / Email).
  Login page: when `MfaMethod === Email`, auto-call `mfa-email/send` and show
  "we emailed you a code" + a resend button instead of the TOTP prompt; reuse the
  existing code-entry field. i18n keys under the existing `auth.mfa*` / `settings.mfa*`
  namespaces.
- **Audit:** reuse `MfaEnabled` / `MfaDisabled` / `MfaChallengeFailed` /
  `RecoveryCodeUsed`, tagging `Detail: "email"` vs `"totp"`. Optionally add one
  `MfaEmailCodeSent` EventId to alert on send-spam.
- **Config:** no new secret. Needs only SMTP (already required in prod).

## 8. Effort & scope

- 1 migration: `MfaMethod` column on `users` + `mfa_email_codes` table.
- Domain: 1 method (`EnableEmailMfa`) + the `MfaMethod` enum; extend `DisableMfa`.
- 1 repository (`IMfaEmailCodeRepository`) + EF config.
- ~4 thin command handlers: email setup, email confirm, email-code send, plus the
  verify branch in `CompleteMfaLogin`.
- API: 3 endpoints (`mfa/email/setup`, `mfa/email/confirm`, `auth/mfa-email/send`);
  add `MfaMethod` to `LoginResponse`.
- Frontend: ~3 touches (settings method picker, login render branch, i18n).
- **No changes to the JWT / challenge-token layer.**

**Leaner-for-launch option:** ship the verify/send plumbing but skip the email-method
enrolment UI, wiring email only as a fallback later. Not recommended — without
enrolment a user can't *choose* email, which is the whole point.
