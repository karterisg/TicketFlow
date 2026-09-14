#!/usr/bin/env bash
# Runs API + Web together, like Visual Studio's "multiple startup projects".
# Stop both with Ctrl+C.
set -m

cd "$(dirname "$0")"

pids=()

cleanup() {
  echo
  echo "Stopping..."
  for pid in "${pids[@]}"; do
    kill -TERM "-$pid" 2>/dev/null
  done
}
trap cleanup EXIT INT TERM

(cd src/TicketFlow.API && dotnet run --launch-profile https 2>&1 | sed 's/^/[api] /') &
pids+=("$!")

(cd src/TicketFlow.Web && dotnet run --launch-profile https 2>&1 | sed 's/^/[web] /') &
pids+=("$!")

wait
