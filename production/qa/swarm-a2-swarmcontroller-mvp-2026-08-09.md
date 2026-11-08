# SWARM-A2 — SwarmController MVP (DRG-87)

**Date:** 2026-08-09  
**Linear:** [DRG-87](https://linear.app/drgamtd-workspace/issue/DRG-87) · Epic [DRG-83](https://linear.app/drgamtd-workspace/issue/DRG-83) · Milestone H8  
**Requirements:** SWARM-03, SWARM-06, SWARM-07 (integrity timeline half of SWARM-02)  
**Surface:** `src/ProjectAegis.Sim/Swarm/**` · `src/ProjectAegis.Sim.Tests/Swarm/**` · this QA note  
**Base:** main @ A1 merge `3c2f1a9` (DRG-86)  
**Verdict:** PASS (agent — local Sim.Tests swarm filter)

## Scope

Aggregate **SwarmController** behavior in pure Sim (ADR-001 / ADR-010 headless-first):

| Concern | Implementation |
|---------|----------------|
| Intent orders Hold / Move / Attack | `SwarmController.IssueHold/IssueMove/IssueAttack` |
| Centroid motion | `Tick(deltaSeconds)` advances lat/lon toward waypoint (Move/Attack only) |
| Headless logged intents | `SwarmOrderLog` / `SwarmOrderLogEntry` (Sim-local; Delegation `IOrderLog` bridge = DRG-91) |
| Integrity pool | Registered from Data `SwarmUnitIntegrity`; mutated **only** via `TryApplyIntegrityDamage` |
| Deterministic aggregate SoT | Same seed + same damage schedule → same `ComputeIntegrityTimelineHash()`; no per-drone physics |

**Not in this PR:** weapon damage curves / hard-counter AA (DRG-88), sensor scaling (DRG-89), C2 map integrity chrome (DRG-90), full replay golden integration (DRG-91), Unity UI, catalog schema (A1 done).

**Vocabulary:** distinct from `SwarmSalvoDeconfliction` (salvo slots) and `SwarmTier` (entity caps).

## Acceptance criteria

| AC | Evidence | Verdict |
|----|----------|---------|
| Headless command moves swarm centroid | `Headless_move_command_advances_swarm_centroid` | **PASS** |
| Attack/hold intents logged and replayable | `Attack_and_hold_intents_are_logged_and_replayable` + `SwarmController.ReplayOrders` | **PASS** |
| Integrity fields update only via authorized damage API | `Integrity_updates_only_via_authorized_damage_api` (record reassignment does not mutate) | **PASS** |
| Same scenario+seed → same integrity timeline | `Same_scenario_seed_yields_same_integrity_timeline` | **PASS** |

## Gates

| Gate | Result |
|------|--------|
| Surface discipline | Sim/Swarm + Sim.Tests/Swarm + production/qa only — no DelegationBridge, no Unity, no catalog schema, no A3 weapon curves |
| `dotnet build ProjectAegis.sln` | 0 errors (agent) |
| `dotnet test` Sim.Tests swarm filter | green (agent) |
| ReplayGoldenSuiteTests | not modified — suite still applies as pre-existing 6/6 (no golden path change) |
| No DelegationBridge hotpath | untouched |

## Key types / files

- `src/ProjectAegis.Sim/Swarm/SwarmController.cs`
- `src/ProjectAegis.Sim/Swarm/SwarmOrderLog.cs` / `SwarmOrderLogEntry.cs`
- `src/ProjectAegis.Sim/Swarm/SwarmIntegrityChange.cs`
- `src/ProjectAegis.Sim/Swarm/SwarmIntentKind.cs`
- `src/ProjectAegis.Sim.Tests/Swarm/SwarmControllerTests.cs`

## Verify commands

```bash
export PATH="$HOME/.dotnet:$PATH"
dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj \
  --filter "FullyQualifiedName~SwarmControllerTests" -v minimal
dotnet build ProjectAegis.sln -v minimal
```

## Follow-ons

- **Wave 3** (surface-disjoint after this merges): DRG-88 ∥ 89 ∥ 90
- **DRG-91** replay + performance caps — bridge Sim swarm order log into Delegation fingerprint path
- Phase B modes / host / link — DRG-92 hold until Phase A closes
