# BukiHub — Threat Model

A pragmatic, asset-oriented threat model for the BukiHub deployment. It records what
we protect, who we protect it from, the controls in place, and the risks we knowingly
accept. It is a companion to `DEPLOYMENT-hardening.md` (which has the operational
detail) and should be revisited whenever a trust boundary or auth flow changes.

Last reviewed: 2026-06-30.

## 1. System overview & trust boundaries

A single VM runs the whole stack via `docker-compose.prod.yml`: four .NET bounded
contexts (Identity, Catalog, Library, Social), four Postgres databases, RabbitMQ,
MinIO, and an nginx web tier. A host TLS proxy (Caddy) terminates HTTPS; Cloudflare
fronts the origin.

```
Internet ──TLS──> Cloudflare ──TLS──> Caddy (host) ──> nginx (web, 127.0.0.1) ──┐
                                                                                 │ internal docker network (no published infra ports)
   ┌─────────────────────────────────────────────────────────────────────────┘
   ├─> identity-api ──> identity-db        (mints JWTs; holds the only private key)
   ├─> catalog-api  ──> catalog-db
   ├─> library-api  ──> library-db
   ├─> social-api   ──> social-db
   ├─> RabbitMQ     (outbox/inbox integration events)
   └─> MinIO        (avatars, banners, covers)
```

**Trust boundaries** (where data crosses a privilege change — the places to scrutinise):
- **B1 Internet → edge/origin.** Untrusted clients. Mitigated by Cloudflare (WAF, DDoS,
  edge rate limit), origin firewall allowing only Cloudflare IPs, host TLS.
- **B2 Browser → API (authn/authz).** Every request claiming a user identity. Mitigated
  by JWT RS256 validation, per-handler ownership (IDOR) checks, rate limiting, Turnstile.
- **B3 Service → service (internal network).** APIs, DBs, broker, object store share a
  private docker network with no host-published ports. Mitigated by least-privilege DB
  roles, TLS to Postgres, per-service secrets, RabbitMQ auth.
- **B4 Host → containers.** A container compromise must not become host/root. Mitigated
  by non-root containers, `cap_drop: ALL`, `no-new-privileges`, read-only rootfs.
- **B5 Operator → secrets.** Whoever holds `.env.prod` + `./secrets` holds everything.

## 2. Assets (what an attacker wants)

| Asset | Why it matters |
|---|---|
| User credentials & sessions | Account takeover; password reuse across sites |
| JWT **private** signing key | Forge tokens for ANY user → total authn bypass |
| MFA encryption key | Decrypt every TOTP secret at rest |
| Personal data (email, reading activity, profiles) | Privacy harm, GDPR exposure |
| Databases (4×) | Bulk PII exfiltration, data tampering |
| MinIO objects | User-uploaded images; defacement/abuse hosting |
| The host / VM | Pivot, cryptomining, full data access |
| Backups + secret files | Offline copy of everything |

## 3. Threat actors

- **Opportunistic internet attacker** — scanners, credential stuffing, bots. (Primary.)
- **Malicious authenticated user** — abuses the API to reach other users' data (IDOR),
  spam, or upload abuse.
- **Network adversary on the host** — has some foothold and tries to move laterally
  between containers or sniff internal traffic.
- **Lost-laptop / leaked-secret** — an operator credential or `.env.prod` leaks.
- *Out of scope:* nation-state, physical access to the datacenter, malicious Docker/OS
  supply chain, a compromised Cloudflare.

## 4. Threats & mitigations

### B2 — Authentication & session (the most-attacked surface)
- **Credential stuffing / brute force** → per-IP rate limiting (AspNetCoreRateLimit on
  the real client IP via forwarded-headers), login-attempt lockout, Turnstile after N
  failures, and HIBP breached-password rejection on register/reset.
- **Token forgery** → access tokens are **RS256**; only `identity-api` has the private
  key, and resource APIs validate with the public key + `ValidAlgorithms=[RS256]`
  (blocks `alg=none`/HS-confusion). Short 15-min access tokens bound the blast radius.
