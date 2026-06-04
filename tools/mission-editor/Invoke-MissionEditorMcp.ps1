# Generic MCP wrapper for ProjectAegis.MissionEditor.Cli verbs.
param(
    [Parameter(Mandatory = $true)]
    [string]$Command,
    [string[]]$ExtraArgs = @()
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$project = Join-Path $repoRoot 'src/ProjectAegis.MissionEditor.Cli/ProjectAegis.MissionEditor.Cli.csproj'
Push-Location $repoRoot
try {
    $args = @('run', '--project', $project, '--', $Command) + $ExtraArgs
    & dotnet @args
    exit $LASTEXITCODE
}
finally {
    Pop-Location
}