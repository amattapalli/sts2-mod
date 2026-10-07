param(
    [string]$Sts2Path = "C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $Sts2Path)) {
    $RegPath = Get-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 2868840" -ErrorAction SilentlyContinue
    if ($RegPath -and $RegPath.InstallLocation) {
        $Sts2Path = $RegPath.InstallLocation
    }
}

$DataDir = Join-Path $Sts2Path "data_sts2_windows_x86_64"
if (-not (Test-Path $DataDir)) {
    Write-Error "Could not find Slay the Spire 2 data directory at '$DataDir'. Pass -Sts2Path 'D:\Path\To\Slay the Spire 2'."
}

$RepoRoot = Split-Path -Parent $PSScriptRoot
$LibDir = Join-Path $RepoRoot "lib"
New-Item -ItemType Directory -Force -Path $LibDir | Out-Null

$Dlls = @("sts2.dll", "0Harmony.dll", "GodotSharp.dll", "SmartFormat.dll", "Steamworks.NET.dll")
foreach ($dll in $Dlls) {
    $src = Join-Path $DataDir $dll
    if (Test-Path $src) {
        Copy-Item -Path $src -Destination (Join-Path $LibDir $dll) -Force
        Write-Host "Copied $dll -> lib/$dll"
    }
}

Write-Host "`nDone! Run: git add lib/ && git commit -m 'chore: add sts2 reference dlls' && git push"
