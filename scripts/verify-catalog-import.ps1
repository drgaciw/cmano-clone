# Pre-merge catalog import verification.
# Run from repo root: ./scripts/verify-catalog-import.ps1
#
# Checks:
#   1. No *.db3 tracked in git (CMO game DB policy)
#   2. CmoMarkdown Import tests pass
# Exits non-zero on any failure.
#
# Also used as the two halves of the catalog gate from tools/verify-ci-local.ps1
# (local parity with tools/buildkite/dotnet-ci.sh):
#   - `-Db3CheckOnly` runs just the fast *.db3 policy check, before the build.
#   - `-Configuration Release -NoBuild` runs the CmoMarkdown Import tests against an
#     already-built Release output (reused after `dotnet build -c Release`), instead
#     of letting `dotnet test` default to a from-scratch Debug compile that is
#     immediately thrown away. Default invocation (no args) is unchanged: Debug,
#     full test run including build, both halves.
param(
    [string]$Configuration = 'Debug',
    [switch]$NoBuild,
    [switch]$Db3CheckOnly
)

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

    if ($Db3CheckOnly) {
        Write-Host '=== PASS (db3 check only) ==='
        return
    }

    # Baltic fixture tests expect default seed mode; clear corpus env leakage from enterprise runs.
    Remove-Item Env:AEGIS_PUBLIC_CORPUS -ErrorAction SilentlyContinue

    Write-Host 'Running CmoMarkdown Import tests...'
    $testArgs = @(
        'src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj'
        '-c', $Configuration
    )
    if ($NoBuild) {
        $testArgs += '--no-build'
    }
    $testArgs += @('-v', 'minimal', '--filter', 'FullyQualifiedName~CmoMarkdown')

    dotnet test @testArgs
    if ($LASTEXITCODE -ne 0) {
        Write-Host 'FAIL: CmoMarkdown Import tests' -ForegroundColor Red
        exit $LASTEXITCODE
    }

    Write-Host '=== PASS ==='
}
finally {
    Pop-Location
}
