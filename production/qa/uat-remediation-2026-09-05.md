# UAT remediation — startup, selection, and command feedback

Date: 2026-09-05. Project Aegis, `DelegationSmoke`, `baltic-patrol-classify`.

The three focused acceptance criteria passed after remediation. Live mouse testing used Computer Use in Unity 6000.3.22f1, with Unity MCP providing test execution, Console evidence, and Game view screenshots. The invalid-command branch was verified by an automated Unity host test through the real facade; it was not exercised by a mouse-accessible invalid menu item.

## Scope and implementation

Base commit: `46485600b180577f7739b40b6ef21c5e129fc239`. .NET SDK: 8.0.400. Three agents worked in isolated, detached worktrees (`.worktrees/uat-startup`, `uat-selection`, `uat-command`); the coordinator integrated the scoped changes and exclusively controlled the Editor. Changes remain uncommitted.

| Area | Observed defect | Remediation |
|---|---|---|
| Startup | Top-bar `OnEnable` could read the session before the bridge's `Awake`, producing a null reference. | `C2TopBarPanelHost.Refresh` waits until `bridgeHost.Bridge` is initialized; normal refresh then binds clock controls. |
| Selection | Per-frame OOB rebuilds invalidated label click handling; returning from a contact to the same friendly row failed. | `C2LeftDrawerPanelHost` uses single `ListView.selectionChanged` and silently synchronizes list selection from presentation state, including clearing it for contact selection. |
| Command feedback | The attack handler discarded the enqueue result and failure reason. | `RightUnitPanelHost` displays persistent `QUEUED` or `DENIED` feedback from the existing facade, clears it when selection changes, wraps the label, caches composed text, and permits handler wiring to retry after an initially empty tree. |

GitNexus upstream impact preceded symbol edits. Startup refresh: MEDIUM, six direct callers, zero indexed processes; command and selection impacts: LOW, no indexed affected processes. Unity lifecycle/event invocations are not fully represented by graph caller counts, so live tests were required. Hindsight at localhost:8888 was unavailable.

## Acceptance retest

Live session: approximately 15:51–15:55 CDT. Screenshots are original MCP Game view captures; mouse actions were performed through Computer Use.

| ID | Actual retest | Result and evidence |
|---|---|---|
| UAT-START | Entered Play Mode twice. Confirmed map and top bar loaded; paused the clock, increased speed, and resumed at 2×. | **PASS.** Zero new exceptions since 15:51 CDT. [2× clock](evidence/uat-remediation-2026-09-05/live-startup-2x.png), [repeat startup](evidence/uat-remediation-2026-09-05/live-startup-repeat.png), [filtered exception count](evidence/uat-remediation-2026-09-05/live-exceptions-filtered.json). |
| UAT-SELECT | While paused, clicked hostile-1 and u1 in OOB; clicked c1 on the map; clicked the same u1 row again; selected hostile-1 on the map. | **PASS.** OOB highlight, map selection outline, and unit/contact detail agreed. Contact selection cleared the OOB selection; returning to u1 restored unit detail. [Hostile](evidence/uat-remediation-2026-09-05/live-selection-hostile.png), [contact](evidence/uat-remediation-2026-09-05/live-selection-contact.png), [friendly return](evidence/uat-remediation-2026-09-05/live-selection-friendly-return.png). |
| UAT-COMMAND | Mouse-clicked Fire 1 round, then Hold fire. Waited through refreshes. Selected a different unit on the map. Automated Unity test submitted a valid button event and an unknown option to the actual host/facade. | **PASS with coverage qualification.** Mouse actions displayed `QUEUED: Fire 1 round` and `QUEUED: Hold fire`; selection change cleared feedback. Invalid option displayed `DENIED: Unknown command (UNKNOWN_OPTION)` in the Unity test. [Fire](evidence/uat-remediation-2026-09-05/live-command-queued.png), [Hold](evidence/uat-remediation-2026-09-05/live-command-hold.png), [cleared](evidence/uat-remediation-2026-09-05/live-command-cleared.png), [denial test](evidence/uat-remediation-2026-09-05/command-green.json). |

`QUEUED` means the existing command facade accepted the request into its queue; it does not claim a weapon fired or a target was hit. The test intentionally did not change simulation authority or make the UI invent execution success. The shipped menu does not expose an unknown command, so that denial case uses a test-only private-handler invocation. The available contact was lost/stale and its detail panel correctly displayed technical targetability NO and a reacquisition reason.

## Regression evidence

The four focused Unity tests passed: startup 1, selection 1, command 2. Each area has a recorded failing result before remediation and a passing result after it:

- [Startup RED](evidence/uat-remediation-2026-09-05/startup-red.json) / [GREEN](evidence/uat-remediation-2026-09-05/startup-green.json).
- [Selection RED](evidence/uat-remediation-2026-09-05/selection-red.json) / [GREEN](evidence/uat-remediation-2026-09-05/selection-green.json).
- [Command RED](evidence/uat-remediation-2026-09-05/command-red.json) / [GREEN](evidence/uat-remediation-2026-09-05/command-green.json).

