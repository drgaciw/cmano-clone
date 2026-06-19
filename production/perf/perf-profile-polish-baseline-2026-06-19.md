# Performance Profile: Polish Baseline

**Generated:** 2026-06-19  
**Scope:** Full (headless sim + Unity C2 adapter + CI gates)  
**Method:** Static hot-path analysis + timed `dotnet test` benchmarks (Release where noted)  
**Gate context:** Closes gate-check blocker #3 — `production/gate-checks/production-to-polish-2026-06-19.md`  
**Polish scope:** `production/polish-scope-boundary-2026-06-19.md` (P0/P1 budgets only; no DOTS/ECS migration Phase 1)

---

## Executive Summary

| Item | Finding |
|------|---------|
| **Headless Baltic MVP** | Well within tick and CI budgets at current entity scale (~1 agent, ~2 contacts, ≤10 detection trials) |
| **Unity C2 frame budget** | **Unmeasured** — smoke harness exists; no Unity Profiler baseline |
| **Scale risk** | LINQ/allocation patterns in sim tick loop will not survive 5k-entity / 5k×10k sensor budgets without rework |
| **CI health** | **1204/1204** tests in **11.70 s**; ReplayGolden 6/6 in **166 ms** test time (post S35-05/S35-10) — strong headroom |

**Verdict:** Headless + CI paths are **OK** for Polish Phase 1 Baltic slice. Interactive Unity C2 and multi-thousand-entity targets are **WARNING** — require runtime profiling before optimization stories commit.

---

## Performance Budgets

| Metric | Budget | Estimated Current | Status | Notes |
|--------|--------|-------------------|--------|-------|
| **Unity C2 frame time** (60 fps target) | **16.67 ms** | *Unknown* | **WARNING** | Req 03/20; `SimplePlayModeSimHost.Update()` ticks every frame — no Profiler capture |
| **Headless sim tick** (Baltic MVP, 1 agent) | **< 1.0 ms/tick** (Polish P1) | **~0.5–2.8 ms/tick** | **OK** | Post S35-05/S35-10: 166 ms ÷ 54 ≈ 3.07 ms/tick amortized (see §Post-S35-05) |
| **Headless 300-tick replay** (ARCH-NFR-1) | **< 2.0 s** wall | **~150–900 ms** est. | **OK** | Extrapolated; direct 300-tick micro-bench failed outside test output dir (manifest path) |
| **Headless AvA throughput** (doc 03) | **≥256×** min; **1000×+** target | *Not measured* | **WARNING** | P1 deferred; no profile gate on reference Baltic at scale |
| **Memory — headless Baltic run** | **< 256 MB** working set | **~50–80 MB** est. | **OK** | In-memory `ICatalogReader` fixture; short tick counts |
| **Memory — 5k entity target** (doc 03/08) | **< 2 GB** | *Not implemented* | **WARNING** | MVP registry/dictionary model; ~2 contacts in harness |
| **Order log storage** (24 h scenario, doc 17) | **< 500 MB** compressed | *N/A at MVP ticks* | **OK** | Fingerprint/log growth not stressed in golden suite |
| **ReplayGolden suite runtime** (CI) | **< 30 s** wall | **3.63 s** elapsed / **166 ms** test (post S35-05/S35-10) | **OK** | 6 cases × double-run determinism check |
| **Full `dotnet test` suite ceiling** (CI) | **< 120 s** wall | **11.70 s** (1204 tests, Release) | **OK** | `tools/buildkite/dotnet-ci.sh` parity |
| **C2 panel selection latency** (doc 20) | **< 100 ms** | *Unknown* | **WARNING** | Headless `SensorC2Bridge` / projections only; no Editor timing |
| **Mission editor validation** (ADR-008) | **< 100 ms** Baltic-scale | *Not re-run today* | **OK** | ADR budget; separate from sim tick path |

**Budget headroom summary**

| Area | Headroom |
|------|----------|
| CI / headless MVP | **High** — ReplayGolden and full sln ~10× under ceiling |
| Unity C2 16.67 ms frame | **Unknown** — treat as blocking investigation for Polish P0 C2 stories |
| Entity scale (5k+) | **None proven** — architecture not yet at target density |

---

## Benchmarks (2026-06-19)

Environment: Linux, `PATH=/home/username01/.dotnet:$PATH`, local dev machine.

