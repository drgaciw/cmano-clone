[CmdletBinding()]
param(
    [string]$OutputPath = "artifacts/resharper/inspectcode.sarif",
    [ValidateSet("INFO", "HINT", "SUGGESTION", "WARNING", "ERROR")]
    [string]$Severity = "WARNING",
    [string]$DotnetPath = "dotnet",
    [string]$ToolPath,
    [switch]$Check,
    [string]$LedgerPath = "tools/resharper/ledger.json"
)

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path
$solution = Join-Path $repoRoot "ProjectAegis.sln"
$toolHome = $env:JETBRAINS_RESHARPER_CLT_HOME

if ([string]::IsNullOrWhiteSpace($toolHome)) {
    $toolHome = [Environment]::GetEnvironmentVariable("JETBRAINS_RESHARPER_CLT_HOME", "User")
}

$inspectCode = if ($ToolPath) { $ToolPath } elseif ($toolHome) { Join-Path $toolHome "inspectcode.exe" } else { $null }
if ($ToolPath -and -not (Test-Path -LiteralPath $ToolPath)) {
    throw "The requested ReSharper executable does not exist: $ToolPath"
}
if (-not $inspectCode -or -not (Test-Path -LiteralPath $inspectCode)) {
    $command = Get-Command inspectcode.exe -ErrorAction SilentlyContinue
    if (-not $command) {
        throw "inspectcode.exe was not found. Install ReSharper Command Line Tools or set JETBRAINS_RESHARPER_CLT_HOME."
    }
    $inspectCode = $command.Source
}
[string[]]$toolPrefix = if ([System.IO.Path]::GetFileNameWithoutExtension($inspectCode) -eq 'jb') { 'inspectcode' } else { @() }

$resolvedOutput = if ([System.IO.Path]::IsPathRooted($OutputPath)) {
    $OutputPath
} else {
    Join-Path $repoRoot $OutputPath
}

$outputDirectory = Split-Path -Parent $resolvedOutput
New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null

Push-Location $repoRoot
try {
    if ($Check -and $Severity -ne "WARNING") {
        throw "The regression gate requires WARNING severity so findings cannot be hidden."
    }
    $sdkVersion = (& $DotnetPath --version | Out-String).Trim()
    if ($LASTEXITCODE -ne 0 -or $sdkVersion -ne "8.0.400") {
        throw "Inspection requires SDK 8.0.400; selected '$sdkVersion'. Pass -DotnetPath to an isolated 8.0.400 installation."
    }
    $toolVersion = (& $inspectCode @toolPrefix --version | Out-String).Trim()
    if ($LASTEXITCODE -ne 0 -or $toolVersion -notmatch '\b2026\.2\.1\b') {
        throw "Inspection requires ReSharper CLI 2026.2.1; selected '$toolVersion'."
    }
    & $DotnetPath build $solution
    if ($LASTEXITCODE -ne 0) { throw "Build failed; inspection was not run." }

    $dotnetExe = (Get-Command $DotnetPath -ErrorAction Stop).Source
    $temporaryReport = "$resolvedOutput.$([guid]::NewGuid().ToString('N')).tmp.sarif"
    $arguments = @(
        $solution, "--output=$temporaryReport", "--format=Sarif", "--severity=$Severity",
        "--swea", "--disable-settings-layers=GlobalAll;GlobalPerProduct;SolutionPersonal;ProjectPersonal",
        "--dotnetcore=$dotnetExe", "--dotnetcoresdk=8.0.400",
        "--caches-home=$(Join-Path $repoRoot 'artifacts/resharper/cache')", "--no-build", "--no-updates"
    )
    & $inspectCode @toolPrefix @arguments
    $inspectionExitCode = $LASTEXITCODE
    $metadata = [ordered]@{
        sourceRevision = (& git rev-parse HEAD | Out-String).Trim()
        sourceStatus = @(& git status --porcelain=v1)
        sdkVersion = $sdkVersion
        toolVersion = $toolVersion
        arguments = $arguments
        exitCode = $inspectionExitCode
        targetFrameworks = "all (InspectCode default)"
    }
    $metadata | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath "$resolvedOutput.run.json" -Encoding utf8
    if ($inspectionExitCode -ne 0) { throw "InspectCode failed with exit code $inspectionExitCode." }
    if (-not (Test-Path -LiteralPath $temporaryReport) -or (Get-Item -LiteralPath $temporaryReport).Length -eq 0) {
        throw "InspectCode did not produce a non-empty report."
    }
    Move-Item -LiteralPath $temporaryReport -Destination $resolvedOutput -Force
    Write-Host "ReSharper inspection report: $resolvedOutput"
    if ($Check) {
        & python (Join-Path $PSScriptRoot 'sarif_gate.py') check --report $resolvedOutput --ledger $LedgerPath --output "$resolvedOutput.comparison.json"
        if ($LASTEXITCODE -ne 0) { throw "ReSharper regression comparison failed (exit $LASTEXITCODE)." }
    }
}
finally {
    Pop-Location
}
