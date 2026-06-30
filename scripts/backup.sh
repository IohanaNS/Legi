#!/usr/bin/env bash
# Back up all BukiHub state: the four Postgres databases (pg_dump custom format) and
# the MinIO buckets (mc mirror), into one timestamped directory.
#
# A backup on the same VM does NOT survive instance loss — SHIP THIS DIRECTORY
# OFF-BOX (object storage / another host). See Docs/DEPLOYMENT-hardening.md §6.4.
#
# Works against docker-compose.prod.yml AND the prod-local stack: it targets the
# fixed container names (identical in both) and dumps over Postgres' local trust
# socket (no password needed).
#
# Usage:
#   ./scripts/backup.sh [BACKUP_ROOT]            # default root: ./backups
#   BACKUP_RETENTION_DAYS=14 ./scripts/backup.sh  # prune dirs older than N days (0=keep all)
set -euo pipefail
cd "$(dirname "$0")/.."

BACKUP_ROOT="${1:-./backups}"
RETENTION_DAYS="${BACKUP_RETENTION_DAYS:-14}"
STAMP="$(date +%Y-%m-%d_%H%M%S)"
DEST="$BACKUP_ROOT/$STAMP"

SERVICES="identity catalog library social"
BUCKETS="legi-media legi-covers"

require_container() {
  # Must be actually RUNNING — `docker inspect` alone passes for a stopped container.
  if [ "$(docker inspect -f '{{.State.Running}}' "$1" 2>/dev/null)" != "true" ]; then
    echo "ERROR: container '$1' is not running — start the stack first." >&2
    exit 1
  fi
}

echo "Backing up to $DEST ..."
mkdir -p "$DEST/db" "$DEST/minio"

# ---- Postgres: one compressed custom-format dump per database -----------------
for svc in $SERVICES; do
  c="legi-${svc}-db"
  require_container "$c"
  adm="$(docker exec "$c" printenv POSTGRES_USER)"
  db="$(docker exec "$c" printenv POSTGRES_DB)"
  echo "  pg_dump $c ($db)"
  # -Fc: compressed custom format → pg_restore can do selective/parallel/robust restores.
  docker exec "$c" pg_dump -U "$adm" -Fc "$db" > "$DEST/db/${svc}.dump"
done

# ---- MinIO: mirror each bucket out via an mc sidecar sharing minio's netns -----
require_container legi-minio
acc="$(docker exec legi-minio cat /run/secrets/Storage__AccessKey)"
sec="$(docker exec legi-minio cat /run/secrets/Storage__SecretKey)"
echo "  mirror MinIO buckets: $BUCKETS"
docker run --rm --network "container:legi-minio" \
  --user "$(id -u):$(id -g)" \
  -e "MC_HOST_m=http://${acc}:${sec}@localhost:9000" \
  -e "MC_CONFIG_DIR=/tmp/.mc" \
  -e "BUCKETS=$BUCKETS" \
  -v "$(cd "$DEST/minio" && pwd):/out" \
  --entrypoint sh minio/mc -c '
    set -e
    # No `|| true`: a failed mirror (MinIO down, bad creds) must abort the backup —
    # set -e here exits non-zero, which the host set -e propagates. An empty bucket
    # mirrors successfully, so this only fails on a real error.
    for b in $BUCKETS; do
      mc mirror --quiet --overwrite --remove "m/$b" "/out/$b"
    done
  '

# ---- Manifest -----------------------------------------------------------------
{
  echo "created=$STAMP"
  echo "databases=$SERVICES"
  echo "minio_buckets=$BUCKETS"
} > "$DEST/manifest.txt"

echo "Backup complete: $DEST"
du -sh "$DEST" 2>/dev/null | awk '{print "  size: "$1}'

# ---- Optional off-box upload hook ---------------------------------------------
# Set BACKUP_UPLOAD_CMD (e.g. in /etc/legi/backup.env, read by the systemd unit) to
# ship this backup off the VM. BACKUP_DIR (this run) and BACKUP_ROOT are exported, so
# the command can reference them, e.g.:
#   BACKUP_UPLOAD_CMD='rclone copy "$BACKUP_DIR" remote:legi-backups/$(basename "$BACKUP_DIR")'
if [ -n "${BACKUP_UPLOAD_CMD:-}" ]; then
  echo "  off-box upload ..."
  BACKUP_DIR="$DEST" BACKUP_ROOT="$BACKUP_ROOT" sh -c "$BACKUP_UPLOAD_CMD"
fi

# ---- Retention: prune backups older than RETENTION_DAYS -----------------------
if [ "$RETENTION_DAYS" -gt 0 ]; then
  find "$BACKUP_ROOT" -mindepth 1 -maxdepth 1 -type d -mtime "+$RETENTION_DAYS" \
    -exec rm -rf {} + -print 2>/dev/null | sed 's/^/  pruned: /' || true
fi

cat <<'EOF'

NEXT: ship this directory OFF-BOX (e.g. `rclone copy` / `mc mirror` to object
storage). Back up your SECRETS SEPARATELY and encrypted — .env.prod + ./secrets +
./db/tls are NOT included here; without them a restored DB cannot be decrypted/used.
EOF
