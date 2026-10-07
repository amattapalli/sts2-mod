# Slay the Spire 2 — The Transmuter (`sts2-mod`)

This private repository holds the **100% plain-text C# source code, localization JSONs, and Godot project files** for **The Transmuter** custom character mod for *Slay the Spire 2*.

> **Zero Binaries in Git:** `.gitignore` blocks all `*.dll`, `*.pck`, `*.pdb`, and `*.exe` files. Only source code and text/image assets are tracked in Git.

## How to Build & Playtest on Your Personal PC

1. **One-time prerequisites on your Personal PC:**
   - [.NET 9 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)
   - Subscribe to **BaseLib** on the [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3737335127).
   - *(Optional — only needed if adding custom Godot 3D/2D scenes or shaders later)*: [MegaDot 4.5.1 (Windows)](https://megadot.megacrit.com/4.5.1-m.14/megadot-4.5.1-m.14-windows-x86_64-llvm-editor-csharp.zip). For v1 (JSON localization + PNG icons), `BSchneppe.StS2.PckPacker` automatically packs `Transmuter.pck` during `dotnet build` without MegaDot.

2. **Pull, Build & Install (Windows PowerShell):**
   ```powershell
   git pull
   .\scripts\build-and-install.ps1 -Launch
   ```
   `Sts2PathDiscovery.props` automatically locates your local Steam install (`C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2`), compiles the C# source against your local `sts2.dll`, packs `Transmuter.pck` via `BSchneppe.StS2.PckPacker`, and copies the mod straight into your Steam `mods/Transmuter/` folder.

## Design & Art Docs
- [Procreate Art & Game Balancing Guide](docs/sts2_art_and_balancing_guide.md)
- [The Transmuter — Character & Card Pool Design](docs/sts2_transmuter_design.md)
- [Modding Toolchain & Architecture Research](docs/sts2_character_mod_research.md)
- [Character Style & Gimmick Brainstorm](docs/sts2_character_brainstorm.md)
