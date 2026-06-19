# Mirrors tools/buildkite/dotnet-ci.sh / .buildkite/pipeline.yml (local CI parity gate).
# Policy: production/qa/sprint-33-ci-hygiene-2026-06-19.md (S33-12)
# Day-1 baseline (Release @ trunk): >=1046 solution tests (S32 closeout floor); closeout target >=1086
# Current sln ~1143 tests (S33-12 verify); ReplayGolden 6/6; PlayModeSmoke 17/17
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