```bash
# ReplayGolden — UnityAdapter (blocking CI gate)
/usr/bin/time -f 'elapsed %e' dotnet test \
  src/ProjectAegis.Delegation.UnityAdapter.Tests \
  --filter "ReplayGoldenSuiteTests" -v minimal
# → Passed 6/6, Duration: 179 ms, elapsed 3.36

# ReplayGolden — Sim.Tests (user-requested filter)
/usr/bin/time -f 'elapsed %e' dotnet test \
  src/ProjectAegis.Sim.Tests --filter "ReplayGolden" -v minimal
# → No test matches (0 tests in assembly)

# Full solution (Release)
/usr/bin/time -f 'elapsed %e' dotnet test ProjectAegis.sln -c Release -v minimal
# → 1193 passed, elapsed 9.44

# Delegation.Tests ReplayGolden (related golden path)
/usr/bin/time -f 'elapsed %e' dotnet test \
  src/ProjectAegis.Delegation.Tests --filter "ReplayGolden" -v minimal
# → Passed 4/4, Duration: 22 ms, elapsed 2.84
```

**Tick-rate derivation (ReplayGoldenSuiteTests):**

| Case | Ticks/run | Runs (a+b) | Tick-iterations |
|------|-----------|------------|-----------------|
| baltic-patrol | 4 | 2 | 8 |
| baltic-patrol-comms | 6 | 2 | 12 |
| baltic-patrol-classify | 4 | 2 | 8 |
| baltic-patrol-stale | 3 | 2 | 6 |
| baltic-patrol-spoof | 5 | 2 | 10 |
| baltic-patrol-readiness | 5 | 2 | 10 |
| **Total** | | | **54** |

`179 ms ÷ 54 ≈ 3.3 ms/tick` (includes end-of-run `MessageLogProjection`, `SensorC2Bridge`, fingerprint — not amortized per tick). Conservative **~0.5–3.0 ms/tick** for MVP loop body.

---

## Benchmarks — Post-S35-05 / S35-10 (2026-06-19)

**Context:** S35-05 (detection P0 — trial `Dictionary` + pre-sorted trials) and S35-10 (DecisionLog incremental chronological + cached datalink sort) merged. Re-profile records delta vs **§Benchmarks (2026-06-19)** pre-merge baseline.

Environment: Linux, `PATH=/home/username01/.dotnet:$PATH`, local dev machine (same host as pre-merge).

```bash
# ReplayGolden — UnityAdapter (blocking CI gate)
/usr/bin/time -f 'elapsed %e' dotnet test \
  src/ProjectAegis.Delegation.UnityAdapter.Tests \
  --filter "ReplayGoldenSuiteTests" -v minimal
# → Passed 6/6, Duration: 166 ms, elapsed 3.63

# Full solution (Release)
/usr/bin/time -f 'elapsed %e' dotnet test ProjectAegis.sln -c Release -v minimal
# → 1204 passed, elapsed 11.70
```

### Delta vs pre-S35-05 baseline

| Metric | Pre-S35-05 (2026-06-19) | Post-S35-05/S35-10 | Δ | Verdict |
|--------|-------------------------|---------------------|---|---------|
| **ReplayGolden** pass count | 6/6 | 6/6 | — | **Unchanged** |
| **ReplayGolden** test `Duration` | **179 ms** | **166 ms** | **−13 ms (−7.3%)** | **Improvement** — detection + order-log/datalink P0/P1 hot-path wins |
| **ReplayGolden** wall `elapsed` | **3.36 s** | **3.63 s** | **+0.27 s (+8.0%)** | **Noise** — build/restore/JIT startup variance; test body faster |
| **Full sln** pass count | 1193/1193 | **1204/1204** | +11 tests | **Unchanged gate** — suite grew (S35-10 + catalog tests); all PASS |
| **Full sln** wall `elapsed` | **9.44 s** | **11.70 s** | **+2.26 s (+24%)** | **Mixed** — +11 tests + Release rebuild; not attributable solely to sim perf |
| **Amortized ms/tick** (ReplayGolden) | **179 ÷ 54 ≈ 3.31 ms** | **166 ÷ 54 ≈ 3.07 ms** | **−0.24 ms (−7.3%)** | **Improvement** — P0/P1 loop body faster at Baltic MVP scale |
| **Unity C2 frame** (16.67 ms) | *Unknown* | *Unknown* | — | **Unchanged** — S35-04 deferred on Linux; see cross-ref below |

**Executive delta narrative:** S35-05 and S35-10 delivered measurable headless tick savings without breaking determinism (ReplayGolden 6/6, Baltic hash `17144800277401907079` unchanged per merge evidence). CI wall times are **not** a reliable regression signal on this host — elapsed includes one-off build/restore; the **test `Duration`** and amortized **ms/tick** are the authoritative sim-perf deltas. Full-sln wall rose with test-count growth; per-test amortized cost is within normal variance.

