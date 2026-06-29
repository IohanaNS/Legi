#!/usr/bin/env bash
# Run the production compose stack locally with generated throwaway secrets.
#
# This intentionally writes .env.prod.local instead of .env.prod so real
# production secrets are never overwritten by a local helper.
set -euo pipefail

cd "$(dirname "$0")/.."

ENV_FILE="${LEGI_PROD_ENV_FILE:-.env.prod.local}"
COMPOSE_FILE="docker-compose.prod.yml"
LOCAL_COMPOSE_FILE="docker-compose.prod-local.yml"
COMPOSE_PROJECT_NAME="${LEGI_PROD_PROJECT_NAME:-legi-prod-local}"
CMD="${1:-up}"

if (($# > 0)); then
  shift
fi

usage() {
  cat <<'EOF'
Usage:
  ./scripts/prod-local.sh init [--force]   Generate .env.prod.local
  ./scripts/prod-local.sh up [args...]     Build and start the prod stack locally
  ./scripts/prod-local.sh build [args...]  Build prod images
  ./scripts/prod-local.sh down [args...]   Stop the prod stack
  ./scripts/prod-local.sh logs [args...]   Follow logs
  ./scripts/prod-local.sh ps [args...]     Show containers
  ./scripts/prod-local.sh config [args...] Validate rendered compose config
  ./scripts/prod-local.sh turnstile test   Enable Turnstile with Cloudflare test keys
  ./scripts/prod-local.sh turnstile on     Enable Turnstile with real keys
  ./scripts/prod-local.sh turnstile off    Disable Turnstile again
  ./scripts/prod-local.sh turnstile status Show Turnstile settings, hiding secrets
  ./scripts/prod-local.sh google on        Enable Google sign-in with a client ID
  ./scripts/prod-local.sh google off       Disable Google sign-in
  ./scripts/prod-local.sh google status    Show Google sign-in settings

Environment:
  LEGI_PROD_ENV_FILE=.env.prod.local       Override the local env file path
  LEGI_PROD_PROJECT_NAME=legi-prod-local   Override the Docker Compose project
  WEB_HTTP_PORT=8080                       Override the local web port on init
  MAILPIT_HTTP_PORT=8025                   Override the local Mailpit UI port on init
  LEGI_TURNSTILE_SITE_KEY=...              Non-interactive real Turnstile setup
  LEGI_TURNSTILE_SECRET_KEY=...            Non-interactive real Turnstile setup
  LEGI_TURNSTILE_HOSTNAME=localhost        Hostname sent to the API allow-list
  LEGI_GOOGLE_CLIENT_ID=...                Non-interactive Google sign-in setup

Examples:
  ./scripts/prod-local.sh up
  ./scripts/prod-local.sh turnstile test
  ./scripts/prod-local.sh turnstile on
  ./scripts/prod-local.sh google on
  ./scripts/prod-local.sh logs web
  ./scripts/prod-local.sh down -v
EOF
}

rand_b64() {
  openssl rand -base64 "$1" | tr -d '\n'
}

rand_hex() {
  openssl rand -hex "$1"
}

set_env_value() {
  local key="$1"
  local value="$2"
  local tmp
  tmp="$(mktemp)"

  if [[ -f "$ENV_FILE" ]]; then
    awk -v key="$key" -v value="$value" '
      BEGIN { found = 0 }
      index($0, key "=") == 1 {
        print key "=" value
        found = 1
        next
      }
      { print }
      END {
        if (!found) {
          print key "=" value
        }
      }
    ' "$ENV_FILE" >"$tmp"
  else
    printf '%s=%s\n' "$key" "$value" >"$tmp"
  fi

  mv "$tmp" "$ENV_FILE"
  chmod 600 "$ENV_FILE"
}

set_env_value_if_missing() {
  local key="$1"
  local value="$2"

  if ! grep -qE "^${key}=.+" "$ENV_FILE"; then
    set_env_value "$key" "$value"
  fi
}

ensure_local_defaults() {
  local web_port
  web_port="$(grep -E '^WEB_HTTP_PORT=' "$ENV_FILE" | tail -n 1 | cut -d= -f2- || true)"
  web_port="${web_port:-${WEB_HTTP_PORT:-8080}}"
  local mailpit_port
  mailpit_port="$(grep -E '^MAILPIT_HTTP_PORT=' "$ENV_FILE" | tail -n 1 | cut -d= -f2- || true)"
  mailpit_port="${mailpit_port:-${MAILPIT_HTTP_PORT:-8025}}"

  set_env_value_if_missing "PasswordReset__FrontendBaseUrl" "http://localhost:$web_port"
  set_env_value_if_missing "PasswordReset__TokenLifetimeMinutes" "60"
  set_env_value_if_missing "EmailConfirmation__FrontendBaseUrl" "http://localhost:$web_port"
  set_env_value_if_missing "EmailConfirmation__TokenLifetimeMinutes" "1440"
  set_env_value_if_missing "WEB_HTTP_PORT" "$web_port"
  set_env_value_if_missing "MAILPIT_HTTP_PORT" "$mailpit_port"

  if grep -qE '^Smtp__Host=localhost$' "$ENV_FILE" || ! grep -qE '^Smtp__Host=.+' "$ENV_FILE"; then
    set_env_value "Smtp__Host" "mailpit"
    set_env_value "Smtp__Port" "1025"
    set_env_value "Smtp__Username" "local"
    set_env_value "Smtp__Password" "local"
    set_env_value "Smtp__FromAddress" "no-reply@localhost"
    set_env_value "Smtp__FromName" "BukiHub Local"
    set_env_value "Smtp__UseStartTls" "false"
  fi

  if grep -qE '^Smtp__Host=mailpit$' "$ENV_FILE"; then
    set_env_value_if_missing "Smtp__Username" "local"
    set_env_value_if_missing "Smtp__Password" "local"
  fi
}

generate_env() {
  local force="${1:-0}"

  if [[ -f "$ENV_FILE" && "$force" != "1" ]]; then
    echo "Using existing $ENV_FILE"
    ensure_local_defaults
    return
  fi

  if ! command -v openssl >/dev/null 2>&1; then
    echo "openssl is required to generate $ENV_FILE" >&2
    exit 1
  fi

  local tmp
  tmp="$(mktemp -d)"

  openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:2048 -out "$tmp/private.pem" 2>/dev/null
  openssl rsa -in "$tmp/private.pem" -pubout -out "$tmp/public.pem" 2>/dev/null

  local jwt_public_key jwt_private_key web_port
  jwt_public_key="$(openssl base64 -A -in "$tmp/public.pem")"
  jwt_private_key="$(openssl base64 -A -in "$tmp/private.pem")"
  web_port="${WEB_HTTP_PORT:-8080}"

  cat >"$ENV_FILE" <<EOF
# Local-only production-like environment generated by scripts/prod-local.sh.
# This file is gitignored. Do not use these throwaway secrets in real production.

Jwt__PublicKey=$jwt_public_key
Jwt__PrivateKey=$jwt_private_key
Jwt__Issuer=BukiHub
Jwt__Audience=BukiHub
Jwt__AccessTokenExpirationMinutes=15
Jwt__RefreshTokenExpirationDays=7

Mfa__EncryptionKey=$(rand_b64 32)

IDENTITY_DB_ADMIN_USER=legi_identity_admin
IDENTITY_DB_ADMIN_PASSWORD=$(rand_hex 24)
IDENTITY_DB_USER=legi_identity_app
IDENTITY_DB_PASSWORD=$(rand_hex 24)
IDENTITY_DB_NAME=legi_identity

CATALOG_DB_ADMIN_USER=legi_catalog_admin
CATALOG_DB_ADMIN_PASSWORD=$(rand_hex 24)
CATALOG_DB_USER=legi_catalog_app
CATALOG_DB_PASSWORD=$(rand_hex 24)
CATALOG_DB_NAME=legi_catalog

LIBRARY_DB_ADMIN_USER=legi_library_admin
LIBRARY_DB_ADMIN_PASSWORD=$(rand_hex 24)
LIBRARY_DB_USER=legi_library_app
LIBRARY_DB_PASSWORD=$(rand_hex 24)
LIBRARY_DB_NAME=legi_library

SOCIAL_DB_ADMIN_USER=legi_social_admin
SOCIAL_DB_ADMIN_PASSWORD=$(rand_hex 24)
SOCIAL_DB_USER=legi_social_app
SOCIAL_DB_PASSWORD=$(rand_hex 24)
SOCIAL_DB_NAME=legi_social

RabbitMq__Username=legi
RabbitMq__Password=$(rand_hex 24)

Storage__AccessKey=legi-local
Storage__SecretKey=$(rand_hex 32)
Storage__Bucket=legi-media
Storage__CoversBucket=legi-covers

Turnstile__Enabled=false
Turnstile__SecretKey=local-turnstile-disabled
Turnstile__AllowedHostnames__0=localhost
VITE_TURNSTILE_SITE_KEY=

GoogleAuth__ClientId=
VITE_GOOGLE_CLIENT_ID=

PasswordReset__FrontendBaseUrl=http://localhost:$web_port
PasswordReset__TokenLifetimeMinutes=60
EmailConfirmation__FrontendBaseUrl=http://localhost:$web_port
EmailConfirmation__TokenLifetimeMinutes=1440

Smtp__Host=mailpit
Smtp__Port=1025
Smtp__Username=local
Smtp__Password=local
Smtp__FromAddress=no-reply@localhost
Smtp__FromName=BukiHub Local
Smtp__UseStartTls=false

ExternalServices__GoogleBooks__Enabled=false
ExternalServices__GoogleBooks__ApiKey=

WEB_HTTP_PORT=$web_port
MAILPIT_HTTP_PORT=${MAILPIT_HTTP_PORT:-8025}
EOF

  chmod 600 "$ENV_FILE"
  rm -rf "$tmp"
  ensure_local_defaults
  echo "Generated $ENV_FILE"
}

enable_turnstile_test() {
  generate_env

  set_env_value "Turnstile__Enabled" "true"
  set_env_value "Turnstile__SecretKey" "1x0000000000000000000000000000000AA"
  set_env_value "Turnstile__AllowedHostnames__0" "localhost"
  set_env_value "VITE_TURNSTILE_SITE_KEY" "1x00000000000000000000AA"

  echo "Enabled Turnstile test keys in $ENV_FILE"
  echo "Run ./scripts/prod-local.sh up to rebuild the web image with the site key."
}

enable_turnstile_real() {
  generate_env

  local site_key="${LEGI_TURNSTILE_SITE_KEY:-}"
  local secret_key="${LEGI_TURNSTILE_SECRET_KEY:-}"
  local hostname="${LEGI_TURNSTILE_HOSTNAME:-localhost}"

  if (($# > 1)); then
    usage
    exit 64
  fi

  if (($# == 1)); then
    hostname="$1"
  fi

  if [[ -z "$site_key" && -t 0 ]]; then
    read -r -p "Turnstile site key: " site_key
  fi

  if [[ -z "$secret_key" && -t 0 ]]; then
    read -r -s -p "Turnstile secret key (hidden; paste and press Enter): " secret_key
    echo
    if [[ -n "$secret_key" ]]; then
      echo "Turnstile secret key captured."
    fi
  fi

  if [[ -z "$site_key" || -z "$secret_key" ]]; then
    echo "Set LEGI_TURNSTILE_SITE_KEY and LEGI_TURNSTILE_SECRET_KEY, or run this command interactively." >&2
    exit 64
  fi

  set_env_value "Turnstile__Enabled" "true"
  set_env_value "Turnstile__SecretKey" "$secret_key"
  set_env_value "Turnstile__AllowedHostnames__0" "$hostname"
  set_env_value "VITE_TURNSTILE_SITE_KEY" "$site_key"

  echo "Enabled real Turnstile keys in $ENV_FILE for hostname $hostname"
  echo "Run ./scripts/prod-local.sh up to rebuild the web image with the site key."
}

disable_turnstile() {
  generate_env

  set_env_value "Turnstile__Enabled" "false"
  set_env_value "Turnstile__SecretKey" "local-turnstile-disabled"
  set_env_value "Turnstile__AllowedHostnames__0" "localhost"
  set_env_value "VITE_TURNSTILE_SITE_KEY" ""

  echo "Disabled Turnstile in $ENV_FILE"
  echo "Run ./scripts/prod-local.sh up to rebuild the web image without the site key."
}

turnstile_status() {
  generate_env

  grep -E '^(Turnstile__Enabled|Turnstile__AllowedHostnames__0|VITE_TURNSTILE_SITE_KEY)=' "$ENV_FILE" || true

  if grep -qE '^Turnstile__SecretKey=.+$' "$ENV_FILE"; then
    echo "Turnstile__SecretKey=(set)"
  else
    echo "Turnstile__SecretKey=(missing)"
  fi
}

enable_google() {
  generate_env

  local client_id="${LEGI_GOOGLE_CLIENT_ID:-}"

  if (($# > 0)); then
    usage
    exit 64
  fi

  if [[ -z "$client_id" && -t 0 ]]; then
    read -r -p "Google OAuth web client ID: " client_id
  fi

  if [[ -z "$client_id" ]]; then
    echo "Set LEGI_GOOGLE_CLIENT_ID, or run this command interactively." >&2
    exit 64
  fi

  set_env_value "GoogleAuth__ClientId" "$client_id"
  set_env_value "VITE_GOOGLE_CLIENT_ID" "$client_id"

  echo "Enabled Google sign-in in $ENV_FILE"
  echo "Run ./scripts/prod-local.sh up to rebuild the web image with the client ID."
}

disable_google() {
  generate_env

  set_env_value "GoogleAuth__ClientId" ""
  set_env_value "VITE_GOOGLE_CLIENT_ID" ""

  echo "Disabled Google sign-in in $ENV_FILE"
  echo "Run ./scripts/prod-local.sh up to rebuild the web image without the button."
}

google_status() {
  generate_env

  if grep -qE '^GoogleAuth__ClientId=.+$' "$ENV_FILE"; then
    echo "GoogleAuth__ClientId=(set)"
  else
    echo "GoogleAuth__ClientId=(missing)"
  fi

  if grep -qE '^VITE_GOOGLE_CLIENT_ID=.+$' "$ENV_FILE"; then
    echo "VITE_GOOGLE_CLIENT_ID=(set)"
  else
    echo "VITE_GOOGLE_CLIENT_ID=(missing)"
  fi
}

# docker-compose.prod.yml now consumes Docker secrets (./secrets) and a Postgres TLS
# cert (./db/tls). Generate both from the throwaway .env.prod.local before any compose
# call so the local stack mirrors prod. gen-prod-secrets.sh reuses the env file's
# values (so existing local DB volumes keep authenticating) and is idempotent.
ensure_secrets_and_certs() {
  LEGI_SECRETS_ENV_FILE="$ENV_FILE" ./scripts/gen-prod-secrets.sh >/dev/null
  ./db/tls/gen-db-certs.sh >/dev/null
}

compose() {
  ensure_secrets_and_certs
  docker compose --env-file "$ENV_FILE" -p "$COMPOSE_PROJECT_NAME" -f "$COMPOSE_FILE" -f "$LOCAL_COMPOSE_FILE" "$@"
}

case "$CMD" in
  init)
    force="0"
    if [[ "${1:-}" == "--force" ]]; then
      force="1"
      shift
    fi
    if (($# > 0)); then
      usage
      exit 64
    fi
    generate_env "$force"
    ;;
  up)
    generate_env
    compose up -d --build "$@"
    echo "Web: http://localhost:$(grep -E '^WEB_HTTP_PORT=' "$ENV_FILE" | cut -d= -f2)"
    echo "Mailpit: http://localhost:$(grep -E '^MAILPIT_HTTP_PORT=' "$ENV_FILE" | cut -d= -f2)"
    ;;
  build)
    generate_env
    compose build "$@"
    ;;
  down)
    generate_env
    compose down "$@"
    ;;
  logs)
    generate_env
    compose logs -f "$@"
    ;;
  ps)
    generate_env
    compose ps "$@"
    ;;
  config)
    generate_env
    compose config "$@"
    ;;
  turnstile)
    subcommand="${1:-status}"
    if (($# > 0)); then
      shift
    fi

    case "$subcommand" in
      test)
        if (($# > 0)); then
          usage
          exit 64
        fi
        enable_turnstile_test
        ;;
      on|enable)
        enable_turnstile_real "$@"
        ;;
      off|disable)
        if (($# > 0)); then
          usage
          exit 64
        fi
        disable_turnstile
        ;;
      status)
        if (($# > 0)); then
          usage
          exit 64
        fi
        turnstile_status
        ;;
      *)
        usage
        exit 64
        ;;
    esac
    ;;
  google)
    subcommand="${1:-status}"
    if (($# > 0)); then
      shift
    fi

    case "$subcommand" in
      on|enable)
        enable_google "$@"
        ;;
      off|disable)
        if (($# > 0)); then
          usage
          exit 64
        fi
        disable_google
        ;;
      status)
        if (($# > 0)); then
          usage
          exit 64
        fi
        google_status
        ;;
      *)
        usage
        exit 64
        ;;
    esac
    ;;
  help|-h|--help)
    usage
    ;;
  *)
    usage
    exit 64
    ;;
esac
