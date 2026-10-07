param(
    [string]$GodotPath = "",
    [switch]$Launch
)

$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path -Parent $PSScriptRoot
$ProjectFile = Join-Path $RepoRoot "Transmuter\Transmuter.csproj"

if (-not (Test-Path $ProjectFile)) {
    Write-Error "Transmuter\Transmuter.csproj not found yet."
}

if ($GodotPath -ne "" -and (Test-Path $GodotPath)) {
    dotnet publish $ProjectFile -p:GodotPath="$GodotPath"
} else {
    # Uses BSchneppe.StS2.PckPacker NuGet package during 'dotnet build' to generate
    # Transmuter.dll, Transmuter.json, and Transmuter.pck without needing MegaDot installed.
    dotnet build $ProjectFile
}

if ($Launch) {
    Start-Process "steam://rungameid/2868840"
}
