# Copies netstandard2.1 publish output into unity/ProjectAegis/Assets/Plugins/ProjectAegis for Unity import.
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

Push-Location $root
try {
    $plugins = Join-Path $root "unity/ProjectAegis/Assets/Plugins/ProjectAegis"
    New-Item -ItemType Directory -Force -Path $plugins | Out-Null

    $tfm = "netstandard2.1"
    $publishDir = Join-Path $root ".tmp-unity-plugin-publish"
    if (Test-Path $publishDir) {
        Remove-Item -Recurse -Force $publishDir
    }

    dotnet publish (Join-Path $root "src/ProjectAegis.Delegation.UnityAdapter/ProjectAegis.Delegation.UnityAdapter.csproj") `
        -c Release -f $tfm -o $publishDir -v minimal

    Get-ChildItem $publishDir -Filter *.dll | ForEach-Object {
        Copy-Item -Force $_.FullName (Join-Path $plugins $_.Name)
    }

    Remove-Item -Recurse -Force $publishDir

    Write-Host "Copied delegation assemblies and dependencies to $plugins"
}
finally {
    Pop-Location
}
