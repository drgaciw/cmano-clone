# Verifies Unity plugin DLLs exist under Assets/Plugins/ProjectAegis (after copy-delegation-assemblies.ps1).
param(
    [switch]$Quiet
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$plugins = Join-Path $root "unity/ProjectAegis/Assets/Plugins/ProjectAegis"
$nuget = Join-Path $root "unity/ProjectAegis/Assets/Plugins/NuGet"

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

$mismatched = @(
    Get-ChildItem $plugins -Filter *.dll | Where-Object {
        $other = Join-Path $nuget $_.Name
        (Test-Path $other) -and
            (Get-FileHash $_.FullName).Hash -ne (Get-FileHash $other).Hash
    }
)
if ($mismatched.Count -gt 0) {
    Write-Error ("Conflicting game/MCP plugin dependencies: " +
        (($mismatched | ForEach-Object Name) -join ", ") +
        ". Run tools/copy-delegation-assemblies.ps1, then reload the Unity Editor domain.")
    exit 1
}

$count = (Get-ChildItem $plugins -Filter *.dll).Count
if (-not $Quiet) {
    Write-Host "Unity plugin assemblies OK ($count DLLs in $plugins)"
    Write-Host "Type-export guard: run 'dotnet test --filter UnityPluginEpicATypesTests' after Epic A host work."
}
exit 0
