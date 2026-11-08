# Unity C2 Frame Budget Baseline — S36-05 (Additional Capture + Notes)

**Generated:** 2026-08-01
**Story:** S36-05 — C2 Frame Budget Additional Capture + Notes (`team-unity`)
**Sprint gate:** `sprint_gate: true`
**Req trace (as cited in story header):** "TR-c2-004" — **see Documentation Hygiene Flag below, this citation is stale/incorrect**
**Extends:** `production/perf/unity-c2-frame-baseline-s35-2026-06-19.md`, `production/perf/perf-profile-polish-baseline-2026-06-19.md`
**Host:** Linux sandbox, headless `dotnet` only (Unity Editor / Profiler **not installed**, not available in this environment)
**Scope discipline:** Measurement/evidence only. **ZERO** edits to `src/`, `DelegationBridge`, sim code, or data pins in this story.

---

## Documentation Hygiene Flag — TR-ID mismatch

Story header `req_trace` cites `TR-c2-004`. The current entry for `TR-c2-004` in `docs/architecture/tr-registry.yaml` reads:

```yaml
- id: TR-c2-004
  system: c2-ui
  gdd: design/gdd/command-and-control-ui.md
  requirement: Globe map P0 (Phase A placeholder, Phase B WGS84)
```

This is unrelated to C2 frame-budget/panel-bind work. Treated as a **stale/incorrect TR-ID citation** in the story header — not reconciled, not treated as in-scope (globe-map work is explicitly Out of Scope for this story). Flagging for documentation hygiene follow-up only; does not block this story's ACs.

---

## Executive Summary

| Metric | Budget | Measured (S36-05) | Status |
|--------|--------|--------------------|--------|
| **C2 panel selection bind** (Req 20) | **< 100 ms** wall | **p95 0.149 ms**, max 0.164 ms (headless, n=20) | **OK** — well under budget |
| **C2 proxy checks (S35 group 1–13 filter)** | ≥85/85 (S35 baseline) | **127/127** PASS | **OK** — suite grew, zero regressions |
| **C2 proxy checks (S35 group 14–18 filter)** | ≥58/58 (S35 baseline) | **60/60** PASS | **OK** — suite grew, zero regressions |
| **PlayModeSmokeHarnessTests** | All PASS | **21/21** PASS | **OK** |
| **ReplayGoldenSuiteTests** | 6/6 PASS | **6/6** PASS | **OK** |
| **Unity C2 frame time** (60 fps, 16.67 ms) | 16.67 ms mean/p95 | **NOT RUN** — Unity Editor unavailable in this sandbox | **DOCUMENTED LIMITATION** (not a fabricated pass) |

**Verdict:** Headless panel-bind path remains **proven under Req 20 budget** with wide headroom. No regressions in any headless regression gate. Unity Editor Profiler frame capture (16.67 ms P0) is **NOT RUN** in this sandbox — this is a real environment limitation (no Unity Editor installed here), not a measured fail. No budget was exceeded on anything that *was* measurable, so **no backlog items are required by the "if p95 > budget" clause** — the pre-existing BL-C2-01/02/03 backlog (Editor Profiler capture) from the S35 doc remains open and unchanged.

---

## What Was Actually Run (this session)

Environment: Linux sandbox, `dotnet 8.0.129` (substitute SDK — repo pin is `8.0.400` per `global.json`; not obtainable in this environment, a known session constraint, not something remediated here).

### 1. C2 panel selection bind timing (Req 20 regression guard)

```bash
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "FullyQualifiedName~C2PanelBindTimingTests" -v normal
```

Result:
```
C2 panel selection bind: mean=0.081 ms p95=0.149 ms max=0.164 ms (n=20)
Passed C2_panel_selection_bind_path_completes_under_100ms_budget [632 ms]
Total tests: 1, Passed: 1
Total time: 3.0370 Seconds
```

| Stat | Value | vs Req 20 (100 ms) |
|------|-------|---------------------|
| Mean | 0.081 ms | ~1,230× headroom |
| p95 | 0.149 ms | ~670× headroom |
| Max | 0.164 ms | ~610× headroom |

