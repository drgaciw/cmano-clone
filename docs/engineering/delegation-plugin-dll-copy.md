# Delegation plugin DLL copy on Linux / headless hosts

Status: DRG-697.

`tools/copy-delegation-assemblies.ps1` publishes
`src/ProjectAegis.Delegation.UnityAdapter` for `netstandard2.1` and copies every
resulting `*.dll` into `unity/ProjectAegis/Assets/Plugins/ProjectAegis/`.

## Why headless tests need it

`ProjectAegis.Delegation.Tests` includes
`Unity_plugin_Delegation_dll_exports_Epic_A_types_used_by_Runtime_hosts`, which
asserts the copied `ProjectAegis.Delegation.dll` exists and exports the Epic A
types Unity runtime hosts use. On a fresh clone or worktree it fails with
"Missing plugin DLL" until the copy runs.

## Bash equivalent (no pwsh)

Run from the repo root:

```bash
P=unity/ProjectAegis/Assets/Plugins/ProjectAegis
T=.tmp-unity-plugin-publish
rm -rf "$T"; mkdir -p "$P"
dotnet publish src/ProjectAegis.Delegation.UnityAdapter/ProjectAegis.Delegation.UnityAdapter.csproj \
  -c Release -f netstandard2.1 -o "$T" -v minimal
cp -f "$T"/*.dll "$P"/
rm -rf "$T"
```

It takes about 5 seconds on a Sprite and copies 14 DLLs.

## Rules

- The DLLs are gitignored (`unity/ProjectAegis/Assets/Plugins/**/*.dll`). Only
  the `.meta` files are tracked. Never commit the DLLs.
- Re-run after changing Delegation, Data, Sim, or UnityAdapter code that Unity
  consumes.
- Each git worktree needs its own copy.
