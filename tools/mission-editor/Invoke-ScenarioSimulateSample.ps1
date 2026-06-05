# Headless MCP adapter for scenario_simulate_sample (ADR-008 / TR-editor-005)
param(
    [Parameter(Mandatory = $true)]
    [string]$ScenarioPath,

    [int]$Ticks = 32
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Push-Location $repoRoot
try {
    dotnet run --project src/ProjectAegis.MissionEditor.Cli -- scenario_simulate_sample --path $ScenarioPath --ticks $Ticks
    exit $LASTEXITCODE
}
finally {
    Pop-Location
}