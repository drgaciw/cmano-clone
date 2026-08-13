# Local CI verification — reproducing the blocking `.NET` gate before you push

> **Scope.** The developer runbook for running the **same gate Buildkite runs**, locally, before you
> push or submit. Two scripts mirror [`.buildkite/pipeline.yml`](../../.buildkite/pipeline.yml) step
> for step:
>
> - [`tools/verify-ci-local.ps1`](../../tools/verify-ci-local.ps1) — PowerShell (Windows / `pwsh`).
> - [`tools/buildkite/dotnet-ci.sh`](../../tools/buildkite/dotnet-ci.sh) — bash (Linux / macOS / Cloud
>   agents; the script Buildkite actually invokes).
>
> Both are self-declared mirrors of each other and of the pipeline, so a local green run is a strong
> predictor of a green CI run. This page documents **what each step does, the prerequisites, and the
> common pitfalls** — it is the runbook companion to [buildkite-ci.md](buildkite-ci.md) (the pipeline
> itself) and [ci-and-branch-protection.md](ci-and-branch-protection.md) (the merge-gate model). It
> does not redefine the gate; `buildkite/cmano-clone` remains the merge authority.
>
> **Authoritative thresholds live elsewhere.** The pass/fail *numbers* (solution test floor,
> ReplayGolden count, PlayModeSmoke count, the replay hash) are governed by
> [`AGENTS.md`](../../AGENTS.md) and the active
> [release-train scope boundary](../../production/release-train-scope-boundary-2026-06-24.md). The
> counts embedded in the two scripts' header comments are **point-in-time** (written at S35/S67) and
> are not the current floor — always treat AGENTS.md as the source of truth.

---

## TL;DR

```bash
# Linux / macOS / Cloud agent (what Buildkite runs)
bash tools/buildkite/dotnet-ci.sh
```

```powershell
# Windows / pwsh
.\tools\verify-ci-local.ps1
```

A successful run ends with `=== PASS ===`. Any non-zero exit means the gate would fail in CI — fix
before pushing. Neither script mutates anything but the build output and (bash only) the gitignored
Unity plugin DLLs.

---

## Prerequisites

| Requirement | Notes |
|-------------|-------|
| **.NET SDK 8.0.400** | Pinned by [`global.json`](../../global.json). If missing: `curl -sSL https://dot.net/v1/dotnet-install.sh \| bash /dev/stdin --version 8.0.400` then `export PATH="$HOME/.dotnet:$PATH"`. The bash script re-applies this PATH via [`tools/buildkite/agent-bootstrap-dotnet.sh`](../../tools/buildkite/agent-bootstrap-dotnet.sh) so spawned test hosts inherit the SDK. |
| **Shell** | `pwsh` for the `.ps1`; any POSIX `bash` for the `.sh`. Use the bash script when PowerShell is unavailable (e.g. Cloud agents). |
| **Node** *(optional)* | Only needed for the GitNexus reindex/annotation steps (see [buildkite-ci.md](buildkite-ci.md)); the `.NET` gate itself does not require it. The bash script logs whether `node` is present but does not fail on its absence. |
| **Run from repo root** | Both scripts resolve the repo root themselves (`$PSScriptRoot` / `BASH_SOURCE`) and `Push-Location`/`cd` there, so they are safe to invoke by path. |

---

## What the gate runs, in order

Both scripts build **Release** and share the same spine. The PowerShell script is the lean core; the
bash script adds the CI-only steps (plugin staging, verification-before hash/bridge echo).

