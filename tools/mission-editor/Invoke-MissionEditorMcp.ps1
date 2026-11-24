# Generic MCP wrapper for ProjectAegis.MissionEditor.Cli verbs.
param(
    [Parameter(Mandatory = $true)]
    [string]$Command,
    [string[]]$ExtraArgs = @()
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$project = Join-Path $repoRoot 'src/ProjectAegis.MissionEditor.Cli/ProjectAegis.MissionEditor.Cli.csproj'
$aiHostProject = Join-Path $repoRoot 'src/ProjectAegis.Ai.Host/ProjectAegis.Ai.Host.csproj'
Push-Location $repoRoot
try {
    # The CLI intentionally communicates with Ai.Host over IPC and does not reference it.
    # Build the host explicitly so provider verbs also work from a clean project-only invocation.
    & dotnet build $aiHostProject --nologo
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    $args = @('run', '--project', $project, '--', $Command) + $ExtraArgs
    & dotnet @args
    exit $LASTEXITCODE
}
finally {
    Pop-Location
}
