#!/usr/bin/env bash
# Restore a backup produced by scripts/backup.sh: the four Postgres databases and the
# MinIO buckets. DESTRUCTIVE — overwrites current data.
#
# The DB containers must be running (their non-superuser app roles must already exist
# — they are created on first init of an empty volume; the dumps do NOT contain roles).
# The API containers are stopped during the DB restore so `pg_restore --clean` isn't
# blocked by open connections, then restarted.
#
# Usage:
#   ./scripts/restore.sh <BACKUP_DIR> [--yes]
#   ./scripts/restore.sh ./backups/2026-06-26_120000 --yes
set -euo pipefail
cd "$(dirname "$0")/.."

SRC="${1:-}"
[ -n "$SRC" ] && [ -d "$SRC" ] || { echo "Usage: $0 <BACKUP_DIR> [--yes]" >&2; exit 64; }
ASSUME_YES=0; [ "${2:-}" = "--yes" ] && ASSUME_YES=1

SERVICES="identity catalog library social"
APIS="legi-identity-api legi-catalog-api legi-library-api legi-social-api"
BUCKETS="legi-media legi-covers"

if [ "$ASSUME_YES" -ne 1 ]; then
  echo "This will OVERWRITE all data in the running stack from: $SRC"
  printf "Type 'restore' to continue: "; read -r ans
  [ "$ans" = "restore" ] || { echo "Aborted."; exit 1; }
fi

echo "Stopping APIs ..."
# shellcheck disable=SC2086
docker stop $APIS >/dev/null 2>&1 || true

# ---- Postgres: drop/recreate objects, then reload (ownership preserved) -------
for svc in $SERVICES; do
  c="legi-${svc}-db"; dump="$SRC/db/${svc}.dump"
  [ -f "$dump" ] || { echo "  skip $svc (no $dump)"; continue; }
  adm="$(docker exec "$c" printenv POSTGRES_USER)"
  db="$(docker exec "$c" printenv POSTGRES_DB)"
  echo "  restore $c ($db)"
  # --clean --if-exists: drop existing objects first (idempotent). Ownership in the
  # dump (the app role) is reapplied since pg_restore runs as the superuser admin.
  docker exec -i "$c" pg_restore -U "$adm" -d "$db" --clean --if-exists < "$dump" 2>&1 \
    | grep -iE "error|fatal" | grep -ivE "does not exist|already exists" | head -5 \
    || true
done

# ---- MinIO: mirror buckets back in --------------------------------------------
if [ -d "$SRC/minio" ]; then
  acc="$(docker exec legi-minio cat /run/secrets/Storage__AccessKey)"
  sec="$(docker exec legi-minio cat /run/secrets/Storage__SecretKey)"
  echo "  restore MinIO buckets: $BUCKETS"
  docker run --rm --network "container:legi-minio" \
    --user "$(id -u):$(id -g)" \
    -e "MC_HOST_m=http://${acc}:${sec}@localhost:9000" \
    -e "MC_CONFIG_DIR=/tmp/.mc" \
    -e "BUCKETS=$BUCKETS" \
    -v "$(cd "$SRC/minio" && pwd):/in:ro" \
    --entrypoint sh minio/mc -c '
      set -e
      for b in $BUCKETS; do
        [ -d "/in/$b" ] && mc mirror --quiet --overwrite --remove "/in/$b" "m/$b" || true
      done
    '
fi

echo "Starting APIs ..."
# shellcheck disable=SC2086
docker start $APIS >/dev/null 2>&1 || true
echo "Restore complete from $SRC"
