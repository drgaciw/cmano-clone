# Linux vs Windows Build Gap Analysis — Project Aegis

| Field | Value |
|-------|--------|
| **Date** | 2026-07-20 |
| **Method** | Superpowers evaluation workflow + GitNexus code intelligence + source/CI evidence |
| **Indexed commit** | `adf3748` |
| **GitNexus index** | 26,939 nodes · 51,171 edges · 468 clusters · 300 flows (rebuilt for this study) |
| **Scope** | Headless .NET gate, Unity Editor/player packaging, tooling OS asymmetry, source portability |
| **Companion HTML** | [linux-vs-windows-build-gap-analysis-2026-07-20.html](./linux-vs-windows-build-gap-analysis-2026-07-20.html) |

---

## Executive verdict

**Headless .NET (the merge authority) is Linux-first and largely OS-agnostic.** Buildkite hosted Linux agents own the blocking gate. Windows local verification exists as a thinner PowerShell mirror.

**Unity player shipping is dual-target in API (`StandaloneLinux64` / `StandaloneWindows64`) but CI-gated on neither OS.** Linux is the documented batchmode example and the only Editor menu shortcut; Windows is assumed for interactive Editor automation via hardcoded `Unity.exe` paths.

**Core sim/delegation/data C# has no OS platform branching** — gaps are tooling, Editor host, and player packaging, not `#if LINUX` / `#if WIN` in `src/`.

```text
                    Linux                              Windows
Headless .NET       Buildkite (authoritative)          verify-ci-local.ps1 (partial mirror)
                    Cursor Cloud agents                Local pwsh / Visual Studio
Unity Editor UTF    Manual GHA ubuntu + license        Hardcoded Unity.exe batch scripts
Unity Player        BuildPlayer API + Linux menu       Same API — neither OS in CI
Plugin DLL copy     Needs pwsh today                   Native PowerShell
Core sim source     Portable (no OS #ifs)              Same binaries under `dotnet`
```

---

## 1. Methodology (Superpowers + GitNexus)

### Superpowers

Used the global Superpowers process skill (`using-superpowers`) to structure the evaluation before edits: orient → gather evidence → compare surfaces → severity-ranked gaps → remediation. No product code was changed for this study.

### GitNexus

1. Rebuilt the index (`npx gitnexus analyze`) — was missing in this workspace; now **up-to-date** at commit `adf3748`.
2. `query("Unity player build StandaloneLinux BuildPlayer")` → resolved `BuildPlayer`, `BuildLinux64Menu`, related Editor builders.
3. `context(BuildPlayer)` / `context(PerformBuild)` → methods `PerformBuild` → `Run` → `ResolveScenes` / `ParseTarget` (Linux64 | Win64 | OSX); **no upstream callers** in the graph (CI entry is external batchmode).
4. `impact(BuildPlayer, upstream)` → **LOW** (0 callers) — player packaging is isolated from the sim call graph.
5. `impact(DelegationBridge, upstream)` → **CRITICAL** (168 impacted) — confirms why merge gates stay on the headless Linux path and avoid Editor/player churn for Release invariants.
6. Cypher scan for BuildPlayer/Standalone symbols → only Editor packaging + ADR-017 docs; no Windows-only or Linux-only runtime forks in the indexed sim graph.
7. `query` for CI/dotnet tooling → mostly test/export symbols; CI scripts are shell-first and lightly indexed as files (e.g. `tools/copy-delegation-assemblies.ps1`).

**Implication from the graph:** changing player packaging is low blast radius; changing `DelegationBridge` is not. Linux CI correctly treats headless .NET as the authority for merge risk.

---

## 2. Headless .NET — Linux CI vs Windows local

### Linux (authoritative)

| Surface | Evidence |
|---------|----------|
| Buildkite blocking gate | `.buildkite/pipeline.yml` → `bash tools/buildkite/agent-dotnet-ci.sh` |
| Release restore/build/test | `tools/buildkite/dotnet-ci.sh` |
| ReplayGolden + PlayModeSmoke filters | Same script |
| Baltic v2 hash + DelegationBridge ref grep | `dotnet-ci.sh` lines 49–54 |
| Gitleaks | `agent-gitleaks.sh` (Linux binary archive) |
| Cursor Cloud | `.cursor/cloud-install.sh` + Linux `dotnet-install.sh` |

Docs: *"Primary blocking CI runs on **Buildkite hosted Linux agents**"* — [`docs/engineering/buildkite-ci.md`](../engineering/buildkite-ci.md).

### Windows (local mirror)

| Surface | Evidence |
|---------|----------|
| Local CI parity | `tools/verify-ci-local.ps1` |
| Comment admits bash fallback | *"Bash parity when pwsh unavailable: bash tools/buildkite/dotnet-ci.sh"* |

### Parity table

