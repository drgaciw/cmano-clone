# Pre-merge catalog import verification.
# Run from repo root: ./scripts/verify-catalog-import.ps1
#
# Checks:
#   1. No *.db3 tracked in git (CMO game DB policy)
#   2. CmoMarkdown Import tests pass
# Exits non-zero on any failure.
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot
try {
    Write-Host '=== verify-catalog-import ==='

    $trackedDb3 = git ls-files '*.db3'
    if ($trackedDb3) {
        Write-Host 'FAIL: tracked *.db3 files (CMO game DB policy violation):' -ForegroundColor Red
        $trackedDb3 | ForEach-Object { Write-Host "  $_" }
        exit 1
    }
    Write-Host 'OK: no *.db3 in git ls-files'

    # Baltic fixture tests expect default seed mode; clear corpus env leakage from enterprise runs.
    Remove-Item Env:AEGIS_PUBLIC_CORPUS -ErrorAction SilentlyContinue

    Write-Host 'Running CmoMarkdown Import tests...'
    dotnet test `
        src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj `
        -v minimal `
        --filter "FullyQualifiedName~CmoMarkdown"
    if ($LASTEXITCODE -ne 0) {
        Write-Host 'FAIL: CmoMarkdown Import tests' -ForegroundColor Red
        exit $LASTEXITCODE
    }

    Write-Host '=== PASS ==='
}
finally {
    Pop-Location
}
