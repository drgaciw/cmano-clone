#!/usr/bin/env bash
# POST a Jev decision request (state + typed questions) to the deployed jev-worker.
# Usage: tools/uat/jev-decide.sh <request.json>   (or "-" to read the request from stdin)
# Env (export in your own shell; never pass on the command line or commit):
#   JEV_WORKER_URL    e.g. https://jev-worker.<account-subdomain>.workers.dev
#   JEV_WORKER_TOKEN  the worker's bearer secret
# The token is handed to curl through a config on stdin so it never appears in the process list.
set -euo pipefail

: "${JEV_WORKER_URL:?Set JEV_WORKER_URL}"
: "${JEV_WORKER_TOKEN:?Set JEV_WORKER_TOKEN}"

request="${1:--}"
if [ "$request" = "-" ]; then
  body="$(cat)"
else
  body="$(cat "$request")"
fi

# Config on stdin carries the Authorization header; the body goes through a here-string file descriptor.
printf 'header = "Authorization: Bearer %s"\n' "$JEV_WORKER_TOKEN" |
  curl -sS --max-time 40 -K - \
    -H "Content-Type: application/json" \
    --data-binary "$body" \
    "$JEV_WORKER_URL"
echo
