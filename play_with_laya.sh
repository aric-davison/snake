#!/usr/bin/env bash
# Build and run Snake with Laya playing. Any arguments go to the Laya agent, e.g.
#   ./play_with_laya.sh --verbose
#   ./play_with_laya.sh --mask-fatal
# Set LAYA_PYTHON to the interpreter that has laya installed (default: python3).
# See agent/README.md.
set -euo pipefail
cd "$(dirname "$0")"
exec dotnet run --project Snake/Snake.DesktopGL -c Release -- --laya "$@"
