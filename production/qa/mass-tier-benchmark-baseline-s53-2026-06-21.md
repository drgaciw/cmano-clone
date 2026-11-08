# S53 MASS Tier Benchmark Baseline (Prep + Initial Harness)

**Date:** 2026-06-21  
**Scope:** S53 MASS tier prep+initial (after S52 benchmark). ONLY this worktree.  
**Boundary:** `production/release-enablement-scope-boundary-2026-06-20.md` (Req 09 MASS deferred post-release; this is initial skeleton prep).  
**Isolation:** No DOTS spawn (separate worktree); pure harness + sim path via BalticReplayHarness + NearFutureArchetypeRuntime.  
**Harness integration:** BalticReplayHarness now accepts `maxSwarmTier: SwarmTier.Mass` (default Medium preserved).  

## Implementation Complete (S53 full)

- Multi-agent spawn: `RegisterNearFutureUnits` now registers + emits NF_SPAWN + **CreateAgent + AssignAgentToTarget** for every accepted plan (including MASS).
- Gate tests extended: accept Mass at Mass cap (TL3), full ApplyAllGates.
- Batch runner extended: `BatchRequest` now carries `NearFutureUnits`, `MaxTechnologyLevel`, `MaxSwarmTier`; forwards to harness.
- CLI spawn/simulate already had metadata parsing (S53 prep); tests extended for Mass.
- Integration test: `BalticBatchRunnerTests.Run_supports_mass_tier_multi_agent_spawn_s53` exercises NF Mass + verifies NF_SPAWN in log.
- Data: `swarm-saturation` (Mass,5000,TL3) + `SwarmTier.Mass` + limits + dto comments ready.

**Data:** Uses `data/catalog/near_future_archetypes.json` "swarm-saturation" (Mass, 5000, TL3).

## Harness + Benchmark Data

- `BalticReplayHarness.Run(..., maxSwarmTier: SwarmTier.Mass)` enables swarm-saturation archetype (up to 5000).
- NF_SPAWN events emitted for accepted plans.
- Multi-agent spawns created (nf- prefixed agents assigned).
- Verification uses golden replay invariant (hash held for existing; MASS new paths use separate fixtures for future).

## Sample Benchmark Invocation (CLI / test)

```csharp
// Example for MASS scale benchmark (headless)
var plans = NearFutureArchetypeRuntime.PlanSpawns(
    new[] { new ScenarioNearFutureUnitRequest("swarm-saturation", "mass-001") },
    maxTech: 3,
    maxSwarmTier: SwarmTier.Mass,
    catalogPath);
var result = BalticReplayHarness.Run(seed: 53, "baltic-patrol", ticks: 10, nearFutureUnits: ..., maxSwarmTier: SwarmTier.Mass);
```

## Expected Metrics (initial skeleton; S52 baseline reference)

- Entity cap gate: MassMaxEntities = 5000 (SwarmTierLimits)
- Gate pass for swarm-saturation at Mass; reject at Medium (tests updated)
- Multi-agent count == accepted plans for NF.
- Perf evidence (demo batch MASS, 5 ticks seed53, swarm-saturation NF): real ~3.37s (user 2.0s) on this host.
- Demo CLI now supports --near-future + --max-swarm-tier Mass for direct MASS benchmark invocation (beyond harness NF_SPAWN).
- Determinism: seeded; hashes for non-mass paths unchanged.

## Verification Evidence (full impl)

- `src/ProjectAegis.Data.Tests/Catalog/CatalogArchetypeGateTests.cs` : ApplySwarmTierCap_accepts... + ApplyAllGates for Mass
- `src/ProjectAegis.Data.Tests/Catalog/NearFutureArchetypeRuntimeTests.cs` : PlanSpawns_accepts_mass...
- `src/ProjectAegis.Delegation.UnityAdapter/Baltic/BalticReplayHarness.cs` : multi-agent in RegisterNearFutureUnits
- `src/ProjectAegis.Delegation.UnityAdapter/Baltic/BalticBatchRunner.cs` + Tests
- `src/ProjectAegis.MissionEditor.Cli.Tests/ScenarioNearFutureSpawnCommandTests.cs`
- `src/ProjectAegis.Delegation.Demo/Program.cs` : S53 MASS CLI --near-future/--max-swarm-tier forwarding (completion beyond harness).

**Next (post S53):** perf scale B4 integration; full DOTS spawn separate track (dots-spawn worktree); dedicated MASS scenario data for 5k profile + tools/batch-replay polish. Demo+harness now enable scale benchmarks.

Cites: release-enablement-scope-boundary-2026-06-20.md , future-sprint-roadpmap.md (post S48 horizons), implementation-tracker (Req09), mass-tier worktree isolation.

**Status:** S53 MASS tier full impl COMPLETE in this worktree (sub ID: S53-MASS-SUB-01).

**Verification evidence (read-full build/test):**
- dotnet build ProjectAegis.sln : succeeded (0 errors)
- Targeted: Data 406/406, UA 253/253 (incl BalticBatch MASS), Cli 43/43
- Full sln: ~1232 tests (monotonic >=1227 baseline); smoke 18/18
- Demo MASS: `dotnet run ... --near-future "swarm-saturation:..." --max-swarm-tier Mass` → NF spawn + csv; ~3.37s/5ticks
- GitNexus pre: impact upstream on RegisterNearFutureUnits/PlanSpawns/Run (CRITICAL ~95-97, RunBatch/CLI affected; LOW SwarmTier); post-edit detect high but scoped to expected (demo+md+prior S53)
- Cites throughout: production/release-enablement-scope-boundary-2026-06-20.md + docs/reports/future-sprint-roadpmap.md §10 + gate-matrix S53
- No default path regression (Medium preserved); isolated worktree.