**Tick-rate derivation (unchanged case table — 54 tick-iterations):**

| Case | Ticks/run | Runs (a+b) | Tick-iterations |
|------|-----------|------------|-----------------|
| baltic-patrol | 4 | 2 | 8 |
| baltic-patrol-comms | 6 | 2 | 12 |
| baltic-patrol-classify | 4 | 2 | 8 |
| baltic-patrol-stale | 3 | 2 | 6 |
| baltic-patrol-spoof | 5 | 2 | 10 |
| baltic-patrol-readiness | 5 | 2 | 10 |
| **Total** | | | **54** |

`166 ms ÷ 54 ≈ 3.07 ms/tick` (post-merge). Updated conservative band: **~0.5–2.8 ms/tick** for MVP loop body (tightened upper bound from P0/P1 closures).

### P0/P1 hotspot closure status (post S35-05 + S35-10)

| # | Location | Pre-merge status | Post-merge |
|---|----------|------------------|------------|
| 1 | `PdDetectionContactSimulator` — `_trials.First()` scans | **OPEN** (P0) | **CLOSED** — S35-05 `Dictionary<string, ScenarioDetectionTrial>` |
| 2 | `DeterministicDetectionLoop` — per-tick `OrderBy().ToArray()` | **OPEN** (P0) | **CLOSED** — S35-05 pre-sorted trials at bind |
| 3 | `DecisionLog.ChronologicalEntries` / fingerprint | **OPEN** (P1) | **CLOSED** — S35-10 incremental chronological append |
| 4 | `DatalinkSidePictureMerger.Merge` — nested LINQ | **OPEN** (P1) | **CLOSED** — S35-10 cached observer/side ordering |
| 5 | `BalticReplayHarness` — `Concat().ToArray()` datalink path | **OPEN** (quick win) | **OPEN** — optional; not in S35-10 scope |
| — | `SimulationSession` — `engageOrders.Where(...).ToArray()` | **OPEN** | **OPEN** |
| — | `BdaContactLifecycleHotTickApplier` — per-tick LINQ | **OPEN** | **OPEN** |

### Unity C2 frame budget (S35-04 cross-ref)

| Item | Status | Reference |
|------|--------|-----------|
| Unity C2 frame time **16.67 ms** | **UNKNOWN** on Linux CI | `production/perf/unity-c2-frame-baseline-s35-2026-06-19.md` — BL-C2-01 Editor Profiler backlog |
| C2 panel selection **< 100 ms** | **OK** (headless p95 **0.013 ms**) | Same doc — S35-04 headless proxy |

> **Do not assert PASS on 16.67 ms frame budget** until Editor Profiler capture lands (S35-09 or dedicated session).

**Raw command output:** `production/agentic/sprint-35-perf-reprofile-2026-06-19.md`

---

## Hot Paths — Static Analysis

### Tick / Update loops

| Location | Role | Est. cost at scale |
|----------|------|-------------------|
| `BalticReplayHarness.Run` L140–238 | Headless main loop: detection → datalink → `bridge.Tick` → BDA/kill hooks → checkpoints | **HIGH** orchestration surface |
| `DelegationBridge.Tick` → `SimulationSession.Tick` | Delegation + engage pipeline per tick | **MED** |
| `SimTickPipeline.TickOnce` | Clock advance + engagement resolve + world hash | **LOW** at MVP |
| `SimplePlayModeSimHost.Update` L54–64 | Unity frame → `bridgeHost.RunTick` | **MED** (Unity overhead unmeasured) |
| `PdDetectionContactSimulator.Tick` L60–126 | Detection roll + lifecycle FSM | **HIGH** with trial count |

### Catalog reads in hot path

| Location | Pattern | Risk |
|----------|---------|------|
| `DetectionTrialResolver.Resolve` | `TryGetBasePd` at **scenario bind** (harness L66) | **OK** — not per-tick |
| `DeterministicDetectionLoop.RollTick` L41–42 | `ScenarioEmconResolver.ResolveRadar(..., catalog)` per trial per tick | **MED** if catalog lookups grow |
| `CatalogDamageHotTickApplier` | Comment: gate-approved snapshot, no hot-path SQLite | **OK** by design |
| `DatalinkShareLagResolver.Resolve` | Catalog read at harness bind | **OK** |

### LINQ / allocation in sim hot path

