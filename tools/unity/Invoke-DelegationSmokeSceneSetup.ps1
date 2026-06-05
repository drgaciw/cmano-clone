# Builds DelegationSmoke.unity via Unity batchmode (PLAYMODE-SMOKE.md stack).
param(
    [string]$ScenarioPolicyId = "baltic-patrol-comms",
    [string]$UnityVersion = "6000.3.14f1"
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path
$projectPath = Join-Path $repoRoot "unity/ProjectAegis"
$unityExe = "C:\Program Files\Unity\Hub\Editor\$UnityVersion\Editor\Unity.exe"
$logFile = Join-Path $repoRoot "unity-delegation-smoke-setup.log"

if (-not (Test-Path $unityExe)) {
    throw "Unity Editor not found at $unityExe. Install 6000.3.14f1 via Unity Hub first."
}

Set-Location $repoRoot
dotnet build ProjectAegis.sln -c Release -v minimal
& (Join-Path $repoRoot "tools/copy-delegation-assemblies.ps1")
& (Join-Path $repoRoot "tools/Test-UnityPluginAssemblies.ps1")

Write-Host "Pass 1: import packages and compile scripts..."
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
    if (Test-Path "$logFile.import") { Get-Content "$logFile.import" -Tail 30 }
    exit $p1.ExitCode
}

Write-Host "Pass 2: build DelegationSmoke scene..."
$args = @(
    "-batchmode",
    "-nographics",
    "-quit",
    "-projectPath", $projectPath,
    "-executeMethod", "ProjectAegis.Unity.Editor.DelegationSmokeSceneBuilder.BuildBatch",
    "-logFile", $logFile
)

$p = Start-Process -FilePath $unityExe -ArgumentList $args -Wait -PassThru -NoNewWindow
if ($p.ExitCode -ne 0) {
    Write-Host "Unity exited with code $($p.ExitCode). Tail of log:"
    if (Test-Path $logFile) { Get-Content $logFile -Tail 40 }
    exit $p.ExitCode
}

$scenePath = Join-Path $projectPath "Assets/Scenes/DelegationSmoke.unity"
if (-not (Test-Path $scenePath)) {
    throw "Scene not created at $scenePath"
}

Write-Host ""
Write-Host "SUCCESS: $scenePath"
Write-Host "Open in Unity Hub: $projectPath"
Write-Host "Press Play with scenario $ScenarioPolicyId (set on DelegationBridgeHost if rebuilding classify scene)."
Write-Host "Manual QA: production/qa/c2-manual-signoff-2026-06-02.md"
