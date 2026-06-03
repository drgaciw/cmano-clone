# .NET — Version Reference (Project Aegis)

| Field | Value |
|-------|-------|
| **SDK pin** | **8.0.400** ([`global.json`](../../../global.json)) |
| **Roll-forward** | `latestMajor` (SDK 10.x may build locally) |
| **Headless target** | `net8.0` (sim, delegation, tests, demo) |
| **Unity plugin target** | `netstandard2.1` (published to `unity/ProjectAegis/Assets/Plugins/ProjectAegis/`) |
| **Last verified** | 2026-06-02 |

## SDK on PATH

```powershell
dotnet --version    # repo pin: 8.0.400; roll-forward may report 10.x
dotnet --list-sdks
```

Install SDK 8.0.400 if missing: [.NET 8 download](https://dotnet.microsoft.com/download/dotnet/8.0).

Microsoft Learn: [global.json](https://learn.microsoft.com/en-us/dotnet/core/tools/global-json), [.NET SDK overview](https://learn.microsoft.com/en-us/dotnet/core/sdk).

## Dual C# toolchain

| Layer | Compiler | Language | Config |
|-------|----------|----------|--------|
| Headless (`src/`) | .NET SDK Roslyn | **C# 12** (`LangVersion: latest` on `net8.0`) | `*.csproj` |
| Unity `Assets/**/*.cs` | Unity Editor Roslyn | **C# 10** (unofficial opt-in) | [`unity/ProjectAegis/Assets/csc.rsp`](../../../unity/ProjectAegis/Assets/csc.rsp): `-langversion:10`, `-nullable:enable` |
| Unity default (official) | Unity 6 | **C# 9.0** | [Unity Manual — C# compiler](https://docs.unity3d.com/Manual/csharp-compiler.html) |

### Implementer rules

1. **Shared libraries** (`ProjectAegis.Sim`, `Delegation`, `Data`) built for `netstandard2.1` must not use C# 11+ syntax or APIs outside .NET Standard 2.1 unless isolated with `#if` or separate files.
2. **Unity-only scripts** under `unity/ProjectAegis/Assets/` — stay within the C# 10 subset validated by the Editor.
3. **Tests and tools** on `net8.0` may use `LangVersion: latest`; do not leak modern syntax into shared `netstandard2.1` code without review.
4. **Nullable** — `enable` in both `csc.rsp` and csproj (aligned).

Microsoft Learn: [C# language versioning](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/configure-language-version).

## Verification (headless gate)

From repo root:

```powershell
dotnet restore ProjectAegis.sln
dotnet build ProjectAegis.sln -v minimal
dotnet test ProjectAegis.sln -v minimal --filter "PlayModeSmokeHarnessTests|ReplayGolden"
```

Unity plugin DLLs (Editor path):

```powershell
dotnet build ProjectAegis.sln -c Release
./tools/copy-delegation-assemblies.ps1
./tools/Test-UnityPluginAssemblies.ps1
```

See [AGENTS.md](../../../AGENTS.md) and [unity/ProjectAegis/PLAYMODE-SMOKE.md](../../../unity/ProjectAegis/PLAYMODE-SMOKE.md).
