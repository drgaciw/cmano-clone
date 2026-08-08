# C2 Panel Bind Bench — Wave 2 (CMD-36)

**Generated:** 2026-08-01  
**Story:** CMD-36 — expand headless C2 panel bind performance coverage  
**Req trace:** Req 20 panel selection latency (< 100 ms)  
**Governing baselines:**  
- `production/perf/perf-profile-polish-baseline-2026-06-19.md`  
- `production/perf/unity-c2-frame-baseline-s35-2026-06-19.md`  
**Host:** Linux CI / headless `dotnet` (Unity Editor Profiler unavailable)

---

## Executive Summary

| Metric | Budget | Measured (2026-08-01) | Status |
|--------|--------|----------------------|--------|
| **C2 selection bind** (legacy path) | p95 & max **< 100 ms** | mean **0.009** / p95 **0.015** / max **0.027** ms (n=20) | **OK** |
| **C2 rich panel bind** (wave-2 apply path) | p95 & max **< 100 ms** | mean **0.006** / p95 **0.009** / max **0.028** ms (n=20) | **OK** |

**Verdict:** Expanded headless panel-bind path (map / unit / contact / agent / envelopes / message log) stays **well under Req 20**. No production hotpath changes required.

---

## Method

1. **Fixture:** `BalticReplayHarness.Run(7, "baltic-patrol-classify", ticks: 10, mvpEngagement: false)`.
2. **Setup (untimed):** OOB tree, `MapPictureProjection.Project`, resolve hostile contact id, sample `UnitDetailEntry`, `AgentRosterProjection` rows.
3. **Warmup:** 3 iterations (JIT + allocation steady state).
4. **Measured path (timed, `Stopwatch`):** richer bind chain per iteration:
   - `TacticalOverlayProjection.ProjectSelectedUnitEnvelopes`
   - `MapPanelApplyState.BindAndApply` (symbols + envelope rings)
   - `UnitDetailApplyState.BindAndApply`
   - `ContactDetailProjection.Project` → `ContactDetailApplyState.Apply`
   - `AgentRosterApplyState.Apply`
   - `MessageLogPanelBinder.Bind` (cheap HUD row map)
5. **Sample:** 20 iterations; report mean / p95 / max via tests-only `C2PanelPerfSample`.
6. **Assert:** p95 and max **< 100 ms** (Req 20 panel-bind budget).

**Legacy selection path** (map/oob binders + contact summary + sensor drawer) remains covered by `C2PanelBindTimingTests`.

---

## How to re-run

```bash
export PATH=$HOME/.dotnet:$PATH

dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests \
  --filter "C2Panel" -v n
```

Filter subtypes:

| Filter | Coverage |
|--------|----------|
| `FullyQualifiedName~C2PanelPerfBenchTests` | Rich wave-2 apply path |
| `FullyQualifiedName~C2PanelBindTimingTests` | Legacy selection bind path |
| `C2Panel` | Both benches + related SensorC2 binder tests |

Numbers appear in NUnit output as:

```
C2 panel selection bind: mean=… ms p95=… ms max=… ms (n=20)
C2 rich panel bind: mean=… ms p95=… ms max=… ms (n=20)
```

---

## Measured results (2026-08-01)

Environment: Linux, `PATH=$HOME/.dotnet:$PATH`, Debug build under `dotnet test`.

```
C2 panel selection bind: mean=0.009 ms p95=0.015 ms max=0.027 ms (n=20)
C2 rich panel bind: mean=0.006 ms p95=0.009 ms max=0.028 ms (n=20)
```

| Path | Mean | p95 | Max | n | vs Req 20 (100 ms) |
|------|------|-----|-----|---|---------------------|
| Selection bind | **0.009 ms** | **0.015 ms** | **0.027 ms** | 20 | **~3,700× p95 headroom** |
| Rich panel bind | **0.006 ms** | **0.009 ms** | **0.028 ms** | 20 | **~11,000× p95 headroom** |

**Test run:** 4 matched (`C2Panel` filter) — all Passed; wall ~0.81 s test time after build.

> **Caveat:** Headless projection/apply bind excludes UI Toolkit layout, USS style resolve, and render thread. Editor PlayMode timing may be higher; still expected ≪ 100 ms at Baltic MVP scale.

---

## Test / helper files

| File | Role |
|------|------|
| `src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/C2PanelPerfBenchTests.cs` | Rich bind path + budget asserts |
| `src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/C2PanelBindTimingTests.cs` | Legacy selection bind path |
| `src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/C2PanelPerfSample.cs` | Tests-only mean/p95/max + `FormatReport()` |

---

## Scope notes (Wave 2 lane P)

- **Allowed:** timing tests under `UnityAdapter.Tests/.../C2Panel*`, `production/perf/*` report markdown.
- **Forbidden / not done:** host production rewrites, `DelegationBridge.Tick` changes, scene rewrites.
- No micro-optimizations applied — budgets already pass with large headroom.
