#!/usr/bin/env bash
# FocusPulse - Universal macOS & Linux Launcher
set -e

DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

echo "=========================================================="
echo "           FocusPulse Companion for Unix/macOS            "
echo "=========================================================="

if command -v python3 >/dev/null 2>&1; then
    exec python3 "$DIR/crossplatform/focuspulse.py" "$@"
elif command -v python >/dev/null 2>&1; then
    exec python "$DIR/crossplatform/focuspulse.py" "$@"
else
    echo "Error: Python 3 is required to run FocusPulse on Linux / macOS."
    echo "Please install python3 using your package manager (e.g. brew install python3 or apt install python3)."
    exit 1
fi
