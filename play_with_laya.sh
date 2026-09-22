#!/usr/bin/env bash
# Build and run Snake with Laya playing. Any arguments go to the Laya agent, e.g.
#   ./play_with_laya.sh --verbose
#   ./play_with_laya.sh --mask-fatal
# Set LAYA_PYTHON to the interpreter that has laya installed (default: python3).
# See agent/README.md.
set -euo pipefail
cd "$(dirname "$0")"
# dotnet-install.sh puts the SDK in ~/.dotnet without adding it to PATH
command -v dotnet >/dev/null || export PATH="$HOME/.dotnet:$PATH"
exec dotnet run --project Snake/Snake.DesktopGL -c Release -- --laya "$@"
