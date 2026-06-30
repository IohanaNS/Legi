#!/usr/bin/env bash
# Encrypt the production secrets AT REST with SOPS + age.
#
# WHY this and not Vault/KMS (see Docs/DEPLOYMENT-hardening.md §6, "Secrets at rest"):
# on a single VM you can't escape the "secret zero" problem — the box must read the
# plaintext at boot, so something it can reach holds the key. Vault would just relocate
# that key and add a stateful service to seal/unseal/back up (a new SPOF) for no real
# gain at this scale. SOPS + age is a CLI, not a service: the encrypted bundle is safe
# to commit and back up (ciphertext); only the age PRIVATE key must be protected. It's
# vendor-neutral and swaps to a cloud KMS backend later WITHOUT changing this workflow.
#
# Flow:
#   ./scripts/secrets.sh init       # one-time: create your age key + .sops.yaml
#   ./scripts/gen-prod-secrets.sh   # produce plaintext ./secrets/*  (bootstrap/rotate)
#   ./scripts/secrets.sh encrypt    # ./secrets/* -> committable secrets.sops.env
#   git add .sops.yaml secrets.sops.env && git commit
#   # on the server / a new box / redeploy:
#   git pull && ./scripts/secrets.sh decrypt   # secrets.sops.env -> ./secrets/*
#   docker compose --env-file .env.prod -f docker-compose.prod.yml up -d
#
# The age PRIVATE key lives at $SOPS_AGE_KEY_FILE (default ~/.config/sops/age/keys.txt),
# NEVER in the repo. Back it up in your password manager — lose it and the bundle is
# unrecoverable. For true at-rest protection, mount ./secrets on a tmpfs on the server
# so the decrypted plaintext never touches persistent disk.
#
# Relies on every secret value being SINGLE-LINE (gen-prod-secrets guarantees this).
set -euo pipefail
cd "$(dirname "$0")/.."

BUNDLE="secrets.sops.env"
SECRETS_DIR="secrets"
AGE_KEY_FILE="${SOPS_AGE_KEY_FILE:-$HOME/.config/sops/age/keys.txt}"

need() {
  command -v "$1" >/dev/null 2>&1 || {
    echo "ERROR: '$1' is not installed. Install sops and age:" >&2
    echo "  https://github.com/getsops/sops/releases  |  https://github.com/FiloSottile/age" >&2
    exit 1
  }
}

usage() {
  sed -n '2,30p' "$0" | sed 's/^# \{0,1\}//'
}

cmd="${1:-help}"
case "$cmd" in
  init)
    need age-keygen
    if [ -f "$AGE_KEY_FILE" ]; then
      echo "age key already present at $AGE_KEY_FILE (keeping it)."
    else
      mkdir -p "$(dirname "$AGE_KEY_FILE")" && chmod 700 "$(dirname "$AGE_KEY_FILE")"
      age-keygen -o "$AGE_KEY_FILE" 2>/dev/null
      chmod 600 "$AGE_KEY_FILE"
      echo "Generated age key at $AGE_KEY_FILE"
      echo ">>> BACK THIS KEY UP (password manager). Without it, secrets.sops.env is unrecoverable."
    fi
    recipient="$(sed -n 's/.*public key: //p' "$AGE_KEY_FILE" | tail -n1)"
    esc="$(printf '%s' "$BUNDLE" | sed 's/\./\\./g')"
    {
      echo "# SOPS rules. The age recipient below is a PUBLIC key (not secret) — commit this file."
      echo "creation_rules:"
      echo "  - path_regex: (^|/)${esc}\$"
      echo "    age: \"${recipient}\""
    } > .sops.yaml
    echo "Wrote .sops.yaml (recipient ${recipient}). Commit .sops.yaml."
    ;;

  encrypt)
    need sops
    [ -d "$SECRETS_DIR" ] && [ -n "$(ls -A "$SECRETS_DIR" 2>/dev/null)" ] \
      || { echo "ERROR: ./$SECRETS_DIR is empty — run ./scripts/gen-prod-secrets.sh first." >&2; exit 1; }
    [ -f .sops.yaml ] || { echo "ERROR: .sops.yaml missing — run ./scripts/secrets.sh init." >&2; exit 1; }
    : > "$BUNDLE"
    for f in "$SECRETS_DIR"/*; do
      val="$(cat "$f")"   # command substitution strips trailing newlines
      # The dotenv bundle is one KEY=value line per secret; an internal newline would
      # split into bogus key-less lines and silently corrupt the round-trip. Refuse it
      # rather than encrypt garbage (gen-prod-secrets only ever writes single-line values).
      case "$val" in
        *$'\n'*) echo "ERROR: $f contains a newline; secrets must be single-line for the SOPS bundle." >&2; rm -f "$BUNDLE"; exit 1;;
      esac
      printf '%s=%s\n' "$(basename "$f")" "$val" >> "$BUNDLE"
    done
    sops --input-type dotenv -e -i "$BUNDLE"
    echo "Encrypted $(ls -1 "$SECRETS_DIR" | wc -l | tr -d ' ') secrets -> $BUNDLE (ciphertext — safe to commit)."
    ;;

  decrypt)
    need sops
    [ -f "$BUNDLE" ] || { echo "ERROR: $BUNDLE not found — encrypt first, or git pull." >&2; exit 1; }
    mkdir -p "$SECRETS_DIR" && chmod 700 "$SECRETS_DIR"
    # Each KEY=value line -> ./secrets/KEY, value verbatim (no trailing newline), 0644 so
    # the non-root API/Postgres uids can read it (matches gen-prod-secrets).
    sops --input-type dotenv --output-type dotenv -d "$BUNDLE" | while IFS= read -r line; do
      [ -z "$line" ] && continue
      k="${line%%=*}"; v="${line#*=}"
      printf '%s' "$v" > "$SECRETS_DIR/$k"
      chmod 644 "$SECRETS_DIR/$k"
    done
    echo "Decrypted $BUNDLE -> ./$SECRETS_DIR/ ($(ls -1 "$SECRETS_DIR" | wc -l | tr -d ' ') files)."
    ;;

  status)
    echo "bundle:   $BUNDLE $( [ -f "$BUNDLE" ] && echo '(present)' || echo '(missing)')"
    echo "sops cfg: .sops.yaml $( [ -f .sops.yaml ] && echo '(present)' || echo '(missing — run init)')"
    echo "age key:  $AGE_KEY_FILE $( [ -f "$AGE_KEY_FILE" ] && echo '(present)' || echo '(missing — run init)')"
    ;;

  *) usage ;;
esac
