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
                echo "Error: --character requires a value (transmuter | usurer | multiplayertrade | trade | all)" >&2
                exit 1
            fi
            CHARACTER="$(echo "$2" | tr '[:upper:]' '[:lower:]')"
            shift 2
            ;;
        --character=*)
            CHARACTER="$(echo "${1#*=}" | tr '[:upper:]' '[:lower:]')"
            shift
            ;;
        transmuter|usurer|multiplayertrade|trade|all)
            CHARACTER="$(echo "$1" | tr '[:upper:]' '[:lower:]')"
            shift
            ;;
        --help|-h)
            echo "Usage: ./scripts/build-and-install.sh [--character transmuter|usurer|multiplayertrade|all] [--launch]"
            exit 0
            ;;
        *)
            echo "Unknown option: $1" >&2
            echo "Usage: ./scripts/build-and-install.sh [--character transmuter|usurer|multiplayertrade|all] [--launch]" >&2
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
    multiplayertrade|trade)
        build_mod "MultiplayerTrade"
        ;;
    all|both)
        build_mod "Transmuter"
        build_mod "Usurer"
        build_mod "MultiplayerTrade"
        ;;
    *)
        echo "Error: Invalid mod '$CHARACTER'. Expected: transmuter, usurer, multiplayertrade, or all." >&2
        exit 1
        ;;
esac

# Clean up duplicate local mods/BaseLib if Steam Workshop BaseLib (3737335127) is installed,
# and ensure BaseLib + Transmuter + Usurer + MultiplayerTrade stay enabled in settings.save.
if [[ "$OSTYPE" == "darwin"* ]]; then
    WORKSHOP_BASELIB="$HOME/Library/Application Support/Steam/steamapps/workshop/content/2868840/3737335127"
    LOCAL_BASELIB="$HOME/Library/Application Support/Steam/steamapps/common/Slay the Spire 2/SlayTheSpire2.app/Contents/MacOS/mods/BaseLib"
    if [ -d "$WORKSHOP_BASELIB" ] && [ -d "$LOCAL_BASELIB" ]; then
        echo "==> Removing duplicate local mods/BaseLib so it doesn't collide with Steam Workshop BaseLib..."
        rm -rf "$LOCAL_BASELIB"
    fi

    if command -v python3 >/dev/null 2>&1; then
        python3 - <<'PY'
import glob, json, os

settings_paths = glob.glob(os.path.expanduser("~/Library/Application Support/Slay the Spire 2/steam/*/settings.save"))
for path in settings_paths:
    try:
        with open(path, "r", encoding="utf-8") as f:
            data = json.load(f)
    except Exception:
        continue

    mod_settings = data.setdefault("mod_settings", {})
    mod_settings["mods_enabled"] = True
    old_list = mod_settings.get("mod_list") or []

    # Remove duplicate local BaseLib entries and ensure BaseLib (steam_workshop),
    # Transmuter (mods_directory), Usurer (mods_directory), and MultiplayerTrade (mods_directory) are enabled.
    new_list = [
        {"id": "BaseLib", "is_enabled": True, "source": "steam_workshop"},
        {"id": "Transmuter", "is_enabled": True, "source": "mods_directory"},
        {"id": "Usurer", "is_enabled": True, "source": "mods_directory"},
        {"id": "MultiplayerTrade", "is_enabled": True, "source": "mods_directory"},
    ]
    seen = {
        ("BaseLib", "steam_workshop"),
        ("Transmuter", "mods_directory"),
        ("Usurer", "mods_directory"),
        ("MultiplayerTrade", "mods_directory"),
    }
    for item in old_list:
        mid = item.get("id")
        src = item.get("source")
        if mid == "BaseLib":
            continue
        if (mid, src) not in seen:
            seen.add((mid, src))
            new_list.append(item)

    mod_settings["mod_list"] = new_list
    try:
        with open(path, "w", encoding="utf-8") as f:
            json.dump(data, f, indent=2, sort_keys=True)
        print(f"==> Enabled BaseLib, Transmuter, Usurer, and MultiplayerTrade in {path}")
    except Exception:
        pass
PY
    fi
fi

if [ "$LAUNCH" = true ]; then
    echo "==> Launching Slay the Spire 2 via Steam..."
    if [[ "$OSTYPE" == "darwin"* ]]; then
        open "steam://rungameid/2868840"
    elif [[ "$OSTYPE" == "linux"* ]]; then
        xdg-open "steam://rungameid/2868840"
    fi
fi
