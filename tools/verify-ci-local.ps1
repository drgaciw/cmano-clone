# Mirrors tools/buildkite/dotnet-ci.sh / .buildkite/pipeline.yml (local CI parity gate).
# Policy: production/qa/sprint-27-ci-hygiene-2026-06-18.md (S27-12)
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