| Check | Linux `dotnet-ci.sh` | Windows `verify-ci-local.ps1` |
|-------|----------------------|-------------------------------|
| Restore / build / test Release | Yes | Yes |
| ReplayGolden filter | Yes | Yes |
| PlayModeSmokeHarness filter | Yes | Yes |
| Hash grep `17144800277401907079` | Yes | **Missing** |
| DelegationBridge ref count | Yes | **Missing** |
| Gitleaks | Separate pipeline step | **Missing** |
| SDK bootstrap | `agent-bootstrap-dotnet.sh` | Assumes `dotnet` on PATH |

**Gap:** docs describe `verify-ci-local.ps1` as the Windows local gate that mirrors Buildkite, but the mirror omits S67 verification-before greps that Linux CI always runs.

---

## 3. Unity Editor and player builds

### GitNexus view of `BuildPlayer`

| Symbol | Role | Upstream impact |
|--------|------|-----------------|
| `BuildPlayer` class | Batchmode + menu entry | LOW (0 callers) |
| `PerformBuild` | CLI entry (`-buildTarget`, `-scriptingBackend`, …) | Calls `Run` / parsers |
| `BuildLinux64Menu` | Only Editor menu item — **Linux Mono** | Isolated |
| `ParseTarget` | `StandaloneLinux64` \| `StandaloneWindows64` \| `StandaloneOSX` | — |
| `DefaultOutput` | Adds `.exe` suffix only for Win64 | — |
| `ResolveScenes` | Enabled Build Settings scenes, else `DelegationSmoke.unity` | — |

### Evidence highlights

- **API supports both OS players**; documented CI example defaults to **Linux64 + IL2CPP** (`unity/ProjectAegis/README.md`, `BuildPlayer.cs` header).
- **Editor menu asymmetry:** only `Build/Build Linux64 Player (Mono)` — no Windows/OSX menu items.
- **No automated player build** in Buildkite or GHA (`unity-ci.yml` is manual **test runner only**, `runs-on: ubuntu-latest`, skips without `UNITY_LICENSE`).
- **Windows-only Unity paths** in Editor automation:

```powershell
$unityExe = "C:\Program Files\Unity\Hub\Editor\$UnityVersion\Editor\Unity.exe"
```

  — `tools/unity/Invoke-C2PlayModeSignoffBatch.ps1`, `Invoke-DelegationSmokeSceneSetup.ps1`.

- **Project settings:** Built-in RP (`m_CustomRenderPipeline: {fileID: 0}`), legacy Input Manager (`activeInputHandler: 0`) — OS-independent policy.
- **Empty `EditorBuildSettings` scenes** (`m_Scenes: []`) — players fall back to smoke scene; Linux and Windows players would share that fallback today.
- **Agent routing:** live Editor PNG evidence is local-only; cloud/Linux agents use the headless Play Mode proxy (`production/agentic/local-cloud-agent-routing.md`, `AGENTS.md`).

---

## 4. Tooling OS asymmetry

| Category | Count / pattern |
|----------|-----------------|
| `tools/**/*.ps1` | 17 |
| `tools/**/*.sh` | 31 |
| Dual twins | Essentially `git-workflow`, Hindsight invoke/test |
| PS1-critical | `verify-ci-local`, `copy-delegation-assemblies`, `init-unity-project`, Unity Invoke-* |
| SH-critical | Entire Buildkite agent stack |

Additional asymmetries:

- **`.gitattributes`** forces LF only for `*.sh` and `.buildkite/*` — not `*.ps1` / general text → CRLF risk on Windows checkouts.
- **Unity CI plugin copy** step uses `shell: pwsh` even on `ubuntu-latest` (`.github/workflows/unity-ci.yml`).
- **Mission Editor MCP** tools assume `pwsh` (`tools/mission-editor/mcp-tools.json`).
- Docs often show `.\tools\…ps1` and Windows Unity paths; Linux Cloud docs correctly emphasize bash + headless `dotnet`.

---

## 5. Source-level portability

Searched `src/` for `RuntimeInformation`, `OSPlatform`, `IsLinux`/`IsWindows`, `UNITY_STANDALONE_*`, `RuntimeIdentifier`, `win-x64`/`linux-x64`.

| Finding | Detail |
|---------|--------|
| **No OS platform branching in `src/`** | Zero matches |
| Unity `#if` usage | `UNITY_EDITOR`, `UNITY_5_3_OR_NEWER`, `CESIUM_FOR_UNITY` — not OS-specific |
| Path handling | `Path.Combine` / repo-relative — portable |
| TFMs | `net8.0` / `netstandard2.1`; **no** RID-specific csproj packaging for the game sim |
| SQLite | `Microsoft.Data.Sqlite` brings RID-native assets via NuGet |

**Conclusion:** under `dotnet build` / `dotnet test`, Linux and Windows should produce equivalent headless behavior. Shipping differences are packaging/host, not sim logic forks.

