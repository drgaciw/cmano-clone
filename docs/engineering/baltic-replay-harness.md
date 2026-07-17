# Baltic replay harness — developer guide

`BalticReplayHarness` is the **single headless entry point** that turns a
`(seed, scenarioPolicyId, ticks)` triple into a fully-resolved, deterministic simulation
`Result`. It composes the whole delegation → detection → engage pipeline end-to-end **with no
Unity dependency**, so it is the runner behind the replay-golden CI gate, the QA Gauntlet, the
Mission Editor CLI `scenario_simulate_sample` verb, the console demo, and 60-plus test fixtures.

- **Source:** [`BalticReplayHarness.cs`](../../src/ProjectAegis.Delegation.UnityAdapter/Baltic/BalticReplayHarness.cs)
  (`ProjectAegis.Delegation.UnityAdapter/Baltic/`).
- **Related:** the *concepts* (hashing, seeded RNG, golden workflow) live in
  [`determinism-and-replay.md`](determinism-and-replay.md); the *adapter seam* (`DelegationBridge`,
  `ISimWorldSnapshot`, `IOrderSink`, C2 projections) lives in
  [`ProjectAegis.Delegation.UnityAdapter/README.md`](../../src/ProjectAegis.Delegation.UnityAdapter/README.md).
  This page documents what a `Run` actually **does** and how to drive/extend it.

> **Why one runner.** Every headless path that must be bit-for-bit reproducible funnels through
> this one method, so composition (ORBAT build, detection wiring, order-log append order) is
> defined in exactly one place. New scenario shapes are added *inside* the harness, not by
> forking it — that is what keeps the golden hashes and the gauntlet oracle stable.

---

## The `Run` entry point

```csharp
BalticReplayHarness.Result Run(
    int seed,
    string scenarioPolicyId,
    int ticks,                                                  // must be >= 1
    bool mvpEngagement = true,
    ICatalogReader? catalog = null,
    IReadOnlyDictionary<string, bool>? unitReadiness = null,
    IReadOnlyList<ScenarioNearFutureUnitRequest>? nearFutureUnits = null,
    int maxTechnologyLevel = 2)
```

| Parameter | Meaning |
|-----------|---------|
| `seed` | Global sim seed. Feeds `SimSeed.FromScenario` (detection) and the delegation RNG. Same seed + same inputs ⇒ identical `Result`. |
| `scenarioPolicyId` | Policy id resolved via `ScenarioPolicyRepository` (`data/scenarios/*.policy.json`). Drives detection trials, EMCON, datalink doctrine, mission timeline, contact triggers, per-unit ROE, checkpoint interval, and the optional `gauntlet.units` ORBAT. A missing/unknown id runs the legacy single-agent fallback with `EffectivePolicy.DefaultFree`. |
| `ticks` | Number of 1.0-second advances. `< 1` throws `ArgumentOutOfRangeException`. |
| `mvpEngagement` | When `true` (default) the MVP engage `Session` resolves engagements headlessly; the harness throws if the session could not be created. |
| `catalog` | Optional explicit `ICatalogReader`. When `null`, resolved by `CatalogReaderFactory.ResolveForScenario(scenarioPolicyId)` — see [`catalog-seeding.md`](catalog-seeding.md). |
| `unitReadiness` | Optional per-unit alive/withdraw overrides; falls back to `profile.UnitReadiness`. |
| `nearFutureUnits` | Optional near-future archetype spawn requests (`ScenarioNearFutureUnitRequest(archetypeId, unitId)`); planned via `NearFutureArchetypeRuntime`. |
| `maxTechnologyLevel` | Technology-level cap applied to near-future spawn planning (default `2`). |

`Run` is a thin wrapper that brackets `RunCore` with
`BalticV3SideRegistry.ClearScenarioSides()` in a `try/finally` so **scenario-scoped catalog
sides never leak between runs** (critical when a process runs many scenarios back-to-back, e.g.
the gauntlet ladder or batch runner).

### The `Result` record

```csharp
sealed record Result(
    int Seed, string ScenarioPolicyId, int Ticks,
    string Fingerprint,            // DecisionLog.ComputeFingerprint() — canonical order-log text
    string FingerprintSha256,      // SHA-256 hex of the fingerprint
    int EngagementCount,
    ulong DetectionWorldHash,      // detection-layer fold
    ulong WorldHash,               // SimWorldHash.Combine(coreHash, detectionHash, 0)
    IReadOnlyList<ReplayCheckpoint> Checkpoints,   // recorded at ReplaySettings.CheckpointIntervalTicks
    IReadOnlyList<MessageLogLine> Messages,        // AAR message-log projection
    SensorC2Snapshot SensorC2,                     // sensor C2 HUD view model
    string ScoringCsvRow,                          // losses/scoring CSV row (BLUE side)
    DecisionLog DecisionLog,                       // full order log
    IReadOnlyList<string> FireOrder);              // AC-2 fire_order (see below)
```