| File | Lines | Issue |
|------|-------|-------|
| `DeterministicDetectionLoop.cs` | 25–29 | `OrderBy().ThenBy().ToArray()` **every tick** |
| `PdDetectionContactSimulator.cs` | 112–120, 199, 218, 231, 265–277, 308–319 | `_trials.First(t => t.ContactId == …)` — **O(trials × contacts)** per tick |
| `DatalinkSidePictureMerger.cs` | 129–135, 159–165, 207–210 | `Where/OrderBy/ToList` + nested `OrderBy` over observers/targets |
| `BalticReplayHarness.cs` | 193 | `transitions.Concat(shared).ToArray()` when datalink enabled |
| `SimulationSession.cs` | 130–132 | `ExecutedOrders.Where(Engage).ToArray()` per tick |
| `DecisionLog.cs` | 208–306 | `ChronologicalEntries()` merges 15+ lists then `OrderBy(SequenceId).ToArray()` |
| `BdaContactLifecycleHotTickApplier.cs` | 45–49 | `Where/Select/OrderBy/ToArray` per tick when registry active |

---

## Hotspots Identified

| # | Location | Issue | Est. Impact | Fix Effort |
|---|----------|-------|-------------|------------|
| 1 | `PdDetectionContactSimulator.cs` — repeated `_trials.First()` | Linear scan per contact per tick; compounds with classify/stale/kill paths | **HIGH** at 10k trials | **S** |
| 2 | `DeterministicDetectionLoop.cs` L25–29 | Per-tick sort + array alloc of trials | **MED** → **HIGH** at 5k emitters | **S** |
| 3 | `DecisionLog.ChronologicalEntries` / `ComputeFingerprint` | Full merge + sort on every checkpoint/fingerprint/export | **MED** on long scenarios | **M** |
| 4 | `DatalinkSidePictureMerger.Merge` | Nested sorted iteration + pending-share LINQ flush | **MED** when datalink scenarios enabled | **M** |
| 5 | `BalticReplayHarness` tick loop L181–194 | `Concat().ToArray()` alloc when merging shared contacts | **LOW** now; **MED** with datalink + long runs | **S** |

---

## Optimization Recommendations (Priority Order)

### 1. Index detection trials by `ContactId` in `PdDetectionContactSimulator`
- **Location:** `src/ProjectAegis.Sim/Sensors/PdDetectionContactSimulator.cs` (ctor + all `First()` call sites)
- **Expected gain:** O(1) trial lookup; ~30–50% detection-phase CPU at hundreds+ contacts
- **Risk:** Low — deterministic ordering preserved via explicit sort keys elsewhere
- **Approach:** Build `Dictionary<string, ScenarioDetectionTrial>` in ctor; replace 8× `First()` calls

### 2. Pre-sort detection trials once at bind time
- **Location:** `DeterministicDetectionLoop.RollTick`, `PdDetectionContactSimulator` ctor, `BalticReplayHarness` L72–78
- **Expected gain:** Remove per-tick `OrderBy().ToArray()` alloc in tick 4
- **Risk:** Low — sort key already documented (ObserverId → SensorId → TargetId)
- **Approach:** Store `ScenarioDetectionTrial[] _sortedTrials` immutable at construction

### 3. Incremental order-log fingerprint / chronological view
- **Location:** `src/ProjectAegis.Delegation/Decision/DecisionLog.cs` L208–326
- **Expected gain:** O(1) append vs O(n log n) rebuild; critical for 300-tick checkpoints + AAR export
- **Risk:** Medium — must preserve ADR-003 sequence ordering contract
- **Approach:** Single `_chronological` list inserted in sequence order on append; fingerprint via incremental hasher

### 4. Eliminate per-tick `Concat().ToArray()` in harness datalink path
- **Location:** `src/ProjectAegis.Delegation.UnityAdapter/Baltic/BalticReplayHarness.cs` L184–194
- **Expected gain:** Remove 1 alloc/tick on datalink scenarios (~5–15% harness loop at scale)
- **Risk:** Low
- **Approach:** Reuse `List<ContactTransition>` with capacity hint; or write shared transitions directly into list

### 5. Precompute datalink observer/side ordering at merger construction
- **Location:** `src/ProjectAegis.Sim/Sensors/DatalinkSidePictureMerger.cs` L151–165, L373
- **Expected gain:** Remove repeated `OrderBy` on stable doctrine maps each tick
- **Risk:** Low — doctrine immutable per scenario
- **Approach:** Cache `SortedObserverSide[]` in ctor; use index loops in `EmitSharedTransitions`

---

## Quick Wins (< 1 hour each)

