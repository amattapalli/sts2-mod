#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

# Allow .NET 9 tools (like StS2PckPacker) to roll forward to .NET 10+
export DOTNET_ROLL_FORWARD="${DOTNET_ROLL_FORWARD:-Major}"

LAUNCH=false
CHARACTER="all"

while [[ $# -gt 0 ]]; do
    case "$1" in
        --launch|-launch|-l)
            LAUNCH=true
            shift
            ;;
        --character|-c)
            if [[ $# -lt 2 ]]; then
                echo "Error: --character requires a value (transmuter | usurer | all)" >&2
                exit 1
            fi
            CHARACTER="$(echo "$2" | tr '[:upper:]' '[:lower:]')"
            shift 2
            ;;
        --character=*)
            CHARACTER="$(echo "${1#*=}" | tr '[:upper:]' '[:lower:]')"
            shift
            ;;
        transmuter|usurer|all)
            CHARACTER="$(echo "$1" | tr '[:upper:]' '[:lower:]')"
            shift
            ;;
        --help|-h)
            echo "Usage: ./scripts/build-and-install.sh [--character transmuter|usurer|all] [--launch]"
            exit 0
            ;;
        *)
            echo "Unknown option: $1" >&2
            echo "Usage: ./scripts/build-and-install.sh [--character transmuter|usurer|all] [--launch]" >&2
            exit 1
            ;;
    esac
done

if ! command -v dotnet >/dev/null 2>&1; then
    echo "Error: 'dotnet' command not found. Please install the .NET 9 SDK (e.g., 'brew install dotnet-sdk' on macOS)." >&2
    exit 1
fi

build_mod() {
    local mod_name="$1"
    local csproj="$REPO_ROOT/$mod_name/$mod_name.csproj"
    echo "==> Building $mod_name for Slay the Spire 2..."
    dotnet build "$csproj"
    echo "==> $mod_name build succeeded and mod copied to mods/$mod_name/!"
}

case "$CHARACTER" in
    transmuter)
        build_mod "Transmuter"
        ;;
    usurer)
        build_mod "Usurer"
        ;;
    all|both)
        build_mod "Transmuter"
        build_mod "Usurer"
        ;;
    *)
        echo "Error: Invalid character '$CHARACTER'. Expected: transmuter, usurer, or all." >&2
        exit 1
        ;;
esac

if [ "$LAUNCH" = true ]; then
    echo "==> Launching Slay the Spire 2 via Steam..."
    if [[ "$OSTYPE" == "darwin"* ]]; then
        open "steam://rungameid/2868840"
    elif [[ "$OSTYPE" == "linux"* ]]; then
        xdg-open "steam://rungameid/2868840"
    fi
fi
