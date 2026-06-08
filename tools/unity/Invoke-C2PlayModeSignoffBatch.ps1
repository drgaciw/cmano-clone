# S19-01 check 1: Unity Editor Play Mode starts without console errors (batchmode).
param(
    [ValidateSet("comms", "classify")]
    [string]$Scenario = "comms",
    [string]$UnityVersion = "6000.3.14f1",
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path
$projectPath = Join-Path $repoRoot "unity/ProjectAegis"
$unityExe = "C:\Program Files\Unity\Hub\Editor\$UnityVersion\Editor\Unity.exe"
$logFile = Join-Path $repoRoot "unity-c2-playmode-signoff.log"

$executeMethod = if ($Scenario -eq "classify") {
    "ProjectAegis.Unity.Editor.C2PlayModeSignoffBatchRunner.RunClassifyBatch"
} else {
    "ProjectAegis.Unity.Editor.C2PlayModeSignoffBatchRunner.RunBatch"
}

if (-not (Test-Path $unityExe)) {
    throw "Unity Editor not found at $unityExe. Install $UnityVersion via Unity Hub first."
}

Set-Location $repoRoot
& (Join-Path $repoRoot "tools/Test-UnityPluginAssemblies.ps1")

if (-not $SkipBuild) {
    dotnet build ProjectAegis.sln -c Release -v minimal
    & (Join-Path $repoRoot "tools/copy-delegation-assemblies.ps1")
    & (Join-Path $repoRoot "tools/Test-UnityPluginAssemblies.ps1")
}

Write-Host "Pass 1: import packages and compile editor scripts..."
$importArgs = @(
    "-batchmode",
    "-nographics",
    "-quit",
    "-projectPath", $projectPath,
    "-logFile", "$logFile.import"
)
$p1 = Start-Process -FilePath $unityExe -ArgumentList $importArgs -Wait -PassThru -NoNewWindow
if ($p1.ExitCode -ne 0) {
    Write-Host "Unity import pass failed ($($p1.ExitCode)). Tail:"
    if (Test-Path "$logFile.import") { Get-Content "$logFile.import" -Tail 40 }
    exit $p1.ExitCode
}

Write-Host "Pass 2: Play Mode sign-off ($Scenario) via $executeMethod ..."
$signoffArgs = @(
    "-batchmode",
    "-nographics",
    "-projectPath", $projectPath,
    "-executeMethod", $executeMethod,
    "-logFile", $logFile
)
$timeoutSeconds = 300
$p2 = Start-Process -FilePath $unityExe -ArgumentList $signoffArgs -PassThru -NoNewWindow
$deadline = (Get-Date).AddSeconds($timeoutSeconds)
while (-not $p2.HasExited) {
    if ((Get-Date) -gt $deadline) {
        Write-Host "Unity sign-off timed out after ${timeoutSeconds}s; terminating pid $($p2.Id)"
        Stop-Process -Id $p2.Id -Force -ErrorAction SilentlyContinue
        exit 124
    }
    Start-Sleep -Seconds 2
}

$consoleErrors = @()
$signoffPass = $false
$signoffFail = $false

if (Test-Path $logFile) {
    $logLines = Get-Content $logFile
    $consoleErrors = $logLines | Where-Object { $_ -match "SIGNOFF_ERROR:" } | ForEach-Object {
        ($_ -replace ".*SIGNOFF_ERROR:\s*", "").Trim()
    }
    $signoffPass = $logLines | Where-Object { $_ -match "C2PlayModeSignoffBatchRunner PASS:" } | Select-Object -First 1
    $signoffFail = $logLines | Where-Object { $_ -match "C2PlayModeSignoffBatchRunner FAIL:" } | Select-Object -First 1
}

$check1Pass = ($p2.ExitCode -eq 0) -and $signoffPass -and (-not $signoffFail)

Write-Host ""
Write-Host "=== S19-01 Check 1 (Play Mode console) ==="
Write-Host "Scenario:        $Scenario"
Write-Host "Execute method:  $executeMethod"
Write-Host "Unity exit code: $($p2.ExitCode)"
Write-Host "Log file:        $logFile"
Write-Host "Check 1 result:  $(if ($check1Pass) { 'PASS' } else { 'FAIL' })"

if ($consoleErrors.Count -gt 0) {
    Write-Host ""
    Write-Host "Captured console errors ($($consoleErrors.Count)):"
    $consoleErrors | ForEach-Object { Write-Host "  - $_" }
}

if (-not $check1Pass) {
    Write-Host ""
    Write-Host "Unity log tail:"
    if (Test-Path $logFile) { Get-Content $logFile -Tail 50 }
    exit [Math]::Max(1, $p2.ExitCode)
}

Write-Host ""
Write-Host "SUCCESS: Play Mode sign-off batch completed with no console errors."
exit 0