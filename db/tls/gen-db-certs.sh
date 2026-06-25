#!/usr/bin/env bash
# Generate a self-signed certificate used to encrypt Postgres connections in
# production (docker-compose.prod.yml).
#
# Trust model: the APIs connect with `SSL Mode=Require;Trust Server Certificate=true`,
# so this certificate is NOT chain-validated. Its job is to defeat *passive*
# sniffing of inter-container traffic on the Docker bridge (defence in depth), not
# to prove the server's identity — every database is on the same host behind an
# internal-only network, so an active MITM is not the threat being mitigated. One
# certificate is shared by all four databases (the CN is irrelevant under Require).
#
# Re-run with --force to rotate, then restart the db containers:
#   ./db/tls/gen-db-certs.sh --force
#   docker compose --env-file .env.prod -f docker-compose.prod.yml up -d \
#     identity-db catalog-db library-db social-db
#
# The generated server.crt / server.key are gitignored — never commit them.
set -euo pipefail

dir="$(cd "$(dirname "$0")" && pwd)"
crt="$dir/server.crt"
key="$dir/server.key"

if [[ -f "$crt" && -f "$key" && "${1:-}" != "--force" ]]; then
  echo "Certs already present in $dir (pass --force to regenerate)."
  exit 0
fi

openssl req -new -x509 -days 3650 -nodes \
  -newkey rsa:2048 \
  -keyout "$key" \
  -out "$crt" \
  -subj "/CN=legi-postgres"

chmod 0644 "$crt"
chmod 0600 "$key"
echo "Wrote $crt and $key (self-signed, valid 10 years)."
