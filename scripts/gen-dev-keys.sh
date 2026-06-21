#!/usr/bin/env bash
# Generate a local DEV RSA keypair for JWT signing and write it into .env.
#
# These are throwaway development keys — never used in production (production keys
# live in .env.prod, generated separately; see Docs/DEPLOYMENT-hardening.md §5).
# We deliberately do NOT commit any private key to the repo.
#
# Usage:  ./scripts/gen-dev-keys.sh
set -euo pipefail

cd "$(dirname "$0")/.."

if [ ! -f .env ]; then
  cp .env.example .env
  echo "Created .env from .env.example"
fi

tmp="$(mktemp -d)"
trap 'rm -rf "$tmp"' EXIT

openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:2048 -out "$tmp/private.pem" 2>/dev/null
openssl rsa -in "$tmp/private.pem" -pubout -out "$tmp/public.pem" 2>/dev/null

# `openssl base64 -A` emits single-line base64 (portable across Linux/macOS).
PUB="$(openssl base64 -A -in "$tmp/public.pem")"
PRIV="$(openssl base64 -A -in "$tmp/private.pem")"

python3 - "$PUB" "$PRIV" <<'PY'
import sys, re, pathlib
pub, priv = sys.argv[1], sys.argv[2]
p = pathlib.Path(".env")
t = p.read_text()

def set_key(text, key, value):
    line = f"{key}={value}"
    if re.search(rf'^{re.escape(key)}=.*$', text, flags=re.M):
        return re.sub(rf'^{re.escape(key)}=.*$', lambda _: line, text, flags=re.M)
    return text.rstrip("\n") + "\n" + line + "\n"

t = set_key(t, "Jwt__PublicKey", pub)
t = set_key(t, "Jwt__PrivateKey", priv)
p.write_text(t)
PY

echo "Wrote a fresh dev RSA keypair into .env (Jwt__PublicKey / Jwt__PrivateKey)."
