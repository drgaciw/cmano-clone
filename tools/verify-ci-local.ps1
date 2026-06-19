# Mirrors tools/buildkite/dotnet-ci.sh / .buildkite/pipeline.yml (local CI parity gate).
# Policy: production/qa/sprint-35-ci-hygiene-2026-06-19.md (S35-15)
# Day-1 baseline (Release @ trunk): >=1204 solution tests (S35-01 floor 1193; current sln count)
# Closeout target >=1204; ReplayGolden 6/6; PlayModeSmoke 17/17
# Bash parity when pwsh unavailable: bash tools/buildkite/dotnet-ci.sh
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot
try {
    Write-Host '=== CI local verify (Release) ==='
    dotnet restore ProjectAegis.sln
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