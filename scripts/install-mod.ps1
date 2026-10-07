param(
    [string]$Sts2Path = "C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2",
    [switch]$Launch
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $Sts2Path)) {
    $RegPath = Get-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 2868840" -ErrorAction SilentlyContinue
    if ($RegPath -and $RegPath.InstallLocation) {
        $Sts2Path = $RegPath.InstallLocation
    }
}

$ModsDir = Join-Path $Sts2Path "mods"
New-Item -ItemType Directory -Force -Path $ModsDir | Out-Null

$RepoRoot = Split-Path -Parent $PSScriptRoot
$DistDir = Join-Path $RepoRoot "dist"

$ModFolders = Get-ChildItem -Path $DistDir -Directory -ErrorAction SilentlyContinue
if (-not $ModFolders) {
    Write-Warning "No built mod folders found in dist/ yet."
    exit 0
}

foreach ($mod in $ModFolders) {
    $dest = Join-Path $ModsDir $mod.Name
    New-Item -ItemType Directory -Force -Path $dest | Out-Null
    Copy-Item -Path (Join-Path $mod.FullName "*") -Destination $dest -Recurse -Force
    Write-Host "Installed $($mod.Name) -> $dest"
}

if ($Launch) {
    Start-Process "steam://rungameid/2868840"
}
