# Unity C2 Frame Budget Baseline — S35-04

**Generated:** 2026-06-19  
**Story:** S35-04 — Unity C2 Frame Budget Baseline (`team-unity`)  
**Req trace:** Req 20 TR-c2-004; perf-profile P0 Unity frame budget  
**Governing baseline:** `production/perf/perf-profile-polish-baseline-2026-06-19.md`  
**Host:** Linux CI / headless `dotnet` (Unity Editor Profiler unavailable)

---

## Executive Summary

| Metric | Budget | Measured (S35-04) | Status |
|--------|--------|-------------------|--------|
| **Unity C2 frame time** (60 fps) | **16.67 ms** mean/p95 | *Unknown* | **DEFERRED** — Unity Profiler unavailable on Linux CI |
| **C2 panel selection bind** (Req 20) | **< 100 ms** wall | **p95 0.013 ms** (headless) | **OK** — well under budget |
| **ReplayGolden** | **6/6** PASS | **6/6** PASS (287 ms) | **OK** |
| **C2 proxy checks 1–13** | **≥61/61** | **85/85** PASS | **OK** — suite grew since S34 baseline |
| **C2 proxy checks 14–18** | **≥58/58** | **58/58** PASS | **OK** |

**Verdict:** Headless panel-bind path is **proven under Req 20 budget**. Unity frame budget (16.67 ms P0) remains **unmeasured** on this host — backlog filed below per `perf-profile-polish-baseline` **WARNING**.

---

## Environment Limitation — Unity Profiler

Unity Profiler and `SimplePlayModeSimHost` frame capture require a **Unity Editor host** with display/render pipeline. The Linux CI / headless `dotnet` host cannot:

- Attach Unity Profiler to `Update()` / UI Toolkit layout passes
- Measure GPU/render thread contribution to frame time
- Capture ≥300 frames of live PlayMode smoke scene

**S35-04 defers live 16.67 ms capture to Editor host** (S35-09 live Editor evidence or dedicated Profiler session).

---

## Headless Fallback Methodology

Proxy for C2 panel selection latency when Profiler is unavailable:

1. **Fixture:** `BalticReplayHarness.Run(7, "baltic-patrol-classify", ticks: 10)` — same classify scenario as `PlayModeSmokeHarnessTests.Baltic_classify_selection_flow_syncs_map_oob_and_contact_summary`.
2. **Setup (untimed):** Build OOB tree, `MapPictureProjection.Project`, resolve hostile symbol.
3. **Warmup:** 3 iterations (JIT + allocation steady state).
4. **Measured path (timed):** Full selection bind chain per user hostile click:
   - `C2SelectionResolver.ResolveDefaultFriendlyUnit`
   - `MapPanelBinder.Bind` (friendly default)
   - `OobTreePanelBinder.Bind`
   - `C2SelectionResolver.TryResolveHostileContactFromSymbol`
   - `ContactSummaryProjection.Project`
   - `MapPanelBinder.Bind` (contact selection)
   - `SensorC2PanelBinder.Bind`
5. **Sample:** 20 iterations; report mean / p95 / max via `Stopwatch.Elapsed.TotalMilliseconds`.
6. **Assert:** p95 and max **< 100 ms** (Req 20 panel-bind budget).

**Test file:** `src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/C2PanelBindTimingTests.cs`  
**Test name:** `C2_panel_selection_bind_path_completes_under_100ms_budget`

### Panel-bind timing results (2026-06-19)

```
C2 panel selection bind: mean=0.007 ms p95=0.013 ms max=0.042 ms (n=20)
Test wall time (incl. harness setup): ~126–183 ms
```

| Stat | Value | vs Req 20 (100 ms) |
|------|-------|---------------------|
| Mean | **0.007 ms** | **~14,000× headroom** |
| p95 | **0.013 ms** | **~7,700× headroom** |
| Max | **0.042 ms** | **~2,400× headroom** |

