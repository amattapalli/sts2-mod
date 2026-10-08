param(
    [ValidateSet("all", "both", "transmuter", "usurer", IgnoreCase = $true)]
    [string]$Character = "all",
    [string]$GodotPath = "",
    [switch]$Launch
)

$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path -Parent $PSScriptRoot
$env:DOTNET_ROLL_FORWARD = "Major"

function Build-Sts2Mod {
    param([string]$ModName)

    $ProjectFile = Join-Path $RepoRoot "$ModName\$ModName.csproj"
    if (-not (Test-Path $ProjectFile)) {
        Write-Error "$ModName\$ModName.csproj not found."
    }

    Write-Host "==> Building $ModName for Slay the Spire 2..."
    if ($GodotPath -ne "" -and (Test-Path $GodotPath)) {
        dotnet publish $ProjectFile -p:GodotPath="$GodotPath"
    } else {
        dotnet build $ProjectFile
    }
    Write-Host "==> $ModName build succeeded and mod copied to mods/$ModName/!"
}

switch ($Character.ToLowerInvariant()) {
    "transmuter" { Build-Sts2Mod -ModName "Transmuter" }
    "usurer"     { Build-Sts2Mod -ModName "Usurer" }
    default      {
        Build-Sts2Mod -ModName "Transmuter"
        Build-Sts2Mod -ModName "Usurer"
    }
}

if ($Launch) {
    Start-Process "steam://rungameid/2868840"
}
