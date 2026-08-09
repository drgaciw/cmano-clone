# SWARM-A6 — Replay + performance caps (DRG-91)

**Date:** 2026-08-09  
**Linear:** [DRG-91](https://linear.app/drgamtd-workspace/issue/DRG-91) · Epic [DRG-83](https://linear.app/drgamtd-workspace/issue/DRG-83)  
**Requirements:** SWARM-24 (replay integrity), SWARM-25 (performance caps)  
**Surface:** `src/ProjectAegis.Sim/Swarm/**` · `src/ProjectAegis.Sim.Tests/Swarm/**` · this QA pack  
**Verdict:** PASS (Phase A)

## Scope

| Concern | Implementation |
|---------|----------------|
| Replay orders + integrity deltas | `SwarmReplayHarness` + `SwarmController.ReplayIntegrityTimeline` |
| Golden fingerprint | `production/qa/swarm-a6-replay-golden-fingerprint.txt` |
| Logical vs render caps | `SwarmPerformanceCaps` (independent ceilings) |
| Stress / pulse budget | `RunDesignMaxStress` — O(swarms×ticks) work units |

**Not in this PR:** Unity UI chrome, Baltic `ReplayGoldenRegressionCatalog` mutation (6/6 suite unchanged), Phase B modes/host/link.

## Caps (SWARM-25)

| Cap | Value | Role |
|-----|------:|------|
| `LogicalMaxDronesPerSwarm` | 40 | Combat SoT integrity ceiling (= generic catalog max) |
| `RenderMaxMembersPerSwarm` | 12 | Cosmetic LOD only — not engagement authority |
| `DesignMaxConcurrentSwarms` | 16 | Phase A scenario design max |
| `DesignMaxLogicalDrones` | 640 | 16 × 40 (logical inventory, not per-pulse work) |
| Engagement work / pulse | = concurrent swarm **units** | Aggregate SoT — **not** O(logical drones) |
| Stress ticks | 60 | Headless integrity + Tick |
| Stress wall budget | 2000 ms | CI-friendly soft gate |

Distinct from `SwarmTierLimits` (req-09 near-future entity tiers Micro/Medium/Mass).

## Golden scenario (SWARM-24)

Seed `42`, unit `swarm-golden-1` @ generic spawn:

1. Hold → Move → Attack (headless orders)
2. Point-fire ×2 + Area-AA ×1 integrity hits (authorized API)
3. Final integrity **30/40** (40 − 1 − 1 − 8)

Pinned canonical string: [`swarm-a6-replay-golden-fingerprint.txt`](./swarm-a6-replay-golden-fingerprint.txt)

Replay reconstructs the same order intents, drones-lost sequence, and final count.

## Stress evidence

| Metric | Result |
|--------|--------|
| Concurrent swarms | 16 |
| Ticks | 60 |
| Integrity ops applied | ≤ 960 (one per swarm per tick while alive) |
| Engagement work units | **960** = 16 × 60 |
| Logical drones at start | 640 |
| Counterfactual O(drones×ticks) | 38 400 — **not** used |
| Wall clock | < 2000 ms on agent CI host (soft) |
| Integrity hash | deterministic across two runs |

## Acceptance

| AC | Evidence | Verdict |
|----|----------|---------|
| Replay reconstructs integrity-affecting events | `SwarmReplayAndCapsTests.Replay_reconstructs_integrity_affecting_events` + golden pin | **PASS** |
| Caps documented (logical vs render) | this table + `SwarmPerformanceCaps` | **PASS** |
| Stress scenario in `production/qa/` | this section + `Design_max_stress_*` test | **PASS** |

## Gates

| Gate | Result |
|------|--------|
| Surface discipline | Sim/Swarm + Sim.Tests + production/qa only |
| `ReplayGoldenSuiteTests` 6/6 | Unchanged (no golden catalog edit) — verify CI |
| Suite floor | held on CI |

## Verify

```bash
export PATH="$HOME/.dotnet:$PATH"
dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj \
  --filter "FullyQualifiedName~Swarm" -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "FullyQualifiedName~ReplayGoldenSuiteTests" -v minimal
```

## Follow-ons

- Wire `SwarmReplayHarness` fingerprint into Baltic order-log bridge (optional; deferred from A2)
- Phase B: render member cosmetics under `RenderMaxMembersPerSwarm`
- DRG-92 Phase B umbrella after Phase A close
