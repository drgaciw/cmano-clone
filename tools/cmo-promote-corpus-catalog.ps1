# Promote scratch nightly catalog to enterprise production DB after coverage gate.
param(
    [Parameter(Mandatory = $true)]
    [string]$RunDate,
    [string]$ScratchDir = "",
    [double]$MinCoveragePercent = 99.0,
    [switch]$SkipCoverageGate
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$scratch = if ($ScratchDir) { $ScratchDir } else { Join-Path $repoRoot "scratch\nightly-cmo-$RunDate" }
$sourceDb = Join-Path $scratch "catalog-proposed.db"
$targetDb = Join-Path $repoRoot "assets\data\catalog\aegis_public_corpus.db"
$balticDb = Join-Path $repoRoot "assets\data\catalog\baltic_patrol.db"

if (-not (Test-Path $sourceDb)) {
    Write-Error "Scratch catalog not found: $sourceDb (run cmo-nightly-import + approve first)"
}

Write-Host "=== cmo-promote-corpus-catalog ==="
Write-Host "Source: $sourceDb"
Write-Host "Target: $targetDb"

if (-not $SkipCoverageGate) {
    & (Join-Path $PSScriptRoot "cmo-verify-corpus-coverage.ps1") -DbPath $sourceDb -MinCoveragePercent $MinCoveragePercent
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Coverage gate failed; not promoting."
    }
}

$targetDir = Split-Path $targetDb -Parent
New-Item -ItemType Directory -Force -Path $targetDir | Out-Null
Copy-Item -Path $sourceDb -Destination $targetDb -Force

if ((Resolve-Path $targetDb).Path -eq (Resolve-Path $balticDb).Path) {
    Write-Error "Refusing to overwrite baltic_patrol.db"
}

Write-Host "Promoted to $targetDb"
Write-Host "Snapshot id: aegis_public_corpus"
Write-Host "=== PASS ==="