The two reproducibility artifacts — `WorldHash`/`DetectionWorldHash` and
`Fingerprint`/`FingerprintSha256` — are what the golden files pin. See
[`determinism-and-replay.md` → Two hashes](determinism-and-replay.md#two-hashes-world-state-vs-order-log-fingerprint).

---

## What a `Run` does (composition pipeline)

`RunCore` builds the world, ticks it, and folds the result. Each stage is deterministic and
appends to the order log in a stable order.

**1. Resolve profile + catalog + detection.**
`ScenarioPolicyRepository.EnsureDefaultJsonLoaded()` then `TryGet(scenarioPolicyId)`. The catalog
reader is the caller's or `CatalogReaderFactory.ResolveForScenario(...)`. Detection trials come
from `DetectionTrialResolver.Resolve(profile, catalog)`:

- Trials present → a `PdDetectionContactSimulator` (Pd model, EMCON, jammers, contact lifecycle).
  If `profile.DatalinkDoctrine.IsSharingEnabled`, a `DatalinkSidePictureMerger` is also built.
- No trials but `profile.ContactSeeds` present → a schedule-driven `ScenarioContactSimulator`.
- Neither → the `HeadlessSnapshot` fallback (2 contacts, has-track `true`).

**2. Build the bridge and the ORBAT.** A `DelegationBridge(seed, mvpEngagement, scenarioPolicyId,
catalog)` is created and unit `u1` is always registered as entity key 1. Then, in order:

| ORBAT source | When | What is registered |
|--------------|------|--------------------|
| **Baltic v3 fixed OOB** | `CatalogReaderFactory.IsBalticV3Scenario(id)` | `ucav-blue`, `ucav-red`, `hostile-1`. |
| **Gauntlet catalog units** | policy JSON has `gauntlet.units` | One registry unit **per** `platformId` (id = `platformId` so the magazine seeder can map shooter→catalog), side recorded in `BalticV3SideRegistry`, magazine rounds seeded from the catalog, and `CATALOG_UNIT:…` / `MAGAZINE_SEED:…` order-log events emitted. Legacy scenarios without `gauntlet.units` are untouched (ReplayGolden safety). See [`qa-gauntlet.md`](qa-gauntlet.md). |
| **Near-future archetypes** | `nearFutureUnits` supplied | Planned via `NearFutureArchetypeRuntime.PlanSpawns` (capped by `maxTechnologyLevel`); each spawn emits an `NF_SPAWN:…` event. |

**3. Assign engage agents.** The hostile target is the catalog **red surface** unit if present
(this eliminates the synthetic `hostile-1`), else any red unit, else — for blue-only joint
ORBATs — a synthetic `hostile-1`. The primary blue unit prefers the catalog **blue surface**
unit, else falls back to `u1`. Then:

- **Multi-domain:** every blue catalog unit (surface/air/sub) gets its own `FullAutonomous`
  engage agent (`a1`, `a-blue-1`, …) so air and subsurface units can actually launch, not merely
  be detected.
- **Legacy:** with no catalog units, a single `a1` agent is bound to `u1`.
- A red agent (`a-red`, `EngagePrimaryMode.BlueForce`) is bound to the hostile target when one
  exists. Patrol-candidate policies are used when `profile.DelegationSettings.UsePatrolCandidates`
  is set, otherwise a minimal `EngageOnlyPolicy`.

**4. Tick loop (`for t in 0..ticks`).** Each tick advances 1.0s and, in this fixed order:
mission-runtime emissions (`MissionTransition` / `EventFired`) → comms-staleness divisor →
detection tick (`pdSim` or `scheduleSim`) → optional datalink merge (transitions **then** shared,
order preserved) → per-transition contact-trigger ROE escalation + `AppendContactTransition` →
`bridge.Tick(snapshot, orderSink)` → drain new kills into the detection sim → BDA
contact-lifecycle "lost" transitions → checkpoint record (when
`simTick % CheckpointIntervalTicks == 0`).

**5. Fold the result.** `WorldHash = SimWorldHash.Combine(coreHash, detectionHash, 0)`; project
the message log, sensor C2, and scoring CSV; compute the fingerprint + SHA-256; resolve
`FireOrder`.

`HeadlessSnapshot` (a private nested class) implements **both** `ISimWorldSnapshot` and
`IOrderSink` — it answers contact/track/kill queries from the detection sim and no-ops
`ApplyOrder` (orders are resolved inside the engage `Session`). It is the canonical example of
the adapter integration contract.

---

## Fire order and divergence diagnosis

- **`ResolveFireOrder(missionTimeline, decisionLog)`** — returns the policy mission timeline's
  `FireOrder` when present, else the chronological `EventFired` event ids from the decision log
  (AC-2). This is what the CLI emits as the `fire_order` array.
- **`DiagnoseDivergence(Result a, Result b)`** — test-only helper (S36-05). Returns `"MATCH"` when
  both fingerprints and both hashes agree, otherwise the **first checkpoint tick** where the world
  hash or log fingerprint diverges (tick-level localization), else a final-mismatch summary. It
  does not touch the `Run` path, append order, or detection trial ordering.

---

## Who calls it

| Consumer | Entry | Notes |
|----------|-------|-------|
| **Replay-golden CI gate** | `ReplayGoldenSuiteTests` over `ReplayGoldenRegressionCatalog.All` | Runs each case twice (`A == B`), asserts pinned hashes + fingerprint fragments. 6/6 core Baltic v2. |
| **QA Gauntlet** | `BalticReplayHarnessGauntlet*Tests`, ladder + multi-domain + theater tests | Catalog-driven ORBATs via `gauntlet.units`; feeds the CSV → `gauntlet_oracle_eval` fail-closed gate ([`qa-gauntlet.md`](qa-gauntlet.md)). |
| **Mission Editor CLI** | `ScenarioSimulateSampleCommand` (`scenario_simulate_sample`) | Emits `SEED=… HASH=…` + a `fire_order` array; `worldStateSha256` prefers `FingerprintSha256` ([`mission-editor-cli.md`](mission-editor-cli.md)). |
| **Console demo** | `dotnet run --project src/ProjectAegis.Delegation.Demo -- --seed N --scenario ID --ticks N [--no-engage]` | Prints the golden fields 1:1 for regeneration. |
| **Batch runner** | `BalticBatchRunner` | Multi-seed/scenario CSV export (built on the same composition). |
| **~60 `BalticReplayHarness*Tests`** | assorted | Feature slices: comms, EMCON, jam, datalink lag, BDA lifecycle, kill persistence, mine hazard, fuel, readiness/withdraw, spoof, intercept, etc. |

---

## Extending the harness

**Add a gauntlet ORBAT scenario (no code change).** Add a `gauntlet.units` block to the policy
JSON — each entry is `{ "platformId": …, "domain": "surface|air|sub", "side": "blue|red" }`
(`ScenarioGauntletUnitJsonDto`). The harness registers, sides, seeds magazines, and assigns
engage agents automatically. Follow the schema and oracle-expect discipline in
[`qa-gauntlet.md`](qa-gauntlet.md).

**Add a new feature slice (code change inside the harness).** Wire it into `RunCore` at the
correct point in the tick order and append to the order log with a **stable** ordinal order. Then:

1. Add a `BalticReplayHarness<Feature>Tests` fixture that runs twice and asserts `A == B`.
2. If it changes an existing golden, that is a **behavior change** — re-record only the affected
   isolated golden and say so in the commit (never re-record to make a red test green).
3. Keep `baltic-v3-*` and gauntlet goldens isolated from Baltic v2.

---

## Constraints

- **Determinism is the contract.** Anything reaching a hash/fingerprint must follow
  [`determinism-and-replay.md`](determinism-and-replay.md): seeded RNG only, `FingerprintFloat`
  for floats, ordinal append order, no wall-clock/global RNG.
- **`DelegationBridge` is zero-touch** through Release v1 — add composition in the harness or the
  core, never in `DelegationBridge.Tick`.
- **Scenario sides are per-run** — never rely on `BalticV3SideRegistry` state surviving a `Run`;
  `Run` clears it before and after.
- **Baltic v2 hash `17144800277401907079`** must be preserved unless an ADR changes it.

---

## Related docs

| Topic | Doc |
|-------|-----|
| Determinism rules, hashing, golden regeneration | [determinism-and-replay.md](determinism-and-replay.md) |
| Adapter seam (`DelegationBridge`, `ISimWorldSnapshot`, C2 projections) | [ProjectAegis.Delegation.UnityAdapter/README.md](../../src/ProjectAegis.Delegation.UnityAdapter/README.md) |
| Catalog reader resolution + seeding | [catalog-seeding.md](catalog-seeding.md) |
| QA Gauntlet ladder + oracle + `gauntlet.units` | [qa-gauntlet.md](qa-gauntlet.md) |
| CLI `scenario_simulate_sample` | [mission-editor-cli.md](mission-editor-cli.md) |
| Scenario policy field reference | [scenario-policy-authoring.md](scenario-policy-authoring.md) |
| Tick pipeline order + world-hash layers | [adr-004-tick-pipeline-order.md](../architecture/adr-004-tick-pipeline-order.md) |
| Hard invariants + verification block | [AGENTS.md](../../AGENTS.md#hard-invariants--never-break-these) |