---

## 6. Gap register

| ID | Gap | Severity | Evidence | Remediation |
|----|-----|----------|----------|-------------|
| G1 | No automated Unity **player** build (Linux or Windows) in CI | **High** | No Buildkite/`unity-builder` step; `BuildPlayer.PerformBuild` unused by CI | Add licensed Linux Editor (or game-ci) step for `StandaloneLinux64`; optional `StandaloneWindows64` artifact job |
| G2 | Editor batch scripts hardcode Windows `Unity.exe` | **High** | `tools/unity/Invoke-*.ps1` | Resolve via `UNITY_EDITOR` env / Hub / Linux `/opt` / macOS `.app` |
| G3 | Windows local CI mirror incomplete vs Linux gate | **Medium** | `verify-ci-local.ps1` vs `dotnet-ci.sh` | Add hash + bridge greps (and document gitleaks as optional), or declare bash `dotnet-ci.sh` canonical on all OSes |
| G4 | Plugin / Unity scaffold PowerShell-only | **Medium** | `copy-delegation-assemblies.ps1`, `init-unity-project.ps1`; GHA uses `pwsh` on Ubuntu | Add `.sh` twins or a `dotnet` MSBuild target so Linux clones boot without PowerShell |
| G5 | No Windows Buildkite agent / no OS build matrix | **Medium** | Single Linux hosted pipeline | Document intentional Linux-only headless authority, **or** add scheduled `win-x64` smoke if Windows shipping is required |
| G6 | Player menu / backend defaults asymmetric | **Low** | Menu = Linux Mono only; docs default IL2CPP | Add Win64/OSX menu items; pin release backend in docs/ProjectSettings |
| G7 | `.gitattributes` LF scope narrow | **Low** | `.gitattributes` | Extend `eol=lf` to `*.ps1` (and ideally `*.cs`) |
| G8 | Mission Editor MCP assumes `pwsh` | **Low** | `mcp-tools.json` | Bash entrypoints or document `pwsh` required on Linux |
| G9 | Empty Editor build scene list | **Medium** (player) | `EditorBuildSettings.asset` | Commit enabled scenes for release so Linux/Windows players match |
| G10 | Stale player-build docs in older reports | **Low** | Historical `BuildLinux64` naming | Sync to `PerformBuild` / `BuildLinux64Menu` |
| G11 | Unity Editor CI never builds player; license-gated | **Medium** | `unity-ci.yml` | Fund license + builder, or keep Editor non-blocking and state ship-build policy explicitly |
| G12 | Gitleaks Linux-only binary fetch | **Low** | `agent-gitleaks.sh` | Fine for Buildkite; need Windows artifact if Win CI added |
| G13 | Absolute path folklore in plans/QA | **Low** | `/home/username01/…`, `D:\…`, `C:\Program Files\Unity\…` | Prefer `$REPO_ROOT` / env-resolved Unity |

---

## 7. Priority recommendations

1. **Keep Linux Buildkite as headless authority** — aligns with GitNexus CRITICAL risk on `DelegationBridge` and portable `src/`.
2. **Close G3** — make `verify-ci-local.ps1` call the same hash/bridge checks as `dotnet-ci.sh` (cheap, high clarity).
3. **Close G2** — unblock Linux (and non-default Windows) Editor automation without path edits.
4. **Decide ship policy for G1/G5/G11** — either (a) document “headless Linux = merge; player builds are manual/release-train only,” or (b) add a Linux64 IL2CPP player job as a soft then hard gate.
5. **Close G4/G9** before treating either OS player as release-ready.

---

## 8. What is *not* a gap

- Determinism / Baltic v2 replay hash path — identical under Linux CI and Windows `dotnet test` when the same filters run.
- Core ROE/delegation/sim logic OS forks — none found in source or GitNexus.
- Built-in RP + legacy Input Manager — shared project policy, not an OS split.
- Cursor Cloud / agentic headless path — intentionally Linux `dotnet`-centric and documented.

---

## 9. References

| Artifact | Path |
|----------|------|
| Buildkite pipeline | `.buildkite/pipeline.yml` |
| Linux CI script | `tools/buildkite/dotnet-ci.sh` |
| Windows local mirror | `tools/verify-ci-local.ps1` |
| Player builder | `unity/ProjectAegis/Assets/Editor/BuildPlayer.cs` |
| Unity CI (manual) | `.github/workflows/unity-ci.yml` |
| Buildkite docs | `docs/engineering/buildkite-ci.md` |
| Local Linux IDE notes | `docs/engineering/local-dev-environment.md` |
| Cloud / agent routing | `AGENTS.md`, `production/agentic/local-cloud-agent-routing.md` |

---

*Generated 2026-07-20 via Superpowers-structured evaluation and GitNexus analyze/query/context/impact on commit `adf3748`.*
