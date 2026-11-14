# Project Aegis .NET 11 upgrade plan

Move the headless build from SDK **10.0.403** and **net10.0** to stable SDK **11.0.100** and **net11.0**. Preserve C# 12, the four Unity-facing **netstandard2.1** targets, the 10.0.x package pins those targets use, and the existing Unity Editor and package pins. [.NET 11 release](https://dotnet.microsoft.com/en-us/download/dotnet/11.0)

Revision 2, 2026-11-12. Checked against `origin/main` at `2ddbd393` and the .NET 11 GA breaking-change list. The main change from revision 1: the migration ships as a three-PR stack instead of one PR, so a replay or test difference can be attributed to the compiler, the runtime, or the inspection tooling. [Review record](#review-record) lists every change.

## Why now, and the exit

.NET 11 is a standard-term release supported for two years. .NET 10 (LTS) is supported for three, so both leave support in November 2028 and moving costs no support time. It does commit the project to adopting .NET 12 (LTS, due November 2027) before that date. [Support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core)

## Decision before starting: `rollForward`

`global.json` uses `rollForward: latestMajor`, and [agent-bootstrap-dotnet.sh](../../../tools/buildkite/agent-bootstrap-dotnet.sh) documents that as intentional. Two consequences:

- Any agent or workstation with SDK 11 installed has been building `main` with it since 2026-11-10. `actions/setup-dotnet` reads `global.json`, so GitHub CI may already be on SDK 11 while Buildkite (installs 10.0.403 only when nothing satisfies the pin) and ReSharper (requires exactly 10.0.403) are not. Confirm from a recent CI log before treating SDK 10 as the baseline.
- After this upgrade the same setting will select .NET 12 preview SDKs wherever they are installed, from early 2027.

Recommended: change to `latestFeature`, which stays on 11.0.x and still takes SDK servicing updates. If the owner keeps `latestMajor`, record that the pin is a minimum, not a version. Either way, make [dotnet-ci.sh](../../../tools/buildkite/dotnet-ci.sh) print `dotnet --version`; today it prints only the path.

## 0. Baseline (before any change)

Capture on `main` with SDK 10.0.403 forced, on one fixed machine:

- Full-suite pass count and test count. The floor is 1,638; the 2026-11-01 dispatch measured 4,058.
- Build warning count. No project sets `TreatWarningsAsErrors` or `AnalysisLevel`, so "zero warnings" is currently unenforced.
- ReplayGolden 6/6, Baltic v2 hash `17144800277401907079`, v3 goldens, PlayModeSmoke count.
- File list and count of the staged Unity plug-in DLLs from [copy-delegation-assemblies.sh](../../../tools/copy-delegation-assemblies.sh).
- ReSharper SARIF from CLI 2026.2.1.
- CPU flags on every Buildkite agent and self-hosted runner: `grep -m1 -oE 'sse4_2|popcnt' /proc/cpuinfo`. .NET 11 refuses to start below x86-64-v2.

## 1. PR 1: SDK and tooling pins (still `net10.0`)

Isolates compiler, analyzer, MSBuild, and NuGet changes. The runtime does not change.

- Generate the `global.json` bump with Renovate. In [renovate.json](../../../renovate.json), add a narrow exception after the existing major-update block: match the NuGet manager's `dotnet-sdk` dependency type in the root `global.json`, restrict candidates to stable 11.0.x, enable the major with Dependency Dashboard approval, disable automerge, and clear inherited grouping. Renovate **44.236.4** validated this locally and returned **10.0.403 → 11.0.100** on branch `renovate/dotnet-sdk-11.x`. The local lookup creates no branch; approve in the hosted dashboard. [NuGet manager](https://docs.renovatebot.com/modules/manager/nuget/) · [Local lookup](https://docs.renovatebot.com/modules/platform/local/) · [Dependency Dashboard](https://docs.renovatebot.com/key-concepts/dashboard/)
- Keep the 11.0.x restriction in place after merge so .NET 12 stays gated.
- Move every hard-coded SDK pin in the same PR. These fail or silently diverge otherwise:
  - [.cursor/cloud-install.sh](../../../.cursor/cloud-install.sh) `DOTNET_VERSION`
  - [agent-bootstrap-dotnet.sh](../../../tools/buildkite/agent-bootstrap-dotnet.sh) `PINNED_SDK` and its header comment
  - [resharper.yml](../../../.github/workflows/resharper.yml) `-Version 10.0.403`
  - [tools/resharper/](../../../tools/resharper/): `inspect.ps1` (exact-match check and `--dotnetcoresdk`), `README.md`, `ledger.json` `sdkVersion`, and the `test_inspect_wrapper.py` fixture
- Leave ReSharper CLI at 2026.2.1 here if it runs on SDK 11. If it does not, pull the CLI bump forward from PR 3.

Exit: all gates in section 4 green on `net10.0` built by SDK 11.0.100; warning count compared with baseline; replay hash unchanged.

## 2. PR 2: retarget to `net11.0`

Isolates runtime and base-library behavior.

- Change `net10.0` to `net11.0` in all 16 project files. Keep `netstandard2.1` in Data, Sim, Delegation, and Delegation.UnityAdapter. Keep `LangVersion` 12.0 everywhere.
- `tools/cmano-db-crawler/import-qa-slice/ImportQaSlice.csproj` is not in `ProjectAegis.sln`. The solution gates never build it. Build and run it explicitly.
- Conditional compilation needs no edits: the source uses only `NET10_0_OR_GREATER`, `NET8_0_OR_GREATER`, and `NETSTANDARD2_1`; csproj conditions test only `netstandard2.1`.
- Leave package versions alone. `Microsoft.Data.Sqlite`, `System.Text.Json`, and `System.Net.Http.Json` stay at 10.0.14. The latter two are referenced only for `netstandard2.1`, so holding them keeps the Unity plug-in dependency set unchanged. Renovate majors are already disabled; note this hold in `renovate.json` so nobody "fixes" it.
- Update the comment in [Directory.Build.props](../../../Directory.Build.props) that describes what `latest` means on the current SDK.
- Do not enable Runtime Async. It is opt-in at GA through `<Features>runtime-async=on</Features>`. The runtime libraries already use it, so library async behavior changes with the retarget regardless.

Exit: all gates in section 4 green; replay hash and goldens identical to baseline; plug-in DLL list identical to baseline.

## 3. PR 3: inspection baseline and documentation

- Validate ReSharper CLI **2026.2.1 → 2026.3.0** against the retargeted solution. Review SARIF differences before updating the baseline or its metadata. Update the exact-version check in `inspect.ps1`. [JetBrains .NET 11 support](https://www.jetbrains.com/resharper/whatsnew/) · [ReSharper CLI 2026.3.0](https://www.nuget.org/packages/JetBrains.ReSharper.GlobalTools/2026.3.0)
- Update live setup documentation: `AGENTS.md` (overview, prerequisite block, cloud-agent section, troubleshooting table), `README.md`, `Tech-Stack.md`, `docs/engine-reference/dotnet/README.md`, and the READMEs under `src/ProjectAegis.Data`, `.Data.Excel`, `.Delegation`, `.Sim`, `.MissionEditor.Cli`.
- Leave dated records alone: ADR "Engine Compatibility" fields, sprint reports, QA evidence.
- Update [ADR-022](../../architecture/adr-022-target-os-and-cpu-architectures.md) with the x86-64-v2 minimum.
- Completeness check: `git grep -nE '10\.0\.403|net10\.0' -- . ':!docs/reports' ':!production' ':!docs/superpowers'` returns only intentional history.

## 4. Compatibility evidence

GitNexus marked `DelegationBridge` and `CatalogWriteGate` as critical hubs on a stale index. Refresh it with the [safe reindex procedure](../../engineering/gitnexus-index-health.md) before any symbol edit. This migration should need none. Run `detect_changes` before each commit.

| Risk | Where it bites in this repository | Required evidence |
| --- | --- | --- |
| SDK, restore, test discovery | xUnit 2.9.3 and NUnit 5.0.0 on Test SDK 18.11.0; VSTest drops its Newtonsoft.Json dependency (no project uses it) | Restore and build clean. Full suite: zero failures, at least 1,638 passes, test count not below baseline. |
| New analyzer warnings | `net11.0` raises the default analysis level | Warning count not above baseline, or each new warning fixed or individually justified. No blanket `NoWarn`. |
| Rounding | `Math.Round(value, digits)` is now correctly rounded. Used in `ValidationRules.cs` (range-excess messages) and `PlatformRelativeScaler.cs` (proposed position, speed, range). Not in tick paths. | Validation-message and platform-proposal tests pass unchanged. Diff one generated workbook against baseline. |
| Float-to-small-integer casts | Unchecked `float`/`double` to `byte`/`short`/`char` now saturate. A pattern search found none in Sim, Delegation, or Data; confirm with an analyzer pass. | Replay evidence below. |
| Replay determinism | Sim and Delegation | ReplayGolden 6/6, Baltic v2 hash unchanged, v3 goldens unchanged. Strongest proof: in PR 1, temporarily multi-target the replay test project to `net10.0;net11.0` and show equal fingerprints from one commit on one machine. |
| ZIP | `AegisScenarioPackage` sync and async paths use `ZipFile.OpenAsync` / `OpenReadAsync`. .NET 11 adds CRC32 validation on read and loads entries eagerly on async open, so corrupt packages now fail at open. | Open every checked-in scenario package through both paths. Add one corrupt-CRC fixture and assert the error is reported cleanly. |
| Workbooks | ClosedXML 0.105.1 over System.IO.Packaging; `DeflateStream` now writes headers for empty payloads | Platform export, import, and diff round-trip tests pass. Check which System.IO.Packaging version restore resolves; the non-seekable `PackagePart.GetStream` change applies only if it is 11.x. |
| Unity plug-ins | `netstandard2.1` output is now compiled by the SDK 11 compiler | Stage with the existing copy script; DLL list equals baseline. PlayModeSmokeHarness at least 20 passes, zero failures. Unity import on the pinned Editor in CI. |
| CI environment | x86-64-v2 minimum; `dotnet test` needs a writable artifacts directory in containers; NuGet lock directory moved on Linux | Baseline CPU check passed. Buildkite and GitHub gates green. [verify-ci-local.ps1](../../../tools/verify-ci-local.ps1) green. |
| Inspection parity | ReSharper wrapper requires exact SDK and CLI versions | SARIF comparison reviewed in PR 3. Record the Unity/C# PR-finish verdict under ADR-010 §2–3, ADR-007, and ADR-001. |

[.NET 11 breaking changes](https://learn.microsoft.com/dotnet/core/compatibility/11) · [Runtime changes](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/runtime) · [Repository test floor](../../../production/test-floor.json) · [Unity/C# PR-finish checklist](../../../production/agentic/skills/unity-csharp-architect/checklists/pr-finish.md)

## 5. Parallel execution

One Graphite stack, three PRs, but the work inside splits across tracks with disjoint files. A coordinator owns `global.json`, `renovate.json`, and the csproj edits.

| Track | Surface | Can start |
| --- | --- | --- |
| A. Baseline capture | No repository edits | Now |
| B. Renovate rule and SDK pins | `renovate.json`, `global.json`, `.cursor/`, `tools/buildkite/`, `.github/workflows/resharper.yml` | Now |
| C. ReSharper wrapper and fixtures | `tools/resharper/` | Now; baseline update waits for PR 2 |
| D. Compatibility fixtures | New corrupt-ZIP fixture and test; replay dual-target experiment | Now |
| E. Documentation | `AGENTS.md`, READMEs, `Tech-Stack.md`, ADR-022 | After PR 2 is green |

The AI plan's Wave 0 touches only new directories and runs alongside all of these.

## 6. Rollback

- PR 2 reverts cleanly on its own; SDK 11 continues to build `net10.0`.
- PR 1 reverts only together with PR 2. The ReSharper wrapper's exact-version check must revert with `global.json`.
- No data migration is involved. Scenario packages, catalogs, and replay formats are untouched.
- Stop and revert if the Baltic v2 hash or any golden changes and the cause is not understood within the PR.

## Status

Verified so far: the SDK candidate, by Renovate lookup. Branch creation, baseline capture, retargeting, builds, tests, and CI verification are implementation steps.

## Feature integration review

The [AI feature integration plan](2026-11-12-dotnet-11-ai-feature-integration.md) adds evidence export, after-action explanations, structured mission authoring, and evaluated retrieval. Its gate G0 is the exit of PR 2 in this plan. Its Wave 0 (contracts, fake provider, fixtures, architecture gates) does not depend on this upgrade. It retains C# 12 and treats Runtime Async as a separate measured opt-in. GitNexus method impacts are provisional because the index is stale; source-verified contracts and the refresh procedure guide implementation.

## Review record

Revision 2, 2026-11-12. Checked against `origin/main` `2ddbd393`, the .NET 11 breaking-change list, and the GA runtime notes. Not checked: ReSharper CLI 2026.3.0 availability, Renovate rule syntax, actual CI logs.

Corrected:
- Runtime Async is opt-in at GA, not preview.
- "Zero warnings" had no enforcement or baseline; now measured against a baseline.

Added:
- Three-PR stack separating SDK, runtime, and inspection changes; rollback per PR.
- `rollForward` decision and the possibility that some CI already runs SDK 11.
- Baseline capture step, including CPU minimum and plug-in DLL list.
- Full list of hard-coded pins: `inspect.ps1`, `ledger.json`, the wrapper test fixture, bootstrap comments.
- `ImportQaSlice` is outside the solution and needs an explicit build.
- Repository-specific breaking-change rows: rounding call sites, eager async ZIP open, CRC validation, analyzer level, container and NuGet changes.
- Package hold rationale for the `netstandard2.1` targets.
- Documentation scope, completeness grep, ADR-022 update, parallel tracks, support-window rationale.


## DRG-724 Decision ruling (2026-11-14)

Recorded on Linear DRG-724 and Notion DoD rubric:

- `rollForward`: **latestFeature**
- Test floor: raise `production/test-floor.json` `solutionMinPass` to the **DRG-725 measured** full-suite pass count (never below 1,638).
- `global.json`: SDK pin **11.0.100**, `rollForward: latestFeature` (stays on 11.0.x; takes servicing and feature-band updates; never rolls to SDK 12).
- Floor: `solutionMinPass` = **4361** (DRG-725 measured, 2026-11-14, Ubuntu-Host, SDK 10.0.403 baseline; 0 failed). Live docs (`AGENTS.md`, `README.md`, per-assembly READMEs) state 4361; the prior 1,638 floor is history only.
- PR 3b (DRG-742, Track E) updated live docs and [ADR-022](../../architecture/adr-022-target-os-and-cpu-architectures.md) (x86-64-v2 minimum). Dated ADRs, sprint reports, and `docs/reports/` are intentionally left unchanged.
