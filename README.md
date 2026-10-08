# Slay the Spire 2 — Custom Character Mods (`sts2-mod`)

This repository contains two playable custom character mods for *Slay the Spire 2* built on **BaseLib**:

1. **The Transmuter (`Transmuter/`)** — A hermetic alchemist who imbues enemies with **Salt**, **Sulfur**, and **Mercury** to trigger volatile 2-Reagent **Reactions** (**Detonate**, **Calcify**, **Dissolve**) and the 3-Reagent **Magnum Opus**.
2. **The Usurer (`Usurer/`)** — An infernal syndicate moneylender (with full custom dark-fantasy artwork) who **Borrows** and **Repays** **Debt**, rides the **Over-Leveraged** (`10+` Debt) threshold, binds enemies in **Liens**, pauses interest with **Moratorium**, and **Forecloses** on enemy collateral.

> **Zero Binaries in Git:** `.gitignore` blocks all `*.dll`, `*.pck`, `*.pdb`, and `*.exe` files. Only C# source code, JSON localization, Godot config, and `.png` art assets are tracked in Git.

## How to Build & Playtest on Your Personal PC

1. **One-time prerequisites on your Personal PC:**
   - [.NET 9 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) (on macOS: `brew install dotnet-sdk`)
   - Subscribe to **BaseLib** on the [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3737335127).
   - `BSchneppe.StS2.PckPacker` automatically packs `Transmuter.pck` and `Usurer.pck` during `dotnet build` without needing MegaDot installed.

2. **Pull, Build & Install:**

   **macOS / Linux (Terminal):**
   ```bash
   git pull

   # Build & install BOTH characters (default) and launch Slay the Spire 2:
   ./scripts/build-and-install.sh --launch

   # Or build & install only The Usurer:
   ./scripts/build-and-install.sh --character usurer --launch

   # Or build & install only The Transmuter:
   ./scripts/build-and-install.sh --character transmuter --launch
   ```

   **Windows (PowerShell):**
   ```powershell
   git pull

   # Build & install BOTH characters (default) and launch Slay the Spire 2:
   .\scripts\build-and-install.ps1 -Launch

   # Or build & install only The Usurer:
   .\scripts\build-and-install.ps1 -Character usurer -Launch
   ```

   `Sts2PathDiscovery.props` automatically locates your local Steam install (`~/Library/Application Support/Steam/steamapps/common/Slay the Spire 2` on macOS or `C:\Program Files (x86)\Steam\...` on Windows), compiles the C# source against your local `sts2.dll`, packs `.pck` bundles via `BSchneppe.StS2.PckPacker`, and copies the mods straight into your game's `mods/Transmuter/` and `mods/Usurer/` folders.

## Running Unit & Simulation Tests
```bash
dotnet test Tests/Transmuter.Tests.csproj
```

## Design & Art Docs
- [The Usurer — Character, Mechanics & Monte Carlo Balance Design](docs/sts2_usurer_design.md)
- [The Transmuter — Character & Card Pool Design](docs/sts2_transmuter_design.md)
- [Procreate Art & Game Balancing Guide](docs/sts2_art_and_balancing_guide.md)
- [Modding Toolchain & Architecture Research](docs/sts2_character_mod_research.md)
- [Character Style & Gimmick Brainstorm](docs/sts2_character_brainstorm.md)
