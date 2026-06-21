# BukiHub — Production Deployment Hardening

This document describes the secure production deployment posture and the steps
required to go live safely. It accompanies `docker-compose.prod.yml` and
`.env.prod.example`.

> **`docker-compose.yml` is for local development only.** It runs services in
> `Development` mode with default credentials and exposed infrastructure ports.
> Never deploy it to a public host. Use `docker-compose.prod.yml`.

---

## 1. What the production compose changes vs. dev

| Concern | Dev compose | Prod compose |
|---|---|---|
| ASP.NET environment | `Development` | **`Production`** → HTTPS redirect on, Swagger off, refresh cookie `Secure` flag set |
| DB / RabbitMQ / MinIO ports | published to host | **not published** — internal Docker network only |
| Credentials | hardcoded defaults | **required env vars**, stack aborts if any is missing/empty |
| API containers | root, full caps | **non-root**, `cap_drop: ALL`, `no-new-privileges`, read-only rootfs, CPU/mem limits |
| Public surface | every API + UI on all interfaces | **only the web tier**, bound to `127.0.0.1` |

## 2. Deploy

```bash
cp .env.prod.example .env.prod
# Fill EVERY value. Generate each secret independently:
#   openssl rand -base64 48
$EDITOR .env.prod

docker compose --env-file .env.prod -f docker-compose.prod.yml up -d --build
```

The stack **fails closed**: a missing `Jwt__Secret`, DB password, RabbitMQ or
MinIO credential aborts startup instead of falling back to a dev default.

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
(15), and refresh tokens are opaque so they survive the change. The dev keypair in
`.env.example` is a committed throwaway for local use only — never deploy it.

## 6. Recommended follow-ups (defense in depth)

- **Least-privilege DB roles — implemented.** Each API connects as a non-superuser
  role that owns only its own schema (so EF `Migrate()` still works) and cannot
  reach other databases, create roles, or run `COPY ... PROGRAM`. A bootstrap
  superuser (`*_DB_ADMIN_*`, never used by the app) creates that role via
  `db/init/create-app-role.sh`, which Postgres runs **once, on first init of an
  empty data volume**. For an EXISTING deployment the script will not re-run — run
  its SQL manually against each DB. Dev (`docker-compose.yml`) intentionally stays
  on the `postgres` superuser for local convenience.
- **Secrets manager.** A `.env.prod` file on disk is better than committed
  defaults, but env vars are visible via `docker inspect` and `/proc`. Move to
  Docker secrets, Vault, or your cloud's KMS when you can.
- **TLS to Postgres** (`sslmode=require`) and encryption at rest.
- **Backups** of all four DB volumes + MinIO, with a **tested restore**.
- **Edge protection / WAF** (e.g. Cloudflare) in front of the host proxy for
  DDoS absorption and a second rate-limiting layer.
- **Tighten DB/broker capabilities** further once a known-good cap set is verified.
- **Unprivileged nginx image** (`nginxinc/nginx-unprivileged`) + read-only rootfs
  for the web tier.

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

- **Cloud firewall** (Oracle *Security Lists* / Lightsail firewall / cloud SG):
  allow inbound **443** and **80** *only from Cloudflare's published IP ranges*;
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
managed backup. Protect against both data loss and instance loss:

- Nightly `pg_dump` of all four databases to Oracle Object Storage (free tier) or
  any off-box bucket. Example cron (one line per DB via the running containers):
  ```bash
  docker exec legi-identity-db pg_dump -U "$IDENTITY_DB_USER" "$IDENTITY_DB_NAME" \
    | gzip > /backups/identity-$(date +%F).sql.gz
  # repeat for catalog/library/social, then upload /backups to object storage
  ```
- Mirror the MinIO buckets (`mc mirror`) to the same off-box storage.
- **Test the restore** before launch — an untested backup is not a backup.
- Keep `.env.prod` backed up **separately and encrypted** — it holds every secret,
  so store it apart from the data backups; leaking it compromises everything.

### 6.5 Stay portable (AWS-ready)

Everything above is vendor-neutral. Do **not** adopt OCI-managed databases or
proprietary services — keep state in the Docker volumes driven by
`docker-compose.prod.yml`. Migrating to Lightsail/EC2/Hetzner is then: provision a
box, copy the compose + `.env.prod`, restore the DB dumps, repoint Cloudflare DNS.

## 8. Pre-launch checklist

- [ ] `.env.prod` filled; no `CHANGE_ME` left; every secret unique and random
- [ ] JWT RS256 keypair generated; `Jwt__PrivateKey` set on identity-api ONLY
- [ ] `docker compose --env-file .env.prod -f docker-compose.prod.yml config` succeeds
- [ ] Cloudflare proxying domain; SSL/TLS = Full (strict)
- [ ] Origin firewall allows 443/80 only from Cloudflare IPs; SSH restricted
- [ ] SSH: password auth off, root login off, key-only; fail2ban + auto-updates on
- [ ] Host TLS (Caddy) serving HTTPS; HTTP→HTTPS redirect verified
- [ ] No infra ports reachable from outside the host (`nmap` the public IP)
- [ ] DB runtime role is non-superuser (`\du` shows no Superuser on `*_app`)
- [ ] No container published on `0.0.0.0` (web is `127.0.0.1` only)
- [ ] Forwarded headers verified: rate limiting throttles by real client IP
- [ ] Turnstile enabled with production keys; login lockout verified
- [ ] SMTP working; SPF/DKIM/DMARC set on the sending domain
- [ ] Nightly DB + MinIO backups running off-box; **restore tested**
- [ ] `.env.prod` backed up separately and encrypted
- [ ] `Jwt__AccessTokenExpirationMinutes` kept short (15)
