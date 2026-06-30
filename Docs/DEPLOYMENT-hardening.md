# BukiHub — Production Deployment Hardening

This document describes the secure production deployment posture and the steps
required to go live safely. It accompanies `docker-compose.prod.yml` and
`.env.prod.example`.

> **`docker-compose.yml` is for local development only.** It runs services in
> `Development` mode with default credentials and exposed infrastructure ports.
> Never deploy it to a public host. Use `docker-compose.prod.yml`.

---

## 1. What the production compose changes vs. dev

| Concern                     | Dev compose                      | Prod compose                                                                         |
| --------------------------- | -------------------------------- | ------------------------------------------------------------------------------------ |
| ASP.NET environment         | `Development`                    | **`Production`** → HTTPS redirect on, Swagger off, refresh cookie `Secure` flag set  |
| DB / RabbitMQ / MinIO ports | published to host                | **not published** — internal Docker network only                                     |
| Credentials                 | hardcoded defaults               | **file-based Docker secrets** (`./secrets`, not env vars); aborts if any is missing                          |
| API containers              | root, full caps                  | **non-root**, `cap_drop: ALL`, `no-new-privileges`, read-only rootfs, CPU/mem limits |
| Public surface              | every API + UI on all interfaces | **only the web tier**, bound to `127.0.0.1`                                          |

## 2. Deploy

```bash
cp .env.prod.example .env.prod
# Fill the NON-SECRET config (hostnames, DB/bucket names, URLs, public site keys).
$EDITOR .env.prod

# Generate every SECRET as a file-based Docker secret under ./secrets (see §6).
# Auto-generates the random ones; leaves placeholders for Turnstile/SMTP.
./scripts/gen-prod-secrets.sh
printf '%s' 'your-cloudflare-turnstile-secret' > secrets/Turnstile__SecretKey
printf '%s' 'your-smtp-password'               > secrets/Smtp__Password
chmod 644 secrets/*    # readable by the non-root API user (see note below)

# Generate the self-signed cert that encrypts API↔Postgres traffic (see §6).
./db/tls/gen-db-certs.sh

docker compose --env-file .env.prod -f docker-compose.prod.yml up -d --build
```

> Always write secret files with `printf '%s'` (no trailing newline). `AddKeyPerFile`
> does not trim, so a stray `\n` corrupts base64 keys and connection strings.
>
> Secret files must be **world-readable (0644)** — docker-compose (non-Swarm)
> bind-mounts them preserving the host mode/owner, and the API containers run as a
> non-root user whose uid doesn't match, so a `0600` file is unreadable. The
> `./secrets` directory is `0700`, so this stays safe on the host.

The stack **fails closed**: the secret _files_ are required — a missing
`./secrets/Jwt__PrivateKey`, `Mfa__EncryptionKey`, DB password, RabbitMQ or MinIO
credential aborts `docker compose up` instead of falling back to a dev default.
Non-secret required config still uses `${VAR:?}` in the compose file.
`Mfa__EncryptionKey` and `Jwt__PrivateKey` are mounted into **identity-api only**.

## 3. TLS termination (required)

The web container publishes only on `127.0.0.1:${WEB_HTTP_PORT}` (default 8080).
Put a TLS-terminating reverse proxy on the host in front of it. Example (Caddy):

```
yourdomain.com {
    reverse_proxy 127.0.0.1:8080
}
```

Caddy auto-provisions Let's Encrypt certificates. Traefik or host nginx work too.
**Do not** expose the web container directly on `0.0.0.0:80` — the refresh-token
cookie and access tokens would travel in cleartext.

## 4. Forwarded headers (implemented — tune if needed)

The APIs run behind nginx (and your host TLS proxy), so the TCP peer they see is
the proxy, not the user. Each API's `Program.cs` now calls `UseForwardedHeaders`
as the first middleware, honoring `X-Forwarded-For`/`X-Forwarded-Proto` so that:

- IP rate limiting, login-attempt lockout and Turnstile key off the **real client IP**, and
- HTTPS is detected correctly behind the TLS proxy (no redirect loops).

