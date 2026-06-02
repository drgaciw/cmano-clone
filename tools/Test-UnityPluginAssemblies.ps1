# Verifies Unity plugin DLLs exist under Assets/Plugins/ProjectAegis (after copy-delegation-assemblies.ps1).
param(
    [switch]$Quiet
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$plugins = Join-Path $root "unity/ProjectAegis/Assets/Plugins/ProjectAegis"

$coreDlls = @(
    "ProjectAegis.Data.dll",
    "ProjectAegis.Sim.dll",
    "ProjectAegis.Delegation.dll",
    "ProjectAegis.Delegation.UnityAdapter.dll"
)

$transitiveDlls = @(
    "Microsoft.Data.Sqlite.dll",
    "System.Text.Json.dll",
    "SQLitePCLRaw.core.dll"
)

$missing = @()
foreach ($name in ($coreDlls + $transitiveDlls)) {
    $path = Join-Path $plugins $name
    if (-not (Test-Path $path)) {
        $missing += $name
    }
}

if ($missing.Count -gt 0) {
    if (-not $Quiet) {
        Write-Host "Missing Unity plugin assemblies in $plugins :" -ForegroundColor Red
        $missing | ForEach-Object { Write-Host "  - $_" }
        Write-Host ""
        Write-Host "Run from repo root: ./tools/copy-delegation-assemblies.ps1" -ForegroundColor Yellow
    }
    exit 1
}

$count = (Get-ChildItem $plugins -Filter *.dll).Count
if (-not $Quiet) {
    Write-Host "Unity plugin assemblies OK ($count DLLs in $plugins)"
}
exit 0
