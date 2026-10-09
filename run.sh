#!/usr/bin/env bash
# Usage: ./run.sh [test]   (default: start the API on port 5246)
set -euo pipefail

cd "$(dirname "$0")"

if [[ "${1:-}" == "test" ]]; then
  dotnet test
else
  # 0.0.0.0 so Codespaces port forwarding can reach the app.
  dotnet run --project src/HackerNewsBestStories.Api --urls http://0.0.0.0:5246
fi
