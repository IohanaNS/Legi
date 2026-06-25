#!/bin/sh
# Enable TLS for this Postgres container, then hand off to the official entrypoint.
#
# Postgres refuses to start unless the private key is owned by the server user (or
# root) with no group/world access. A bind-mounted key is owned by the host UID,
# which inside the container is neither `postgres` nor `root`, so Postgres rejects
# it. We therefore copy the mounted cert/key into a postgres-owned directory with
# the required permissions on every start, before exec'ing docker-entrypoint.sh.
#
# This script runs as root (the image drops to the postgres user via gosu inside
# docker-entrypoint.sh). It works on both first init and subsequent starts because
# the cert is supplied by a host mount, not generated after the server is up — so
# there is no chicken-and-egg with the temp server the entrypoint runs during init.
set -e

src_dir=/etc/postgres-tls
dst_dir=/var/lib/postgresql/tls

mkdir -p "$dst_dir"
cp "$src_dir/server.crt" "$dst_dir/server.crt"
cp "$src_dir/server.key" "$dst_dir/server.key"
chown -R postgres:postgres "$dst_dir"
chmod 0700 "$dst_dir"
chmod 0644 "$dst_dir/server.crt"
chmod 0600 "$dst_dir/server.key"

# The image's default CMD is "postgres"; drop it so we don't pass it twice.
[ "${1:-}" = "postgres" ] && shift

exec docker-entrypoint.sh postgres \
  -c ssl=on \
  -c ssl_cert_file="$dst_dir/server.crt" \
  -c ssl_key_file="$dst_dir/server.key" \
  "$@"