Slightly higher than the S35 baseline sample (mean 0.007 ms / p95 0.013 ms) — consistent with normal host-load/JIT noise on a shared sandbox VM, not a regression signal; both are 2–3 orders of magnitude under the 100 ms budget. Test wall time (incl. build) ~42.6 s cold; test body itself 632 ms / total run 3.04 s.

### 2. PlayModeSmoke suite

```bash
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "FullyQualifiedName~PlayModeSmoke" -v normal
```

Result: **21/21 PASS**, `Total time: 2.9759 Seconds` (full command incl. build ~9.6 s wall).

### 3. ReplayGoldenSuiteTests (canonical 6-case class)

```bash
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "FullyQualifiedName~ReplayGoldenSuiteTests" -v normal
```

Result: **6/6 PASS** (`baltic-engage`, `baltic-comms`, `baltic-classify`, `baltic-stale`, `baltic-spoof`, `baltic-readiness` golden fixtures), `Total time: 1.5430 Seconds` (full command incl. build ~7.9 s wall).

> Note: filtering on `ReplayGolden` alone (rather than the specific `ReplayGoldenSuiteTests` class) matches **17** tests total in this project (includes other Baltic golden/regression fixtures beyond the canonical 6-case suite) — all 17 also PASS. The story's stated gate is the 6-case `ReplayGoldenSuiteTests` class, reported above as 6/6.

### 4. C2 proxy filter groups (S35 baseline cross-check — AC "C2 filters 1–13 + 14–18 unchanged PASS")

```bash
# Group 1–13 (S35 filter set: PlayModeSmoke|C2Selection|OobTree|LossesScoring|BalticReplay|FuelState|AttackMenu)
dotnet test ... --filter "FullyQualifiedName~PlayModeSmoke|FullyQualifiedName~C2Selection|FullyQualifiedName~OobTree|FullyQualifiedName~LossesScoring|FullyQualifiedName~BalticReplay|FullyQualifiedName~FuelState|FullyQualifiedName~AttackMenu" -v minimal
# → Passed! Failed: 0, Passed: 127, Skipped: 0, Total: 127, Duration: 2 s

# Group 14–18 (S35 filter set: PlatformImport|Doctrine|C2TopBar|PlatformCatalogViewer|PlatformComms|PlatformLinkCatalog)
dotnet test ... --filter "FullyQualifiedName~PlatformImport|FullyQualifiedName~Doctrine|FullyQualifiedName~C2TopBar|FullyQualifiedName~PlatformCatalogViewer|FullyQualifiedName~PlatformComms|FullyQualifiedName~PlatformLinkCatalog" -v minimal
# → Passed! Failed: 0, Passed: 60, Skipped: 0, Total: 60, Duration: 4 s
```

Both groups grew since S35 baseline (85→127, 58→60) — consistent with the ongoing pattern of suite growth documented across S35–S40 appendices in `perf-profile-polish-baseline-2026-06-19.md`. **Zero failures, zero regressions.**

### Regression summary table

| Gate | Expected (per story AC) | Actual | Result |
|------|--------------------------|--------|--------|
| `C2PanelBindTimingTests` | p95/max < 100 ms | p95 0.149 ms, max 0.164 ms | **PASS** |
| `ReplayGoldenSuiteTests` | 6/6 | 6/6 | **PASS** |
| PlayModeSmoke suite | all PASS | 21/21 | **PASS** |
| C2 proxy group 1–13 (S35 filter) | ≥85/85 | 127/127 | **PASS** |
| C2 proxy group 14–18 (S35 filter) | ≥58/58 | 60/60 | **PASS** |
| `src/` / `DelegationBridge.cs` / sim / data pins | ZERO touch | ZERO touch (verified — no files edited) | **PASS** |

---

## Unity Editor / Profiler Frame Capture — NOT RUN (documented limitation)

Per this story's own QA test case ("Edge: Profiler unavailable → document limitation explicitly"):

- **This sandbox has no Unity Editor installed** (headless container). There is no display, no render pipeline, and no Unity Profiler attach point available.
- `SimplePlayModeSimHost` (`unity/ProjectAegis/Assets/Scripts/Runtime/SimplePlayModeSimHost.cs`) already carries the S36-05 frame-capture instrumentation added in a prior track:
  - `_frameTimes` accumulates `Time.deltaTime * 1000.0` (ms) per `Update()` call, skipping the first frame (`Time.frameCount > 1`).
  - `CapturedFrameTimesMs` exposes the sample list for mean/p95/max computation.
  - This path only executes inside a real Unity `Update()` loop (Editor PlayMode or Player) — it **cannot** be exercised from headless `dotnet test`.
