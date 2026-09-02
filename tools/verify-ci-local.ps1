# Mirrors tools/buildkite/dotnet-ci.sh / .buildkite/pipeline.yml (local CI parity gate).
# Current solution test floor and standing invariants are governed by AGENTS.md §Hard Invariants (baseline floor >=1638 / 0 failures).
# ReplayGolden 6/6; PlayModeSmoke >=20/20; hash 17144800277401907079 preserved
# Bash parity when pwsh unavailable: bash tools/buildkite/dotnet-ci.sh
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot
try {
    Write-Host '=== CI local verify (Release) ==='
    dotnet restore ProjectAegis.sln
    & (Join-Path $repoRoot 'scripts\verify-catalog-import.ps1')
    dotnet build ProjectAegis.sln -c Release --no-restore
    dotnet test ProjectAegis.sln -c Release --no-build -v minimal
    dotnet test `
        src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj `
        -c Release --no-build -v minimal `
        --filter FullyQualifiedName~ReplayGoldenSuiteTests
    dotnet test `
        src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj `
        -c Release --no-build -v minimal `
        --filter FullyQualifiedName~PlayModeSmokeHarnessTests
    Write-Host '=== PASS ==='
}
finally {
    Pop-Location
}