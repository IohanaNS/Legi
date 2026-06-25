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
# IDEMPOTENT: existing secret files are kept (so re-running never rotates a live
# DB password out from under an already-initialised database). Pass --force to
# regenerate the auto-generated ones for a FRESH deploy (empty data volumes only).
# Structural values (DB names/users) are read from .env.prod, so fill that first.
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

if [ ! -f .env.prod ]; then
  echo "ERROR: .env.prod not found. Copy .env.prod.example to .env.prod and fill it first." >&2
  exit 1
fi

# Load the structural (non-secret) config we need to compose connection strings.
set -a
# shellcheck disable=SC1091
. ./.env.prod
set +a

SECRETS_DIR=secrets
mkdir -p "$SECRETS_DIR"
# Host-side protection is the directory (0700 — only the owner can enter it). The
# files themselves are 0644 ON PURPOSE: docker-compose (non-Swarm) bind-mounts each
# secret preserving its host mode/owner, and the API containers run as a NON-ROOT
# user whose uid does not match the host owner — a 0600 file would be unreadable
# (Permission denied) by AddKeyPerFile and by the Postgres init script (uid 70).
chmod 700 "$SECRETS_DIR"

# Write a secret file with no trailing newline. Honors --force; otherwise keeps an
# existing file so live DB passwords are never rotated by accident.
write_secret() {
  local name="$1" value="$2" f="$SECRETS_DIR/$1"
  if [ -f "$f" ] && [ "$FORCE" -eq 0 ]; then
    return
  fi
  printf '%s' "$value" > "$f"
  chmod 644 "$f"   # readable by the non-root container uid; see note above
  echo "  generated $name"
}

# Create a placeholder only if the file is missing — NEVER overwrites a real value,
# even with --force (these are filled in by hand from the provider's dashboard).
placeholder() {
  local name="$1" f="$SECRETS_DIR/$1"
  if [ ! -f "$f" ]; then
    printf '%s' "REPLACE_ME_$name" > "$f"
    chmod 644 "$f"
    echo "  placeholder $name  <-- EDIT THIS"
  fi
}

rand_pw()  { openssl rand -hex 24; }          # 192-bit, connection-string/SQL safe
rand_b64() { openssl rand -base64 32 | tr -d '\n'; }

echo "Generating secrets in $SECRETS_DIR/ (FORCE=$FORCE) ..."

# ---- JWT RS256 keypair ------------------------------------------------------
if [ ! -f "$SECRETS_DIR/Jwt__PrivateKey" ] || [ "$FORCE" -eq 1 ]; then
  tmp="$(mktemp -d)"; trap 'rm -rf "$tmp"' EXIT
  openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:2048 -out "$tmp/private.pem" 2>/dev/null
  openssl rsa -in "$tmp/private.pem" -pubout -out "$tmp/public.pem" 2>/dev/null
  write_secret Jwt__PrivateKey "$(openssl base64 -A -in "$tmp/private.pem")"
  write_secret Jwt__PublicKey  "$(openssl base64 -A -in "$tmp/public.pem")"
fi

# ---- MFA secret-encryption key ----------------------------------------------
write_secret Mfa__EncryptionKey "$(rand_b64)"

# ---- Per-database: admin + app passwords, and the API connection string ------
# $1 = service key (identity|catalog|library|social)
# $2 = connection-string secret name   $3 = db host   $4 = NAME var   $5 = USER var
gen_db() {
  local key="$1" cs_name="$2" host="$3" name_var="$4" user_var="$5"
  local db_name="${!name_var:-legi_$key}" db_user="${!user_var:-legi_${key}_app}"

  write_secret "${key}_db_admin_password" "$(rand_pw)"
  write_secret "${key}_db_app_password"   "$(rand_pw)"

  # Always (re)compose the connection string from the current app password +
  # structural values, so it stays in sync if you edit DB name/user in .env.prod.
  local app_pw; app_pw="$(cat "$SECRETS_DIR/${key}_db_app_password")"
  printf '%s' \
    "Host=${host};Port=5432;Database=${db_name};Username=${db_user};Password=${app_pw};SSL Mode=Require;Trust Server Certificate=true" \
    > "$SECRETS_DIR/${cs_name}"
  chmod 644 "$SECRETS_DIR/${cs_name}"
}

gen_db identity ConnectionStrings__IdentityDb      identity-db IDENTITY_DB_NAME IDENTITY_DB_USER
gen_db catalog  ConnectionStrings__CatalogDatabase catalog-db  CATALOG_DB_NAME  CATALOG_DB_USER
gen_db library  ConnectionStrings__LibraryDatabase library-db  LIBRARY_DB_NAME  LIBRARY_DB_USER
gen_db social   ConnectionStrings__SocialDatabase  social-db   SOCIAL_DB_NAME   SOCIAL_DB_USER
echo "  composed all ConnectionStrings__* (synced to app passwords)"

# ---- Message broker + object storage ----------------------------------------
write_secret RabbitMq__Password "$(rand_pw)"
write_secret Storage__AccessKey "$(rand_pw)"
write_secret Storage__SecretKey "$(rand_pw)"

# ---- Externally-issued secrets (fill these in by hand) ----------------------
placeholder Turnstile__SecretKey
placeholder Smtp__Password

echo
echo "Done. Review './secrets' and replace every REPLACE_ME_* placeholder before deploying:"
grep -rl "^REPLACE_ME_" "$SECRETS_DIR" 2>/dev/null | sed 's/^/  - /' || true
