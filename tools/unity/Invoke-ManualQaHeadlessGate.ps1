# Headless proxy before Unity manual C2 sign-off (production/qa/c2-manual-signoff-2026-06-02.md)
param(
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path
Set-Location $repoRoot

if (-not $SkipBuild) {
    dotnet build ProjectAegis.sln -v minimal
}

dotnet test ProjectAegis.sln -v minimal --filter "FullyQualifiedName~PlayModeSmoke|FullyQualifiedName~BalticReplayHarnessComms|FullyQualifiedName~MapPanelBinder|FullyQualifiedName~FuelState|FullyQualifiedName~ReplayGoldenBalticComms"

Write-Host ""
Write-Host "Headless proxy PASS expected. Next: Unity Editor manual checklist:"
Write-Host "  production/qa/c2-manual-signoff-2026-06-02.md"
Write-Host "  unity/ProjectAegis/PLAYMODE-SMOKE.md"