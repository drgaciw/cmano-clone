[CmdletBinding()]
param(
    [string]$OutputPath = "artifacts/resharper/inspectcode.sarif",
    [ValidateSet("INFO", "HINT", "SUGGESTION", "WARNING", "ERROR")]
    [string]$Severity = "WARNING"
)

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path
$solution = Join-Path $repoRoot "ProjectAegis.sln"
$toolHome = [Environment]::GetEnvironmentVariable("JETBRAINS_RESHARPER_CLT_HOME", "User")

if ([string]::IsNullOrWhiteSpace($toolHome)) {
    $toolHome = $env:JETBRAINS_RESHARPER_CLT_HOME
}

$inspectCode = if ($toolHome) { Join-Path $toolHome "inspectcode.exe" } else { $null }
if (-not $inspectCode -or -not (Test-Path -LiteralPath $inspectCode)) {
    $command = Get-Command inspectcode.exe -ErrorAction SilentlyContinue
    if (-not $command) {
        throw "inspectcode.exe was not found. Install ReSharper Command Line Tools or set JETBRAINS_RESHARPER_CLT_HOME."
    }
    $inspectCode = $command.Source
}

$resolvedOutput = if ([System.IO.Path]::IsPathRooted($OutputPath)) {
    $OutputPath
} else {
    Join-Path $repoRoot $OutputPath
}

$outputDirectory = Split-Path -Parent $resolvedOutput
New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null

& $inspectCode `
    $solution `
    "--output=$resolvedOutput" `
    "--format=Sarif" `
    "--severity=$Severity" `
    "--caches-home=$(Join-Path $repoRoot 'artifacts/resharper/cache')" `
    "--no-build" `
    "--no-updates"

if ($LASTEXITCODE -ne 0) {
    throw "InspectCode failed with exit code $LASTEXITCODE."
}

Write-Host "ReSharper inspection report: $resolvedOutput"
