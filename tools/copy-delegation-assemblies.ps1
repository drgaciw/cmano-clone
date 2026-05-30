# Copies Release builds of delegation assemblies into unity/ProjectAegis/Plugins for Unity import.
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

Push-Location $root
try {
    dotnet build ProjectAegis.sln -c Release -v minimal
    $plugins = Join-Path $root "unity/ProjectAegis/Assets/Plugins/ProjectAegis"
    New-Item -ItemType Directory -Force -Path $plugins | Out-Null

    $dlls = @(
        "src/ProjectAegis.Sim/bin/Release/net8.0/ProjectAegis.Sim.dll",
        "src/ProjectAegis.Delegation/bin/Release/net8.0/ProjectAegis.Delegation.dll",
        "src/ProjectAegis.Delegation.UnityAdapter/bin/Release/net8.0/ProjectAegis.Delegation.UnityAdapter.dll"
    )

    foreach ($dll in $dlls) {
        $src = Join-Path $root $dll
        if (-not (Test-Path $src)) {
            throw "Missing build output: $src"
        }
        Copy-Item -Force $src (Join-Path $plugins (Split-Path -Leaf $src))
    }

    Write-Host "Copied delegation assemblies to $plugins"
}
finally {
    Pop-Location
}
