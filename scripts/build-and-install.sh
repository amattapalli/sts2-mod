#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
PROJECT_FILE="$REPO_ROOT/Transmuter/Transmuter.csproj"

LAUNCH=false
for arg in "$@"; do
    case "$arg" in
        --launch|-launch|-l)
            LAUNCH=true
            ;;
    esac
done

if ! command -v dotnet >/dev/null 2>&1; then
    echo "Error: 'dotnet' command not found. Please install the .NET 9 SDK (e.g., 'brew install dotnet-sdk' on macOS)." >&2
    exit 1
fi

echo "==> Building Transmuter for Slay the Spire 2..."
dotnet build "$PROJECT_FILE"

echo "==> Transmuter build succeeded and mod copied to mods folder!"

if [ "$LAUNCH" = true ]; then
    echo "==> Launching Slay the Spire 2 via Steam..."
    if [[ "$OSTYPE" == "darwin"* ]]; then
        open "steam://rungameid/2868840"
    elif [[ "$OSTYPE" == "linux"* ]]; then
        xdg-open "steam://rungameid/2868840"
    fi
fi