- **Stolen/replayed refresh token** → refresh tokens are opaque, rotated on use, revoked
  on logout/password-change, capped at 5 per user; the cookie is `HttpOnly` + `Secure` +
  `SameSite=Strict`. Access token lives only in memory (no localStorage → XSS can't read it).
- **MFA bypass** → second factor (TOTP or email code) enforced post-password; the login
  challenge token uses a **distinct audience** (`<aud>:mfa`) so it can't be replayed as
  an access token. TOTP secrets encrypted at rest (AES-256-GCM).
- **Account-enumeration / reset abuse** → generic responses; reset tokens are
  time-limited; Turnstile gates reset.

### B2 — Authorization (IDOR)
- **Accessing another user's resource** → ownership is checked in the command/query
  handlers (not just the route); cross-context ownership uses the `ContentSnapshot`
  OwnerId. Catalog management is role-gated.

### B1/B2 — Input & content
- **XSS** → strict CSP (no inline scripts beyond the allowlisted Turnstile/GIS origins),
  `X-Content-Type-Options`, `Referrer-Policy`, framing denied. React escapes by default.
- **SQL injection** → EF Core parameterises everything; no string-built SQL.
- **Malicious uploads** → images are re-encoded on upload (strips polyglots/EXIF), size-
  capped (`client_max_body_size`), served from a separate origin path.
- **DoS** → Cloudflare absorption + edge rate limit, per-container CPU/memory limits.

### B3 — Internal network
- **Compromised non-Identity service forging tokens** → impossible: it never holds the
  private key.
- **A compromised API reaching another database / escalating in Postgres** → each API
  connects as a NOSUPERUSER role scoped to its own DB; cannot reach other DBs, create
  roles, read server files, or `COPY ... PROGRAM`.
- **Passive sniffing of DB traffic** → `SSL Mode=Require` (TLS) between APIs and Postgres.
- **Unauthorized broker/object access** → RabbitMQ and MinIO require credentials; no
  infra port is published to the host.

### B4 — Container → host
- **Container escape / privilege escalation** → containers run **non-root** (APIs +
  unprivileged nginx), `cap_drop: ALL`, `no-new-privileges`, read-only rootfs where
  possible. No container is published on `0.0.0.0` (web is loopback-only behind Caddy).

### B5 — Secrets & data at rest
- **Secret exposure via `docker inspect`/`/proc`** → secrets are file-based Docker
  secrets under `/run/secrets`, not env vars.
- **Data loss / instance loss** → nightly `pg_dump` + MinIO mirror (`scripts/backup.sh`),
  shipped off-box, with a tested restore.

## 5. Residual & accepted risks

- **Secrets are plaintext files on the VM.** Invisible to `docker inspect`, but a host
  compromise reads them. *Accepted for launch; graduate to Vault/cloud KMS (B5).*
- **No access-token revocation list.** A stolen access token is valid until it expires
  (≤15 min). *Accepted — refresh revocation + short TTL bound it.*
- **Self-signed Postgres TLS, not chain-validated** (`Trust Server Certificate=true`):
  protects against sniffing, not an active in-network MITM. *Accepted on a single host;
  upgrade to `VerifyFull` + a CA if the DB ever leaves the box.*
- **HIBP / Turnstile fail open** on outage so sign-up isn't blocked — a deliberate
  availability-over-strictness trade.
- **Single VM = single point of failure.** No HA. *Accepted for launch; backups cover
  recovery, the stack is portable to a new box.*
- **MinIO objects are public-read** (download). Acceptable for avatars/covers; do not
  store anything private there.

## 6. Maintenance

Re-review this document when: a new auth flow or second factor is added; a new
externally-reachable endpoint or upload path appears; the trust boundaries change
(e.g. splitting services across hosts, adding a managed DB); or a dependency with a
known-exploited CVE is flagged by the CI security pipeline.