To resist IP spoofing it trusts forwarded headers **only** from the private proxy
networks (defaults: `10.0.0.0/8`, `172.16.0.0/12`, `192.168.0.0/16`) and bounds
the hop count (`ForwardLimit`, default 2 = host proxy → nginx). A client cannot
reach the APIs directly (their ports aren't published), so injected `X-Forwarded-For`
entries sit to the left of the trusted hops and are ignored.

**Tuning** (only if your Docker bridge subnet differs from the RFC1918 defaults, or
you add another proxy hop), via env vars — no rebuild needed:

```
ForwardedHeaders__KnownNetworks__0=172.18.0.0/16
ForwardedHeaders__ForwardLimit=2
```

> Do **not** also set `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` — it registers a
> second forwarded-headers middleware that would double-strip `X-Forwarded-For`.

**Verify after deploy:** make a failed login through the public URL and confirm the
rate-limit/lockout counter keys off your real IP (not one shared bucket).

## 5. JWT signing keys (RS256)

Access tokens are signed with RSA (RS256), not a shared secret. **Only Identity
holds the private key and can mint tokens**; the other services get only the public
key — so a compromise of Catalog/Library/Social cannot be turned into forged
user tokens. Generate a keypair and base64-encode each PEM (single-line, env-friendly):

```bash
openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:2048 -out jwt-private.pem
openssl rsa -in jwt-private.pem -pubout -out jwt-public.pem
base64 -w0 jwt-public.pem    # -> Jwt__PublicKey  (every service)
base64 -w0 jwt-private.pem   # -> Jwt__PrivateKey (identity-api ONLY)
```

Put both in `.env.prod`. The prod compose injects `Jwt__PublicKey` into every API
(shared anchor) but `Jwt__PrivateKey` only into `identity-api` — keep the private
key out of every other service and out of the frontend. To rotate, issue a new
keypair: existing access tokens expire within `Jwt__AccessTokenExpirationMinutes`
(15), and refresh tokens are opaque so they survive the change. For local dev, the
`Jwt__PublicKey`/`Jwt__PrivateKey` fields in `.env.example` are intentionally **empty**
— run `./scripts/gen-dev-keys.sh` to generate a throwaway keypair into your gitignored
`.env`. No private key is ever committed.

## 6. Recommended follow-ups (defense in depth)

- **Least-privilege DB roles — implemented.** Each API connects as a non-superuser
  role that owns the `public` schema **and** holds `CREATE` on its own database (so
  EF `Migrate()` can create the `identity` migrations-history schema and run
  migrations) yet cannot reach other databases, create roles, or run
  `COPY ... PROGRAM`. A bootstrap
  superuser (`*_DB_ADMIN_*`, never used by the app) creates that role via
  `db/init/create-app-role.sh`, which Postgres runs **once, on first init of an
  empty data volume**. For an EXISTING deployment the script will not re-run — run
  its SQL manually against each DB. Dev (`docker-compose.yml`) intentionally stays
  on the `postgres` superuser for local convenience.
- **Security-event audit log — implemented.** Logins (success/failure/lockout),
  password resets and account deletions are emitted as structured logs under a stable
  per-event `EventId` (1000-range). Ship container logs to a sink (Loki, CloudWatch,
  etc.) and alert on spikes in `LoginFailed`/`LoginBlockedLockout`. These records
  contain PII (attempted identifier, IP) for forensics — set a retention policy
  consistent with your privacy notice. The `ISecurityAuditLogger` abstraction can be
  swapped for a database-backed, queryable store later without touching call sites.
- **Breached-password check — implemented.** Registration and password reset reject
  passwords found in the Have I Been Pwned corpus via k-anonymity (only a SHA-1
  prefix is sent; never the password). It makes an outbound HTTPS call to
  `api.pwnedpasswords.com` and **fails open** on any error, so egress restrictions
  or an HIBP outage never block sign-up. Disable with `BreachedPassword__Enabled=false`.
- **MFA (TOTP) — implemented.** Users can enrol an authenticator app (RFC 6238 TOTP)
  and get one-time recovery codes; login then requires a second factor. The login
  endpoint returns a short-lived **challenge token** (a distinct JWT audience,
  `<Audience>:mfa`, so it is rejected anywhere an access token is expected) that is
  exchanged at `/auth/mfa-login` for the real session. Authenticator secrets are
  encrypted **at rest** with AES-256-GCM under `Mfa__EncryptionKey` (a base64-encoded
  32-byte key, identity-api only): `openssl rand -base64 32`. The key is **required**
  in prod — without it, identity-api fails to start. Rotating it invalidates existing
  TOTP enrolments (users must re-enrol), so treat it like the JWT private key: generate
  once, back it up encrypted, do not rotate casually. Enrol/confirm/disable and the
  second-factor login are rate-limited (see `IpRateLimiting` in identity appsettings).
- **TLS to Postgres — implemented.** Inter-container DB traffic is encrypted: each
  prod `*-db` runs with `ssl=on` and every API connects with
  `SSL Mode=Require;Trust Server Certificate=true`, so a passive sniffer on the
  Docker bridge sees only ciphertext. Generate the (self-signed, shared) certificate
  once before deploy: `./db/tls/gen-db-certs.sh` — it writes the gitignored
  `db/tls/server.{crt,key}` that the compose mounts into all four databases. A small
  entrypoint wrapper (`db/tls/pg-tls-entrypoint.sh`) installs the key with the
  owner/permission Postgres requires (the raw bind-mount is owned by the host UID,
  which Postgres rejects). The cert is **not** chain-validated
  (`Trust Server Certificate=true`): the goal is confidentiality against sniffing,
  not server-identity proof — every DB is on the same host behind an internal-only
  network, so an active MITM is out of scope. To rotate:
  `./db/tls/gen-db-certs.sh --force` then recreate the db containers. Want a stronger
  guarantee? Distribute the cert as a CA to the APIs and switch the clients to
  `VerifyFull`, and/or force `hostssl` in `pg_hba.conf` to reject any cleartext
  connection server-side. Consider **encryption at rest** for the data volumes too.
- **Docker secrets — implemented.** Secrets are no longer environment variables
  (which leak via `docker inspect` and `/proc/<pid>/environ`). Every sensitive
  value — the JWT keys, MFA key, all DB/RabbitMQ/MinIO passwords, the API
  connection strings, and the Turnstile/SMTP secrets — is a file-based Docker
  secret under `./secrets`, mounted read-only into `/run/secrets`. Postgres and
  MinIO read theirs via `*_FILE` env conventions; RabbitMQ 4.x removed
  `RABBITMQ_DEFAULT_PASS_FILE`, so a small wrapper (`rabbitmq/secret-entrypoint.sh`)
  reads the secret into the plain env var instead; the four .NET APIs read theirs
  via `AddKeyPerFile("/run/secrets")` (a file named `Jwt__PublicKey` becomes config
  key `Jwt:PublicKey`). Generate the whole set with **`./scripts/gen-prod-secrets.sh`**
  — it auto-generates the secrets we own (RS256 keypair, MFA key, every DB
  password, RabbitMQ, MinIO) and composes the connection strings; it leaves
  placeholder files for the externally-issued ones (`Turnstile__SecretKey`,
  `Smtp__Password`) to fill in. It is idempotent (never rotates a live DB password);
  pass `--force` only for a fresh deploy on empty volumes. The files are `0644`
  (the API containers run non-root and must read them) inside a `0700` directory,
  and `./secrets` is gitignored. **Limitation:** on a single VM these files still
  sit on the same host disk — invisible to `docker inspect`, but *not*
  host-compromise protection. For that, graduate to **Vault or your cloud's KMS**
  (e.g. AWS Secrets Manager) once a deploy target is chosen.
- **Backups — scripted (`./scripts/backup.sh` / `./scripts/restore.sh`).** See §6.4;
  the local round-trip restore is verified. Still **schedule it off-box** and back up
  the secrets separately.
- **Edge protection / WAF** (e.g. Cloudflare) in front of the host proxy for
  DDoS absorption and a second rate-limiting layer.
- **Tighten DB/broker capabilities** further once a known-good cap set is verified.
- **Unprivileged nginx web tier — implemented.** The web container uses
  `nginxinc/nginx-unprivileged` (runs as uid 101, never root) and gets the same
  lockdown as the APIs: `cap_drop: ALL`, `no-new-privileges`, read-only rootfs with a
  single tmpfs at `/tmp` (the image puts the pid and all temp paths there).
  nginx listens on **8080** (a non-root process can't bind <1024); the host TLS proxy
  and the compose port mapping front it. No container in the prod stack now runs as root.

## 7. Single-VM host hardening (Oracle Cloud A1 / any VPS)

When the whole stack runs on one box, **the VM is your security perimeter**. The
compose file keeps databases/RabbitMQ/MinIO off the host, but the host itself must
be locked down. Cheapest viable launch target: **Oracle Cloud Always Free Ampere
A1** (4 ARM cores / 24 GB RAM — fits the whole stack; the multi-arch .NET/Postgres/
RabbitMQ/MinIO images build natively on ARM, no code changes). Same steps apply to
any VPS (Lightsail, Hetzner, DigitalOcean).

### 6.1 Network: two firewalls + Cloudflare

Put **Cloudflare (free)** in front of the origin — it provides TLS, DDoS
protection, a WAF, and edge rate limiting at no cost, and pairs with the Turnstile
you already use. Set DNS records to **Proxied** (orange cloud) and SSL/TLS mode to
**Full (strict)**.

Then lock the origin so attackers can't bypass Cloudflare by hitting the raw IP:

- **Cloud firewall** (Oracle _Security Lists_ / Lightsail firewall / cloud SG):
  allow inbound **443** and **80** _only from Cloudflare's published IP ranges_;
  allow **22** only from your IP (or close it entirely and use a bastion / Tailscale).
  Everything else: denied.
- **Host firewall** as defence-in-depth (`ufw` on Ubuntu):

    ```bash
    sudo ufw default deny incoming
    sudo ufw default allow outgoing
    sudo ufw allow 22/tcp           # restrict to your IP if you can
    sudo ufw allow 80,443/tcp
    sudo ufw enable
    ```

    > **Docker + ufw gotcha:** Docker writes iptables rules directly and a published
    > port can bypass `ufw`. The prod compose avoids this by binding web to
    > `127.0.0.1:8080` (not `0.0.0.0`), so Caddy on the host — not Docker — is what
    > faces the network. Keep it that way; never publish a container on `0.0.0.0`.

### 6.2 SSH

```bash
# /etc/ssh/sshd_config.d/hardening.conf
PasswordAuthentication no
PermitRootLogin no
KbdInteractiveAuthentication no
```

Key-based auth only, log in as a non-root sudo user, then `sudo systemctl reload ssh`.
Add **fail2ban** (`sudo apt install fail2ban`) to throttle SSH brute force, and
**unattended-upgrades** for automatic security patches:

```bash
sudo apt install unattended-upgrades && sudo dpkg-reconfigure -plow unattended-upgrades
```

### 6.3 Origin TLS (Caddy)

Caddy fronts the loopback-bound web container and auto-manages certificates:

```
yourdomain.com {
    reverse_proxy 127.0.0.1:8080
}
```

With Cloudflare in **Full (strict)** mode, use a Cloudflare Origin certificate (or
let Caddy obtain a Let's Encrypt cert via DNS-01). Caddy sends `X-Forwarded-Proto`,
which the APIs now honor (section 4).

### 6.4 Backups (non-negotiable on a free VM)

A free Always-Free VM can be **reclaimed by Oracle if idle**, and there is no
managed backup. Protect against both data loss and instance loss.

**`./scripts/backup.sh`** captures everything into one timestamped directory under
`./backups`: a `pg_dump -Fc` (compressed custom format) of all four databases plus an
`mc mirror` of the MinIO buckets. It targets the fixed container names (so it works
for both `docker-compose.prod.yml` and the prod-local stack) and dumps over Postgres'
local trust socket (no password). `BACKUP_RETENTION_DAYS` (default 14) prunes old runs.

```bash
./scripts/backup.sh                       # -> ./backups/<timestamp>/
```

**Schedule it (systemd timer).** The repo ships `deploy/legi-backup.{service,timer}`
(nightly at 03:00, `Persistent=true` so a missed run after downtime still fires):

```bash
sudo cp deploy/legi-backup.{service,timer} /etc/systemd/system/
# edit User= and the /opt/legi paths in the .service to match your install
sudo systemctl daemon-reload
sudo systemctl enable --now legi-backup.timer
systemctl list-timers legi-backup.timer     # confirm next run
journalctl -u legi-backup.service            # see results
```

> Until you install that timer, **backups are not generated automatically** — the
> script only runs on demand.

- **Ship it off-box** (object storage / another host) — a backup on the same VM does
  not survive instance loss. Set `BACKUP_UPLOAD_CMD` (the timer reads it from an
  optional `/etc/legi/backup.env`) so each run uploads itself, e.g.:
  ```bash
  # /etc/legi/backup.env
  BACKUP_RETENTION_DAYS=14
  BACKUP_UPLOAD_CMD='rclone copy "$BACKUP_DIR" remote:legi-backups/$(basename "$BACKUP_DIR")'
  ```
- **Restore** with `./scripts/restore.sh <backup-dir> [--yes]`: it stops the APIs,
  `pg_restore --clean`s each database (ownership preserved → the app role still owns
  its schema), mirrors the buckets back, and restarts the APIs. **DESTRUCTIVE** — it
  overwrites current data. The DB containers must be running first (their app roles
  are created on init; the dumps contain no roles).
- **Test the restore** before launch — an untested backup is not a backup. The local
  round-trip (wipe a table → `restore.sh` → data recovered, APIs healthy) is verified;
  rehearse it on a throwaway box for full instance-loss recovery.
- Keep `.env.prod`, `./secrets` and `./db/tls` backed up **separately and encrypted** —
  they are NOT in the data backup, yet a restored DB is unusable without them (and the
  MFA/JWT keys decrypt user data). Store them apart; leaking them compromises everything.

### 6.5 Stay portable (AWS-ready)

Everything above is vendor-neutral. Do **not** adopt OCI-managed databases or
proprietary services — keep state in the Docker volumes driven by
`docker-compose.prod.yml`. Migrating to Lightsail/EC2/Hetzner is then: provision a
box, copy the compose + `.env.prod`, restore the DB dumps, repoint Cloudflare DNS.

## 8. Pre-launch checklist

- [ ] `.env.prod` filled (non-secret config); no `CHANGE_ME` left
- [ ] `./scripts/gen-prod-secrets.sh` run; no `REPLACE_ME_*` left in `./secrets` (Turnstile/SMTP filled, no trailing newline); files are `0644`
- [ ] Secrets are files, not env vars: `docker inspect legi-identity-api | grep -i privatekey` returns nothing
- [ ] JWT RS256 keypair in `./secrets`; `Jwt__PrivateKey` mounted into identity-api ONLY
- [ ] `Mfa__EncryptionKey` in `./secrets`, mounted into identity-api; backed up encrypted
- [ ] `docker compose --env-file .env.prod -f docker-compose.prod.yml config` succeeds
- [ ] Cloudflare proxying domain; SSL/TLS = Full (strict)
- [ ] Origin firewall allows 443/80 only from Cloudflare IPs; SSH restricted
- [ ] SSH: password auth off, root login off, key-only; fail2ban + auto-updates on
- [ ] Host TLS (Caddy) serving HTTPS; HTTP→HTTPS redirect verified
- [ ] No infra ports reachable from outside the host (`nmap` the public IP)
- [ ] DB runtime role is non-superuser (`\du` shows no Superuser on `*_app`)
- [ ] Postgres TLS cert generated (`./db/tls/gen-db-certs.sh`); a DB session over TCP shows SSL (`SELECT ssl FROM pg_stat_ssl WHERE pid = pg_backend_pid();`)
- [ ] No container published on `0.0.0.0` (web is `127.0.0.1` only)
- [ ] Forwarded headers verified: rate limiting throttles by real client IP
- [ ] Turnstile enabled with production keys; login lockout verified
- [ ] SMTP working; SPF/DKIM/DMARC set on the sending domain
- [ ] Nightly DB + MinIO backups running off-box; **restore tested**
- [ ] `.env.prod` **and `./secrets`** backed up separately and encrypted (they hold every secret)
- [ ] `Jwt__AccessTokenExpirationMinutes` kept short (15)
