# Headless MCP adapter for scenario_validate (ADR-008 / TR-editor-005)
param(
    [Parameter(Mandatory = $true)]
    [string]$ScenarioPath,

    [switch]$ExportBrief,
    [string]$BriefOut
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Push-Location $repoRoot
try {
    if ($ExportBrief) {
        $args = @("scenario_export_brief", "--path", $ScenarioPath)
        if ($BriefOut) { $args += @("--out", $BriefOut) }
        dotnet run --project src/ProjectAegis.MissionEditor.Cli -- @args
        exit $LASTEXITCODE
    }

    dotnet run --project src/ProjectAegis.MissionEditor.Cli -- scenario_validate --path $ScenarioPath
    exit $LASTEXITCODE
}
finally {
    Pop-Location
}