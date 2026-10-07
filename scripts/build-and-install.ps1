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

if ($GodotPath -ne "") {
    dotnet publish $ProjectFile -p:GodotPath="$GodotPath"
} else {
    dotnet publish $ProjectFile
}

if ($Launch) {
    Start-Process "steam://rungameid/2868840"
}
