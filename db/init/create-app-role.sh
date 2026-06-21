#!/bin/bash
# Create a NON-SUPERUSER application role that owns the `public` schema.
#
# Why: the APIs run EF Core migrations (DDL) at startup AND serve queries (DML)
# on the same connection. Owning the schema lets a plain role do both, while
# NOSUPERUSER means a compromised API cannot reach other databases on the cluster,
# create roles, read server files, or use COPY ... PROGRAM. The bootstrap
# superuser (POSTGRES_USER) is used only to create this role and is never used by
# the application at runtime.
#
# Runs once, automatically, on first initialisation of an empty data directory
# (Postgres only executes /docker-entrypoint-initdb.d scripts on a fresh volume).
#
# Requires APP_DB_USER / APP_DB_PASSWORD in the container environment.
# Note: the password is interpolated into a SQL string literal — keep it free of
# single quotes (the base64 secrets the project recommends already are).
set -euo pipefail

: "${APP_DB_USER:?APP_DB_USER is required}"
: "${APP_DB_PASSWORD:?APP_DB_PASSWORD is required}"

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-EOSQL
    DO \$do\$
    BEGIN
       IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = '${APP_DB_USER}') THEN
          CREATE ROLE "${APP_DB_USER}" LOGIN PASSWORD '${APP_DB_PASSWORD}'
             NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS;
       END IF;
    END
    \$do\$;

    GRANT CONNECT ON DATABASE "${POSTGRES_DB}" TO "${APP_DB_USER}";

    -- Own the schema so EF Core migrations work, but only inside THIS database.
    ALTER SCHEMA public OWNER TO "${APP_DB_USER}";

    -- Defence in depth: no implicit object creation by the PUBLIC role
    -- (already the default on PostgreSQL 15+, set explicitly for older clusters).
    REVOKE CREATE ON SCHEMA public FROM PUBLIC;
EOSQL

echo "create-app-role: ensured non-superuser role '${APP_DB_USER}' owns schema public in '${POSTGRES_DB}'."
