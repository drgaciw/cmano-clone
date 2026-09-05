# Slice A contact UI implementation evidence — 2026-09-05

## Scope and authority

Branch: `codex/slice-a-completion`, based on `origin/main` at `f6d14769`.
User delegated the remaining UI implementation decisions, including proceeding before DRG-208 owner Play Mode signoff. This does not manufacture Play Mode evidence or close that acceptance gate.

Requirements reviewed:

- [Combat Interaction UX / Slice A](https://app.notion.com/p/3c1f7cb4e4df810c96b0ea6e32cc6933).
- [Any Sensor / Best Shooter requirements](https://app.notion.com/p/3c1f7cb4e4df8119a7c7c5ae46b258e4).
- Existing DRG-179/206/207/209 headless projection contracts on main.

Linear issue retrieval failed with `oauth_token_invalid_grant`; tracker status was not changed. Hindsight localhost:8888 was unavailable. GitNexus was refreshed locally; the MCP process continued serving an older index, so local CLI impact/detect-changes supplied the useful analysis.

## Implemented

- `SliceAContactFrameBridge`: tick-level, read-only contact/kill-chain/provenance/shooter/authority frame. Multi-observer target BDA folds without a target-ID dictionary collision; degraded and lost identities remain visible.
- `SliceAContactPresenter`: phase, source, confidence, freshness/comms, technical chain, separate release authority, and actionable explanation. Missing evidence is unknown, never permission.
- `DelegationBridgeHost`: one projection call after each simulation tick; `DelegationBridge.cs` untouched.
- `ContactDetailPanelHost`: binds the cached frame only when frame or selection changes; no per-render-frame log projection. Scrollable details and independent technical/authority foldouts.
- Committed smoke scene references the panel host, bridge, UXML and USS. Existing scene builder already supports these assets.
- 33 new headless tests: 15 frame, 14 presenter, 4 source/asset wiring contracts. Added a Unity lifecycle/cache/selection smoke test, not executed here.

## Explicitly not complete

The existing `SimplePlayModeSimHost` does not supply authoritative current per-shooter eligibility or actor/contact authority. It therefore displays sensing data and UNKNOWN shooter/authority state. The adapter accepts `ISensorToShooterShooterSource` plus `ISliceAContactAuthoritySource` on the world snapshot; tests exercise supplied evidence. A real runtime producer must still provide those facts, including side, current geometry, readiness and commitment. Scenario defaults or historical engagement contexts are not substitutes.

The first technically eligible shooter is the existing projection's nomination; this work does not implement authority-aware ranking across all alternative shooters. Full any-sensor/best-shooter scenario acceptance remains open.

Unity Editor was not found in standard installation paths, Unity Hub records or installed-program records. Unity compilation/import, actual layout and Play Mode smoke have not been executed. Headless proxy tests are not DRG-208 signoff.

## Verification

- `dotnet build ProjectAegis.sln --no-restore -v minimal`: 0 warnings, 0 errors.
- `dotnet test ProjectAegis.sln --no-build -v minimal`: 3,095 passed, 0 failed. Breakdown: Sim 577; Delegation 1,091; Data 769; Data.Excel 24; CLI 115; UnityAdapter 519.
- Release full suite: same 3,095 passed, 0 failed.
- `ReplayGoldenSuiteTests`: 6/6; `PlayModeSmokeHarnessTests`: 24/24.
- Combined Slice A / replay / smoke / C2 panel performance filter passed; existing <100 ms bind budget tests remain green.
- Plugin assemblies refreshed from Release `netstandard2.1` with `tools/copy-delegation-assemblies.ps1`. This fixed the initial stale-plugin export test failure. Build outputs are not source changes.
- `git diff --check`: clean. Baltic v2 hash `17144800277401907079` preserved; no golden changes; zero `DelegationBridge.cs` changes.
- Local GitNexus impact: panel Refresh has two direct lifecycle callers, LOW; host RunTick has one direct caller, LOW. Combined tracked-diff detect-changes: HIGH, 16 indexed symbols / 11 flows through the composition host. New untracked files were reviewed and tested separately; this is not a fresh index of the new symbols.
- Installed SDK is 8.0.424, selected by the current `global.json` roll-forward policy; exact 8.0.400 parity was not run.

The first CI-parity invocation overlapped an earlier test process and hit a locked test DLL during the catalog gate. Its printed final PASS was not accepted as complete evidence. The serial `tools/verify-ci-local.ps1` rerun passed: catalog 67/67, Release build 0 warnings/errors, full suite 3,095/3,095, replay 6/6, proxy smoke 24/24. Every result was read rather than relying solely on the script's final banner.

## unity-csharp-architect — PR finish

**Checklist:** `production/agentic/skills/unity-csharp-architect/checklists/pr-finish.md`

**Skill:** unity-csharp-architect

**ADRs:** ADR-010, ADR-007, ADR-001; ADR-006 for read-only catalog access.

**Verdict:** BLOCKED

**Evidence:**

- Presentation reads: immutable/read-only frame and projection outputs; no UI writes to simulation/log.
- Command path: N/A, inspect-only explanation. Existing command workflow remains authoritative; no new firing controls.
- Assemblies: existing adapter/runtime/test assemblies; no new dependency edges; DelegationBridge zero-touch.
- MB / DI / alloc: serialized composition reference, cached labels, dirty frame/selection binding; no Find/Resources calls.
- Editor: no authoring changes; saved scene asset references statically tested but not imported in Editor.
- Tests: headless proof above; Unity last-mile test added but unexecuted.
- Plugins: netstandard2.1 refreshed; no net8.0 DLLs copied into Unity.
- Review: approved as partial UI implementation after scene-wiring and BDA corrections. Full Slice A acceptance remains blocked by runtime source integration and actual Unity evidence.

**Handoff:** implement the live evidence producer, exercise multi-sensor / unauthorized / unavailable / eligible alternatives and stale-link transitions in Unity, then close DRG-208 and refresh Linear status after authentication. Do not mark Slice A Done from this report.

No commits, submissions or tracker mutations were made for this implementation. Existing housekeeping stashes were preserved. The working tree intentionally contains the implementation for review.