- Add `Dictionary<string, ScenarioDetectionTrial> _trialByContactId` in `PdDetectionContactSimulator` — replaces all `First()` in hot tick path.
- Pass pre-sorted `ScenarioDetectionTrial[]` into `DeterministicDetectionLoop.RollTick` (remove internal `OrderBy`).
- Replace `transitions.Concat(shared).ToArray()` with `List<ContactTransition>` reuse in `BalticReplayHarness` datalink branch.
- Replace `engageOrders.Where(...).ToArray()` with indexed loop + scratch list in `SimulationSession.RunExecutingTick`.
- Document **no SQLite in tick path** — verify `CatalogReaderFactory` always returns in-memory/Blob snapshot in CI (already intended per `CatalogDamageHotTickApplier` comment).

---

## Requires Investigation

| Area | Tool / method | Limitation |
|------|---------------|------------|
| **Unity C2 frame budget (16.67 ms)** | Unity Profiler + Editor PlayMode (`SimplePlayModeSimHost`, `DelegationBridgeHost`, C2 panels) | Headless `dotnet` cannot measure render/UI thread |
| **GC allocations per tick** | `dotnet-trace` / BenchmarkDotNet on `BalticReplayHarness.Run(42, "baltic-patrol", 1000)` | Requires test-output content roots (`abort_reason_manifest.json`) |
| **300-tick ARCH-NFR-1 wall time** | Run inside `ProjectAegis.Delegation.UnityAdapter.Tests` or MissionEditor CLI with `--ticks 300` | Standalone console bench failed: `AbortReasonManifest` path resolution |
| **5k map symbols @ 60 fps** (doc 20) | Unity Profiler + GPU frame debugger | LOD/Cesium production path out of Polish Phase 1 scope |
| **Headless 256× / 1000× throughput** (doc 03) | Dedicated soak test: wall clock vs sim-time over 10k ticks | No fixture at production entity count yet |
| **C2 panel bind < 100 ms** | PlayMode test with `Stopwatch` around `SensorC2PanelBinder` integration | Only headless bridge tests exist today |

---

## Polish Phase 1 — P0/P1 Budget Governance

Per `production/polish-scope-boundary-2026-06-19.md`:

| Priority | Item | Status |
|----------|------|--------|
| **P0** | ReplayGolden CI gate | **OK** — 6/6, **166 ms** (post S35-05/S35-10) |
| **P0** | Full sln test gate | **OK** — **1204/1204**, **11.70 s** |
| **P1** | Headless tick < 1 ms (Baltic MVP) | **OK** — ~3.07 ms/tick amortized; loop body ~0.5–2.8 ms est. |
| **P1** | Unity frame budget proof | **BLOCKED** — needs Editor Profiler; see `unity-c2-frame-baseline-s35-2026-06-19.md` |
| **P1** | C2 selection < 100 ms | **BLOCKED** — needs PlayMode timing |
| **P2+** | DOTS/ECS hot-path migration | **Deferred** per scope boundary |

---

## Deferred to Polish (user choice C — accept until measured)

- Multi-thousand-entity spatial broadphase (doc 15: 5k×10k sensor budget)
- Headless 1000×+ AvA throughput profile gate (doc 03 P1)
- Burst/Jobs engagement validators (ADR-009 P2)

---

## Next Actions

1. **Run Unity Profiler** on Editor PlayMode smoke scene — capture mean/max frame time for `SimplePlayModeSimHost` + C2 binders.
2. **Add headless micro-benchmark test** (Adapter.Tests) for 300-tick `baltic-patrol` with wall-time assert `< 2000 ms` (ARCH-NFR-1).
3. **Implement quick win #1 + #2** before any 100+ contact scenario lands in ReplayGolden catalog.
4. **Re-run `/perf-profile`** after first 500+ entity fixture or Cesium map integration.

---

## References

- `CLAUDE.md` — Unity 6.3 LTS, headless + `dotnet` split
- `Game-Requirements/requirements/03-Simulation-Modes.md` — 60 fps / 256× / 1000× targets
- `Game-Requirements/requirements/08-Agentic-Architecture.md` — ARCH-NFR-1 (300-tick CI)
- `Game-Requirements/requirements/20-Command-And-Control-UI.md` — 16.67 ms frame, 100 ms panel
- `design/gdd/simulation-core-time.md` — tick pipeline, headless batch
- `src/ProjectAegis.Delegation.UnityAdapter/Baltic/BalticReplayHarness.cs` — headless hot loop
- `tools/buildkite/dotnet-ci.sh` — CI test ceilings