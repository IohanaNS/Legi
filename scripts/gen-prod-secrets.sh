#!/usr/bin/env bash
# Populate ./secrets with the Docker-secret files docker-compose.prod.yml expects.
#
# What it does:
#   * auto-generates the secrets we own (JWT RS256 keypair, MFA key, every DB
#     password, RabbitMQ, MinIO access/secret keys) — strong and unique;
#   * composes each API's connection string (embedding the generated app-role
#     password) so the DB password is never an API env var;
#   * leaves placeholder files for the externally-issued secrets (Turnstile,
#     SMTP) for you to fill in.
#
# Every file is written WITHOUT a trailing newline — .NET's AddKeyPerFile does not
# trim, and a stray "\n" would corrupt base64 keys and connection strings.
#
# REUSE-OR-GENERATE: if the env file already defines a secret (e.g. the throwaway
# values scripts/prod-local.sh writes into .env.prod.local), that value is reused —
# so an already-initialised DB keeps working. Otherwise a strong random value is
# generated. Real prod's .env.prod holds no secrets, so it always gets fresh randoms.
#
# IDEMPOTENT: existing secret files are kept (so re-running never rotates a live
# DB password out from under an already-initialised database). Pass --force to
# regenerate them for a FRESH deploy (empty data volumes only).
#
# Env file: defaults to .env.prod; override with LEGI_SECRETS_ENV_FILE (prod-local
# points this at .env.prod.local). Structural values (DB names/users) come from it.
#
# Usage:
#   cp .env.prod.example .env.prod && $EDITOR .env.prod
#   ./scripts/gen-prod-secrets.sh
#   # then drop in the external secrets, e.g.:
#   #   printf '%s' 'cf-turnstile-secret'  > secrets/Turnstile__SecretKey
#   #   printf '%s' 'smtp-password'        > secrets/Smtp__Password
set -euo pipefail

cd "$(dirname "$0")/.."

FORCE=0
[ "${1:-}" = "--force" ] && FORCE=1

ENV_FILE="${LEGI_SECRETS_ENV_FILE:-.env.prod}"

if [ ! -f "$ENV_FILE" ]; then
  echo "ERROR: $ENV_FILE not found. Copy .env.prod.example to .env.prod and fill it first." >&2
  exit 1
fi

# Read one value from the env file by key. Everything after the first `=` is the
# literal value (no quote/space processing) — matching how `docker compose
# --env-file` reads it, and unlike `source`, which would choke on values containing
# spaces (e.g. `Smtp__FromName=BukiHub Local`). Last assignment wins.
env_get() {
  sed -n "s/^$1=//p" "$ENV_FILE" | tail -n1
}

SECRETS_DIR=secrets
mkdir -p "$SECRETS_DIR"
# Host-side protection is the directory (0700 — only the owner can enter it). The
# files themselves are 0644 ON PURPOSE: docker-compose (non-Swarm) bind-mounts each
# secret preserving its host mode/owner, and the API containers run as a NON-ROOT
# user whose uid does not match the host owner — a 0600 file would be unreadable
# (Permission denied) by AddKeyPerFile and by the Postgres init script (uid 70).
chmod 700 "$SECRETS_DIR"

# Write a secret file with no trailing newline. Honors --force; otherwise keeps an
# existing file so live DB passwords are never rotated by accident. If env_var (3rd
# arg) names a non-empty value in the loaded env file, that value is reused instead
# of the generated fallback.
write_secret() {
  local name="$1" value="$2" env_var="${3:-}" f="$SECRETS_DIR/$1" reused
  if [ -f "$f" ] && [ "$FORCE" -eq 0 ]; then
    return
  fi
  if [ -n "$env_var" ]; then
    reused="$(env_get "$env_var")"
    [ -n "$reused" ] && value="$reused"
  fi
  printf '%s' "$value" > "$f"
  chmod 644 "$f"   # readable by the non-root container uid; see note above
  echo "  wrote $name"
}

# Externally-issued secrets: reuse the env-file value if present, else write a
# placeholder. NEVER clobbers an existing file (even with --force) — a hand-filled
# real secret must survive a regenerate.
placeholder() {
  local name="$1" env_var="$2" f="$SECRETS_DIR/$1" val
  if [ -f "$f" ]; then
    return
  fi
  val="$(env_get "$env_var")"
  if [ -n "$val" ]; then
    printf '%s' "$val" > "$f"; chmod 644 "$f"
    echo "  wrote $name (from $env_var)"
  else
    printf '%s' "REPLACE_ME_$name" > "$f"; chmod 644 "$f"
    echo "  placeholder $name  <-- EDIT THIS"
  fi
}