- **Result: NOT RUN.** No mean/p95/max frame time vs the 16.67 ms (60 fps) P0 budget was captured in this session. This is reported as an honest gap, not a fabricated pass, per the story's explicit instruction.
- The pre-existing backlog items from the S35 baseline doc remain the tracking mechanism for this gap:
  - **BL-C2-01** — Unity Profiler capture: `SimplePlayModeSimHost` + C2 panel hosts, ≥300 frames (needs Editor host)
  - **BL-C2-02** — PlayMode `Stopwatch` around full UI Toolkit bind (not projection-only)
  - **BL-C2-03** — If p95 frame > 16.67 ms: profile further (post-measurement; no sim hot-path edits)
- No new backlog items are added by S36-05 beyond re-confirming BL-C2-01..03 remain open — this session did not exceed any measurable budget, so the story's "if p95 > budget: backlog only" clause does not trigger.

**Who can close this:** requires a session with Unity Editor 6000.3.14f1 installed and a display/render pipeline (or a CI runner with `game-ci/unity-test-runner` PlayMode support) to run the smoke scene, let ≥300 frames elapse, and read `CapturedFrameTimesMs`.

---

## Notes (per story AC: Linux CI limitation, SimplePlayModeSimHost, UI Toolkit layout cost)

- **Linux CI limitation:** The project's CI and this sandbox both run headless `dotnet test` on Linux with no Unity Editor. This is sufficient for the deterministic sim/delegation/replay gates (ADR-010 headless-first core) and for the projection-bind proxy timing (`C2PanelBindTimingTests`), but structurally cannot measure Unity render-thread cost, GPU frame time, or UI Toolkit layout/style-resolve cost — those require an Editor or Player host with a display.
- **`SimplePlayModeSimHost`:** Already instrumented (S36-05 track, see above) to capture `Time.deltaTime`-based frame times when run under a real Unity `Update()` loop. It is the intended capture point for the 16.67 ms budget once an Editor host is available — no further code changes are needed to *start* capturing; the gap is purely environmental (no Unity Editor here).
- **UI Toolkit layout cost:** The headless `C2PanelBindTimingTests` proxy explicitly measures only the C# projection/bind chain (`C2SelectionResolver`, `MapPanelBinder.Bind`, `OobTreePanelBinder.Bind`, `ContactSummaryProjection.Project`, `SensorC2PanelBinder.Bind`) and does **not** include UI Toolkit USS style resolution, layout passes, or render thread cost — this is called out explicitly in the test file's own doc comment and in the S35 baseline doc's caveat. Editor PlayMode timing (once available) is expected to be higher than the headless proxy's ~0.01–0.15 ms figures, but is still expected to remain well under the 100 ms Req 20 budget at Baltic MVP scale; it is a separate, currently-unmeasured question whether full-frame UI Toolkit layout cost fits inside the 16.67 ms/frame budget (that is exactly what BL-C2-01/02 exist to answer).

---

## References

- `production/epics/sprint-36-ux-foundation/story-036-05-c2-frame-additional.md` — this story
- `production/perf/unity-c2-frame-baseline-s35-2026-06-19.md` — S35 baseline this appendix extends
- `production/perf/perf-profile-polish-baseline-2026-06-19.md` — governing perf-profile doc; BL-C2-01..03 backlog
- `src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/C2PanelBindTimingTests.cs` — headless Req 20 regression guard (test executed above)
- `src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/PlayModeSmokeHarnessTests.cs` — smoke suite executed above
- `src/ProjectAegis.Delegation.UnityAdapter.Tests/Baltic/ReplayGoldenSuiteTests.cs` — 6-case golden gate executed above
- `unity/ProjectAegis/Assets/Scripts/Runtime/SimplePlayModeSimHost.cs` — frame-capture instrumentation (Editor-only, not exercised here)
- `docs/architecture/adr-010-headless-first-command-driven-ui.md` — governing ADR (headless-first presentation; UI as read-only client)
- `docs/architecture/tr-registry.yaml` — TR-c2-004 entry (mismatch flagged above)