| # | Step | `verify-ci-local.ps1` | `dotnet-ci.sh` | Purpose |
|---|------|:---------------------:|:--------------:|---------|
| 1 | `dotnet restore ProjectAegis.sln` | ✔ | ✔ | Restore packages. |
| 2 | **Catalog policy gate** | ✔ (calls [`scripts/verify-catalog-import.ps1`](../../scripts/verify-catalog-import.ps1)) | ✔ (inline bash parity) | Fail if any `*.db3` is tracked (proprietary CMO DB policy), then run the `CmoMarkdown` import tests. |
| 3 | `dotnet build ProjectAegis.sln -c Release --no-restore` | ✔ | ✔ | Expect **0 errors / 0 warnings**. |
| 4 | **Unity plugin DLL staging** | — | ✔ ([`tools/copy-delegation-assemblies.sh`](../../tools/copy-delegation-assemblies.sh)) | Stage the `netstandard2.1` plugin DLLs a clean checkout lacks (they are gitignored; only `.meta` is tracked) so `UnityPluginEpicATypesTests` does not fail CI-only. On Windows run [`tools/copy-delegation-assemblies.ps1`](../../tools/copy-delegation-assemblies.ps1) if that test fails locally. |
| 5 | `dotnet test ProjectAegis.sln -c Release --no-build -v minimal` | ✔ | ✔ | The full solution suite (see AGENTS.md for the current floor). |
| 6 | `--filter FullyQualifiedName~ReplayGoldenSuiteTests` | ✔ | ✔ | The Baltic v2 replay golden suite (**6/6**). |
| 7 | `--filter FullyQualifiedName~PlayModeSmokeHarnessTests` | ✔ | ✔ | The headless C2 Play Mode smoke proxy. |
| 8 | **verification-before hash + bridge** | — | ✔ | Grep the immutable replay hash `17144800277401907079` and count `DelegationBridge` source refs (the ZERO-behaviour-change invariant), citing the scope boundary. Echo-only — does not itself fail the build. |

The two `--filter` runs (6, 7) use `--no-build` against the Release output from step 3, so they are
fast re-runs of specific suites, not full rebuilds.

> **Why `verify-catalog-import` first?** It is the cheapest fail-closed gate (a `git ls-files '*.db3'`
> check + a narrow test filter), so a policy violation aborts before the expensive build/test. See
> [dual-track-cmo-analysis-and-catalog.md](dual-track-cmo-analysis-and-catalog.md) for the `.db3`
> policy it enforces.

---

## Faster inner loop

The full gate is Release + every suite. While iterating you usually want the quicker Debug commands
from [`AGENTS.md`](../../AGENTS.md):

```bash
dotnet build ProjectAegis.sln
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter PlayModeSmokeHarnessTests
```

Run the **full** `verify-ci-local.ps1` / `dotnet-ci.sh` (Release + ReplayGolden + PlayModeSmoke) as
the last step before you push — Release config and the golden/smoke filters catch failures the Debug
inner loop can miss.

---

## Troubleshooting

| Symptom | Fix |
|---------|-----|
| `dotnet: command not found` / wrong SDK | Install SDK **8.0.400** to `~/.dotnet` and add it to `PATH` (see Prerequisites); confirm `dotnet --version` prints `8.0.400`. |
| `pwsh` unavailable | Run the bash mirror: `bash tools/buildkite/dotnet-ci.sh`. |
| `UnityPluginEpicATypesTests` fails with *"Missing plugin DLL — run tools/copy-delegation-assemblies.ps1"* | The gitignored plugin DLLs are not staged. The bash gate stages them automatically (step 4); locally run `tools/copy-delegation-assemblies.ps1` (or `.sh`) before the test. |
| `FAIL: tracked *.db3 files` | A proprietary CMO DB was committed. Remove it — catalog is fed via markdown/fixtures only ([dual-track](dual-track-cmo-analysis-and-catalog.md)). |
| Build reports warnings | The gate expects **0 warnings**; treat warnings as failures. Do not override `Directory.Build.props` (`ProduceReferenceAssembly=false` prevents CS0006 on parallel builds). |
| Test count / ReplayGolden / PlayModeSmoke below floor | Compare against **AGENTS.md** and the scope boundary, not the scripts' header comments (which are historical). A regression below the monotonic floor must be fixed, not the floor lowered. |
| Replay hash grep is empty / changed | The Baltic v2 hash `17144800277401907079` must stay present in `tests/` / `data/`; a change means a determinism regression (see [determinism-and-replay.md](determinism-and-replay.md)). |
| `dotnet format` noise | Known pre-existing whitespace in `ProjectAegis.Delegation.Demo/Program.cs`; unrelated to the gate. |

---

## See also

| Doc | For |
|-----|-----|
| [buildkite-ci.md](buildkite-ci.md) | The Buildkite pipeline these scripts mirror (steps, plugins, cutover). |
| [ci-and-branch-protection.md](ci-and-branch-protection.md) | The merge-gate model — why `buildkite/cmano-clone` is the required status and local parity is advisory. |
| [determinism-and-replay.md](determinism-and-replay.md) | The replay hash / golden invariant the gate re-checks. |
| [dual-track-cmo-analysis-and-catalog.md](dual-track-cmo-analysis-and-catalog.md) | The `*.db3` policy enforced by the catalog-import step. |
| [`AGENTS.md`](../../AGENTS.md) | The authoritative build/test commands and current gate thresholds. |
