# Scaffolds a minimal Unity 6.3 folder layout beside the .NET assemblies.
# Run from repo root after: dotnet build ProjectAegis.sln -c Release
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$unityRoot = Join-Path $root "unity/ProjectAegis"

$dirs = @(
    "Assets/Plugins/ProjectAegis",
    "Assets/Scripts/Runtime",
    "Packages",
    "ProjectSettings"
)
foreach ($d in $dirs) {
    New-Item -ItemType Directory -Force -Path (Join-Path $unityRoot $d) | Out-Null
}

$manifestTemplate = Join-Path $unityRoot "Packages/manifest.template.json"
$manifest = Join-Path $unityRoot "Packages/manifest.json"
if (-not (Test-Path $manifest)) {
    Copy-Item -Force $manifestTemplate $manifest
    Write-Host "Created Packages/manifest.json from template"
}

$versionFile = Join-Path $unityRoot "ProjectSettings/ProjectVersion.txt"
if (-not (Test-Path $versionFile)) {
    Set-Content -Path $versionFile -Value "m_EditorVersion: 6000.3.14f1`nm_EditorVersionWithRevision: 6000.3.14f1 (placeholder)"
    Write-Host "Created ProjectSettings/ProjectVersion.txt (open in Unity Hub 6.3 LTS)"
}

$runtimeSrc = Join-Path $unityRoot "Runtime"
$runtimeDst = Join-Path $unityRoot "Assets/Scripts/Runtime"
if (Test-Path $runtimeSrc) {
    Get-ChildItem -Path $runtimeSrc -Filter "*.cs" | ForEach-Object {
        Copy-Item -Force $_.FullName (Join-Path $runtimeDst $_.Name)
    }
    Write-Host "Copied Runtime scripts to Assets/Scripts/Runtime"
}

& (Join-Path $root "tools/copy-delegation-assemblies.ps1")

Write-Host "Unity scaffold ready under unity/ProjectAegis - open folder in Unity Hub 6.3 LTS"
Write-Host "See unity/ProjectAegis/PLAYMODE-SMOKE.md for scene setup."
