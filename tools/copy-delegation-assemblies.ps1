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
    if ($LASTEXITCODE -ne 0) {
        throw "Unity plugin publish failed with exit code $LASTEXITCODE. Existing plugins were not changed."
    }

    $dlls = @(Get-ChildItem $publishDir -Filter *.dll)
    $nuget = Join-Path $root "unity/ProjectAegis/Assets/Plugins/NuGet"
    $shared = @{}
    foreach ($dll in $dlls) {
        $other = Join-Path $nuget $dll.Name
        if (-not (Test-Path $other)) {
            continue
        }

        $published = [Reflection.AssemblyName]::GetAssemblyName($dll.FullName)
        $installed = [Reflection.AssemblyName]::GetAssemblyName($other)
        if ($published.Name -ne $installed.Name -or
            $published.CultureName -ne $installed.CultureName -or
            [BitConverter]::ToString($published.GetPublicKeyToken()) -ne
                [BitConverter]::ToString($installed.GetPublicKeyToken()) -or
            $published.Version -lt $installed.Version) {
            throw "Cannot unify $($dll.Name): published $published is incompatible with installed $installed. Align dependencies before publishing."
        }
        $shared[$dll.Name] = $other
    }

    $dlls | ForEach-Object {
        Copy-Item -Force $_.FullName (Join-Path $plugins $_.Name)
        # Mono can load excluded MCP DLLs by assembly-qualified name, bypassing importer settings.
        if ($shared.ContainsKey($_.Name)) {
            Copy-Item -Force $_.FullName $shared[$_.Name]
        }
    }

    Remove-Item -Recurse -Force $publishDir

    Write-Host "Copied delegation assemblies and dependencies to $plugins; unified $($shared.Count) shared MCP dependencies."
}
finally {
    Pop-Location
}