> **Caveat:** Headless projection bind excludes UI Toolkit layout, USS style resolve, and render thread. Editor PlayMode timing may be higher; still expected ≪ 100 ms at Baltic MVP scale.

---

## Unity Frame Budget — Unknown / Backlog

Per `production/perf/perf-profile-polish-baseline-2026-06-19.md`:

| Item | perf-profile status | S35-04 action |
|------|---------------------|---------------|
| Unity C2 frame time 16.67 ms | **WARNING** — unmeasured | No live capture on Linux CI |
| C2 panel selection < 100 ms | **WARNING** — unmeasured | **RESOLVED** headless — see above |
| `SimplePlayModeSimHost` + C2 binders ≥300 frames | Not run | **BACKLOG** |

### Optimization backlog (frame budget — no hot-path edits in S35-04)

| ID | Item | Owner / when | Notes |
|----|------|--------------|-------|
| **BL-C2-01** | Unity Profiler capture: `SimplePlayModeSimHost` + C2 panel hosts, ≥300 frames | Editor host — S35-09 or pre-S35-14 | Record mean/p95/max frame ms; compare to **16.67 ms** P0 |
| **BL-C2-02** | PlayMode `Stopwatch` around full UI Toolkit bind (not projection-only) | S35-09 | Validates Req 20 on render path |
| **BL-C2-03** | If p95 frame > 16.67 ms: profile `DelegationBridgeHost.RunTick` vs UI layout | Post-measurement | File targets only; **no sim hot-path edits in S35-04** |

**Frame p95:** *Unknown* — cannot assert pass/fail vs 16.67 ms on this host.

---

## Verification Commands (S35-04 evidence)

Environment: `PATH=/home/username01/.dotnet:$PATH`, Linux, Debug build.

```bash
# Panel-bind timing + ReplayGolden
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests \
  --filter "C2PanelBindTiming|ReplayGolden" -v minimal
# → Passed 18/18 (broader ReplayGolden name match + 1 timing), Duration ~218 ms

dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests \
  --filter "FullyQualifiedName~ReplayGoldenSuiteTests" -v minimal
# → Passed 6/6, Duration: 287 ms

# C2 proxy checks 1–13
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests \
  --filter "PlayModeSmoke|C2Selection|OobTree|LossesScoring|BalticReplay|FuelState|AttackMenu" -v minimal
# → Passed 85/85, Duration: 445 ms
# Note: S34 baseline was 61/61; suite expanded (PlayModeSmoke batch + S35 additions) — all PASS, zero regressions

# C2 proxy checks 14–18
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests \
  --filter "PlatformImport|Doctrine|C2TopBar|PlatformCatalogViewer|PlatformComms|PlatformLinkCatalog" -v minimal
# → Passed 58/58, Duration: 470 ms
```

### Regression summary

| Gate | Expected | Actual | Result |
|------|----------|--------|--------|
| `C2PanelBindTiming` | < 100 ms p95 | 0.013 ms p95 | **PASS** |
| `ReplayGoldenSuiteTests` | 6/6 | 6/6 | **PASS** |
| C2 checks 1–13 | ≥61/61 | 85/85 | **PASS** |
| C2 checks 14–18 | 58/58 | 58/58 | **PASS** |
| `DelegationBridge.cs` | ZERO touch | ZERO touch | **PASS** |

---

## References

- `production/epics/sprint-35-c2-platform-polish/story-035-04-c2-profiler-baseline.md`
- `production/perf/perf-profile-polish-baseline-2026-06-19.md` — WARNING items BL-C2-01..03
- `production/qa/qa-plan-sprint-35-2026-06-19.md` — C2 18/18 filter definitions
- `Game-Requirements/requirements/20-Command-And-Control-UI.md` — 16.67 ms frame, 100 ms panel
- `src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/C2PanelBindTimingTests.cs`
- `src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/PlayModeSmokeHarnessTests.cs` — selection scenario reference