rand_pw()  { openssl rand -hex 24; }          # 192-bit, connection-string/SQL safe
rand_b64() { openssl rand -base64 32 | tr -d '\n'; }

echo "Generating secrets in $SECRETS_DIR/ (FORCE=$FORCE) ..."

# ---- JWT RS256 keypair ------------------------------------------------------
if [ ! -f "$SECRETS_DIR/Jwt__PrivateKey" ] || [ "$FORCE" -eq 1 ]; then
  env_priv="$(env_get Jwt__PrivateKey)"; env_pub="$(env_get Jwt__PublicKey)"
  if [ -n "$env_priv" ] && [ -n "$env_pub" ]; then
    write_secret Jwt__PrivateKey "$env_priv"
    write_secret Jwt__PublicKey  "$env_pub"
  else
    tmp="$(mktemp -d)"; trap 'rm -rf "$tmp"' EXIT
    openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:2048 -out "$tmp/private.pem" 2>/dev/null
    openssl rsa -in "$tmp/private.pem" -pubout -out "$tmp/public.pem" 2>/dev/null
    write_secret Jwt__PrivateKey "$(openssl base64 -A -in "$tmp/private.pem")"
    write_secret Jwt__PublicKey  "$(openssl base64 -A -in "$tmp/public.pem")"
  fi
fi

# ---- MFA secret-encryption key ----------------------------------------------
write_secret Mfa__EncryptionKey "$(rand_b64)" Mfa__EncryptionKey

# ---- Per-database: admin + app passwords, and the API connection string ------
# $1 = service key   $2 = connection-string secret name   $3 = db host
# $4 = NAME var   $5 = USER var   $6 = ADMIN_PASSWORD var   $7 = APP_PASSWORD var
gen_db() {
  local key="$1" cs_name="$2" host="$3" name_var="$4" user_var="$5" admin_pw_var="$6" app_pw_var="$7"
  local db_name db_user
  db_name="$(env_get "$name_var")"; db_name="${db_name:-legi_$key}"
  db_user="$(env_get "$user_var")"; db_user="${db_user:-legi_${key}_app}"

  write_secret "${key}_db_admin_password" "$(rand_pw)" "$admin_pw_var"
  write_secret "${key}_db_app_password"   "$(rand_pw)" "$app_pw_var"

  # Always (re)compose the connection string from the current app password +
  # structural values, so it stays in sync with the app-password secret.
  local app_pw; app_pw="$(cat "$SECRETS_DIR/${key}_db_app_password")"
  printf '%s' \
    "Host=${host};Port=5432;Database=${db_name};Username=${db_user};Password=${app_pw};SSL Mode=Require;Trust Server Certificate=true" \
    > "$SECRETS_DIR/${cs_name}"
  chmod 644 "$SECRETS_DIR/${cs_name}"
}

gen_db identity ConnectionStrings__IdentityDb      identity-db IDENTITY_DB_NAME IDENTITY_DB_USER IDENTITY_DB_ADMIN_PASSWORD IDENTITY_DB_PASSWORD
gen_db catalog  ConnectionStrings__CatalogDatabase catalog-db  CATALOG_DB_NAME  CATALOG_DB_USER  CATALOG_DB_ADMIN_PASSWORD  CATALOG_DB_PASSWORD
gen_db library  ConnectionStrings__LibraryDatabase library-db  LIBRARY_DB_NAME  LIBRARY_DB_USER  LIBRARY_DB_ADMIN_PASSWORD  LIBRARY_DB_PASSWORD
gen_db social   ConnectionStrings__SocialDatabase  social-db   SOCIAL_DB_NAME   SOCIAL_DB_USER   SOCIAL_DB_ADMIN_PASSWORD   SOCIAL_DB_PASSWORD
echo "  composed all ConnectionStrings__* (synced to app passwords)"

# ---- Message broker + object storage ----------------------------------------
write_secret RabbitMq__Password "$(rand_pw)" RabbitMq__Password
write_secret Storage__AccessKey "$(rand_pw)" Storage__AccessKey
write_secret Storage__SecretKey "$(rand_pw)" Storage__SecretKey

# ---- Externally-issued secrets (reused from env if present, else placeholder) ---
placeholder Turnstile__SecretKey Turnstile__SecretKey
placeholder Smtp__Password Smtp__Password

echo
echo "Done. Review './secrets' and replace every REPLACE_ME_* placeholder before deploying:"
grep -rl "^REPLACE_ME_" "$SECRETS_DIR" 2>/dev/null | sed 's/^/  - /' || true
