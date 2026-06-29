#!/bin/sh
# Feed RabbitMQ its default-user password from a Docker secret, then hand off to the
# official entrypoint.
#
# Why a wrapper: RabbitMQ 4.x treats RABBITMQ_DEFAULT_PASS_FILE as a *removed*
# variable and refuses to boot if it is set. The plain RABBITMQ_DEFAULT_PASS still
# works, so we read the secret file here and export it — keeping the password out of
# the compose file and `docker inspect` (it only ever lives in /run/secrets and the
# broker's own process environment).
#
# Runs as root; the official entrypoint drops to the rabbitmq user afterwards.
set -e

if [ -f /run/secrets/RabbitMq__Password ]; then
  RABBITMQ_DEFAULT_PASS="$(cat /run/secrets/RabbitMq__Password)"
  export RABBITMQ_DEFAULT_PASS
fi

# Overriding `entrypoint:` in compose clears the image's default CMD, so default to
# rabbitmq-server when no command was passed.
[ "$#" -eq 0 ] && set -- rabbitmq-server

exec docker-entrypoint.sh "$@"