Use `PassedTests`, `FailedTests`, and named `Results` in these files: MCP's `TotalTests: 9` describes discovery and is not the number executed by each filter. Test-fixture issues found during development (UIDocument tree replacement, disposable theme setup, and an ambiguous initial selection) were corrected before the final green results; failing logs were not suppressed.

## Final headless verification

All required gates completed successfully. Counts below are actual executed tests, not the historical minimum floors.

| Gate | Command | Result |
|---|---|---|
| Debug build | `dotnet build ProjectAegis.sln -v minimal` | **PASS**, zero warnings/errors. [Log](evidence/uat-remediation-2026-09-05/build.txt). |
| Debug full suite | `dotnet test ProjectAegis.sln -v minimal --no-build` | **PASS**, 3,096 passed, zero failed/skipped. [Log](evidence/uat-remediation-2026-09-05/solution-tests.txt). |
| Smoke/replay/performance subset | `dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --no-build --filter 'PlayModeSmokeHarnessTests|ReplayGolden|C2PanelPerfBenchTests' -v minimal` | **PASS**, 42 passed. [Log](evidence/uat-remediation-2026-09-05/smoke-replay-perf.txt). |
| Repository Release verification | `.\tools\verify-ci-local.ps1` | **PASS**, catalog import subset 67/67; Release build zero warnings/errors; full Release suite 3,096/3,096; ReplayGolden 6/6; PlayModeSmokeHarness 24/24. [Log](evidence/uat-remediation-2026-09-05/verify-ci-local.txt). |
| Scope/invariants | Scoped `git diff --check`; hash search; zero-touch diff inspection | **PASS**, clean scoped diff, hash `17144800277401907079` preserved, zero changed bridge/write-gate/golden/policy paths. [Recorded checks](evidence/uat-remediation-2026-09-05/invariants.json). |

The prior Debug run's single generated-define failure is retained as [diagnostic evidence](evidence/uat-remediation-2026-09-05/solution-tests-editor-define-failure.txt), followed by the successful full rerun. The checked-in verification script's actual steps were run and inspected; no separate secret-scan execution is claimed.

The eight-file remediation payload is available as an [uncommitted patch](evidence/uat-remediation-2026-09-05/remediation.patch). Planning/checklist record: [implementation plan](../../docs/superpowers/plans/2026-09-05-uat-remediation.md).

## Tooling and retained limitations

- The live project already had Unity MCP 0.90.0, generated skills/import metadata, and other working changes before remediation. Those changes were preserved and are outside the eight-file code/test payload.
- MCP `console-clear-logs` hit a file-lock exception. The Console was cleared through the Unity UI instead. MCP retained historical logs; the report explicitly filters by the live retest start time. No new product exception occurred during either live startup. Editor account/connectivity and MCP disposal warnings remained.
- Opening the Editor regenerated `UNITY_MCP_READY` player defines, causing the structural headless isolation test to fail. The existing cleanup helper removed 18 targets but missed Server; the resolver re-added defines during compilation. After closing the Editor, only generated `UNITY_MCP_READY;` tokens were removed from ProjectSettings, retaining other settings. Reopening the Editor can reproduce this tooling configuration drift; it is not a runtime remediation change.
- Unity was closed after evidence capture. No scene/prefab edits, package upgrades, commits, or publication were performed as part of the remediation payload.
- Small text, general clipping, empty mission content, and broader scenario correctness remain outside these three acceptance criteria. This is not a full game release certification or a standalone-player test.

## unity-csharp-architect — PR finish (UCA-M4)

Checklist: [pr-finish.md](../../production/agentic/skills/unity-csharp-architect/checklists/pr-finish.md). Skill: [unity-csharp-architect](../../production/agentic/skills/unity-csharp-architect/SKILL.md).

ADRs: [ADR-010 §§2–3](../../docs/architecture/adr-010-headless-first-command-driven-ui.md), [ADR-007](../../docs/architecture/adr-007-c2-map-presentation.md), [ADR-001](../../docs/architecture/adr-001-sim-assembly-boundary.md).

**Verdict: PASS.** All applicable checklist items passed; N/A items are explained below.

- Presentation reads remain through existing bridge/projection contracts; OOB selection remains presentation state. No DecisionLog or world-truth writes.
- Command feedback consumes the existing `TrySelectAttackOption` facade result; no UI-to-order-sink bypass. Startup and selection are pure presentation and do not enter the command/replay stream.
- Existing assemblies only. `DelegationBridge.cs`, CatalogWriteGate paths, v2/v3 policies, and replay goldens are untouched. No public adapter API changes; plugin DLL refresh N/A.
- Hosts remain thin; no new Find/Resources calls, mutable authority singletons, or per-frame feedback string allocation. Selection uses a cached one-element array and no-notify synchronization.
- Editor authoring and new asmdef topology N/A: no authoring/assembly changes. Catalog/Excel ADR additions N/A: no catalog or authoring changes.
- Headless tests preceded last-mile Unity tests. The C2 performance subset was rerun alongside smoke/replay tests. New Unity tests are lifecycle/event/label tests, not substitutes for facade/domain proof.
- Independent startup-agent review approved the final integrated runtime diffs and report, including the handler-wiring retry and selection fixture theme cleanup; coordinator inspected the integrated diff and live behavior. No architecture waiver requested.
