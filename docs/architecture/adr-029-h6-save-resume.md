# ADR-029: H6 Save/Resume Model and Multiplayer Boundary vs DelegationBridge / SimulationSession ZERO-Touch

## Status

**Proposed — 2026-10-06; DRG-328 decided 2026-10-07 00:10 CT.** The owner picked **Option B** from the H6 product decision brief: single-player, same-build/same-catalog save/resume in H6, and multiplayer in a separate later horizon under its own ADR. See [Owner Decisions](#owner-decisions-drg-328-2026-10-07).

**Numbering:** ADR-027 is on `main` (Unity MCP stack). ADR-028 is claimed by the parallel H5 draft (`ADR-028-h5-addressables.md`). ADR-028 lands as `adr-028-content-pipeline-addressables.md`.

## Date

2026-10-06

## Last Verified

2026-10-06 — against `main` `434b3cf6cf0dc64ef285d47e6f78857779369636` (PR #693). Symbol locations re-checked by grep: `SimulationSession.Tick` L214, `TickHeadless` L231, `PauseSim`/`ResumeSim`/`TryResumeSim` L138–142, `SetTimeAccelerationFactor` L210; `DelegationBridge.Tick` L130, `TryEnqueueHumanOrder` L167, `TryIssuePlayerCommand` L210, `TryEnqueueAttackOption` L249, `TryTakeDirectControl` L282, `AttachReplayViewer` L100; `BalticReplayHarness.Run` L88; `ReplayCheckpointStore.Record`/`FindAtOrBefore` L10/L20; `CatalogSnapshotHasher.ComputeSha256Hex` L11; `DelegationOrchestrator.TryApprovePendingOrder`/`TryRejectPendingOrder`/`TryRebindAgentTraits` L94/L101/L394. No tests were run. GitNexus impact was not run for this draft; it is required before any implementation PR.

## Decision Makers

Owner / Technical Director (drg amtd) — sole decision maker. Linear: [DRG-329](https://linear.app/drgamtd-workspace/issue/DRG-329) (this ADR), blocked on [DRG-328](https://linear.app/drgamtd-workspace/issue/DRG-328) (product decision). Parent epic [DRG-325](https://linear.app/drgamtd-workspace/issue/DRG-325). Inputs: [DRG-330](https://linear.app/drgamtd-workspace/issue/DRG-330) spike and review. Downstream: [DRG-348](https://linear.app/drgamtd-workspace/issue/DRG-348) (save contract), [DRG-349](https://linear.app/drgamtd-workspace/issue/DRG-349) (first slice).

## Owner Decisions (DRG-328, 2026-10-07)

Owner (drg amtd) accepted CMANO's picks at 00:10 CT:

- **Option B:** single-player save/resume via an input journal outside `DelegationBridge`, replayed from tick 0 on load. Multiplayer moves to a later horizon.
- **Slots:** 3 manual slots plus 1 rolling autosave. Mid-engagement saves are allowed.
- **Compatibility:** a load requires the same build, catalog and scenario.
- **Restored state:** orders, pending approvals and the override are restored. The sim resets to paused, and watch acknowledgements are dropped.
- **Load cost:** re-sim from tick 0 is acceptable for v1. Revisit if a load takes more than about 30 s.
- **Waiver:** W0 (no waiver).
- **Multiplayer mode:** deferred.

## Summary

H6 needs game-state save/resume, and none exists on `main`. `ReplayCheckpointStore` holds only `(SimTick, WorldHash, LogFingerprint, LastSequenceId)`. The hard constraint is ZERO-touch on the `DelegationBridge` hotpath (`AGENTS.md`) and on the frozen `SimulationSession` hub, unless the owner grants a product waiver. This ADR proposes **replay-to-tick restore driven by a versioned save envelope plus an external input journal**. A new driver outside the bridge and session rebuilds a fresh candidate session through existing public APIs, then verifies it against recorded fingerprints before swapping it in. **No product waiver is required** for the first slice. Multiplayer is out of scope here and needs its own ADR.

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6.3 LTS client; save driver in engine-free .NET (`ProjectAegis.Delegation` / UnityAdapter headless layer) |
| **Domain** | Core / Simulation / Persistence |
| **Knowledge Risk** | MEDIUM — the determinism contract is well documented, but interactive restore parity has not been demonstrated |
| **References Consulted** | ADR-001, ADR-003, ADR-004, ADR-006, ADR-007, ADR-010, frozen-hub addendum (2026-06-20); `docs/engineering/determinism-and-replay.md`; `production/agentic/h6-save-model-spike-2026-09-27.md`; `production/agentic/h6-save-model-review-2026-10-01.md`; Req 17 RPL-29…31; GDD `simulation-core-time.md` L126, `order-log-replay.md` L80/L97 |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | DRG-349 differential proof (uninterrupted vs resumed); restore-latency measurement; GitNexus impact on every touched symbol |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | DRG-328 product decision (Option B assumed); ADR-003 Order Log Schema; ADR-004 Tick Pipeline Order; frozen-hub addendum; ADR-006 Data Layer Boundary |
| **Enables** | DRG-348 save contract; DRG-349 first save slice; later save UI (ADR-010 / 007 / 001) |
| **Blocks** | Any H6 `src/` work; any later multiplayer ADR that reuses the input journal |
| **Ordering Note** | Product decision → this ADR Accepted → DRG-348 contract → DRG-349 slice and proof → save UI. Multiplayer is a separate track. |

## Context

### Problem Statement

The game cannot be saved and resumed. The GDD says save/load "restores simTick, scenarioSeed, RNG stream counters (checkpoint + log tail)". The code doesn't match that: sim RNG is stateless, delegation RNG state is private (`Decision/SeededRng._state`), and checkpoints are hash-only. Any design must keep the Baltic v2 golden hash and ReplayGolden 6/6, and must stay off the bridge hotpath and the frozen hub.

### Current State (verified on `434b3cf6`)

- **No game-state save/load.** Scenario *authoring* save/load (`ScenarioAuthoringSession`) is separate and out of scope.
- **Checkpoints are a verify boundary only.** `ReplayCheckpointStore.Record`/`FindAtOrBefore` store hashes and sequence metadata, recorded every `ScenarioReplaySettings.CheckpointIntervalTicks` (default 300). There is no hydrate.
- **Re-simulation exists for agent-vs-agent.** `BalticReplayHarness.Run(seed, scenarioPolicyId, ticks, …)` re-sims from tick 0 and records checkpoints. Its private `RunCore` performs the scenario mesh setup. It takes no human-input stream and no resume envelope.
- **Not reproducible from `(scenario, seed, DecisionLog)` alone** (spike §2, review table): clock pause and acceleration (pause is also set by the watch auto-pause path); pending approve/reject (no log append); trait rebind (no log append); one-shot `NextEngageSalvoOverride`; watch ack/dismiss; explicit force-resume; `RiskLevel` and salvo payload, which are not on `PlayerOrderRecord`; and the split between the observed tick `(ulong)state.SimTime` and the pipeline `SimClock.SimTick` under acceleration.
- **Catalog identity is partial.** `CatalogSnapshotHasher` hashes sensor rows only, not weapons, mounts, damage or mobility.
- **The frozen hub has CRITICAL blast radius.** It was 68 impacted symbols in June. The 2026-09-30 provisional figure is 111 direct callers and 360 impacted symbols.

### Constraints

- `DelegationBridge` hotpath: ZERO new lines (`AGENTS.md` L147/L150). `TryEnqueueHumanOrder`/`TryIssuePlayerCommand` may be **called**, not edited.
- `SimulationSession`: frozen through Release v1.0 unless the owner waives that (frozen-hub addendum; `local-cloud-agent-routing.md` L80).
- Baltic v2 hash `17144800277401907079` and ReplayGolden 6/6 must be preserved. **No silent re-bless.** Baltic v3 goldens stay isolated.
- ADR-003: save-only data must not enter `DecisionLog.ComputeFingerprint` input. ADR-004: tick order is unchanged.
- `CatalogWriteGate`: extend-only. A save never writes through it.
- Presentation wall: ADR-010 §2–3, ADR-007, ADR-001. Save/load UI is a command-façade client. It does not edit `MapPlaceholderPanelHost` or `GlobeMapProductHost`.
- No reflection or private-field hydration to bypass the boundary (review, 2026-10-01).

### Requirements

- RPL-29 deterministic resume; RPL-30 explicit compatibility (same build, scenario, catalog and policy); RPL-31 non-mutating failure with isolated candidate restore.
- DRG-349 acceptance: continuous N+M ticks equals save at N, restore, then M ticks, for world hash, order-log sequence and content, and replay fingerprint. Include pending engagements, orders and triggers, and corrupt or incompatible fixtures.

## Decision

**Proposed (contingent on DRG-328 Option B).**

1. **Save model: (a) replay-to-tick, with an external input journal.** A save file is a versioned side-file envelope. It is never part of `DecisionLog` or `SimWorldHash`, and contains:
   - **Identity:** schema version; exact build id; scenario id and content hash; full catalog snapshot identity (`DbSnapshotStore` snapshot id plus a whole-catalog hash, not the sensor-only `CatalogSnapshotHasher` output alone); policy ids and hashes; global seed.
   - **Position:** observed tick (`state.SimTime`), pipeline tick (`SimClock.SimTick`), last sequence id.
   - **Input journal:** an ordered list of every external public-API call that changed the run, with tick and arguments: `TryTakeDirectControl` and `TryReleaseDirectControl`; `TryEnqueueHumanOrder`; `TryIssuePlayerCommand` (CMD-31, keeping `commandId`); `TryEnqueueAttackOption` (preserves the salvo override); `TryApprovePendingOrder` and `TryRejectPendingOrder`; `TryRebindAgentTraits`; `PauseSim`, `TryResumeSim(explicitOverride)` and `SetTimeAccelerationFactor`; and any in-scope watch actions.
   - **Verification:** `DecisionLog.ComputeFingerprint()` SHA-256 via `OrderLogReplayFingerprint`, `Sim.LastWorldHash`, and the checkpoint rows up to the save tick.
2. **Journal capture sits at the command façade, not in the bridge.** A new `SessionInputJournal` type wraps the call site that UI, CLI and MCP commands already route through (ADR-010). It records the call and then forwards it unchanged to the existing public method. `DelegationBridge` and `SimulationSession` get no new members and no new lines. CMD-31/32/34 seams are wrapped, not modified.
3. **Restore runs in an isolated candidate session.** A new `SaveResumeDriver`:
   - validates the envelope before anything is constructed (RPL-30);
   - builds a **fresh** `DelegationBridge` with the pinned catalog bytes and `AttachReplayViewer = false`;
   - replays scenario setup and journal calls tick by tick through the same stepper the harness uses, up to the save tick;
   - compares fingerprints, world hash and checkpoints;
   - only then hands the candidate session to the host.

   Any failure discards the candidate, so the active session, RNG, queues and log are unchanged (RPL-31).
4. **Use one shared stepper and do not fork `RunCore`.** The first slice extracts the scenario-mesh and per-tick append sequence from `BalticReplayHarness.RunCore` into a reusable stepper that both the harness and the driver call. The extraction is a harness refactor, not a session or bridge edit. Its gate is **byte-identical** v2 and v3 golden output (see the replay-hash plan).
5. **No skip-to-checkpoint in H6.** Restore re-sims from tick 0, so cost is O(T). Checkpoints are used only for verification. Options (b) full snapshot and (c) hybrid are deferred because a faithful version needs restore setters on the frozen hub or private state export, and that needs a waiver.
6. **Watch ack/dismiss is presentation.** It is dropped on load and the cards are rebuilt from the replayed sim (`WatchAttentionQueue.cs` L6–7), unless DRG-328 says otherwise.
7. **Product waiver: not required** for items 1–6. A waiver is requested only if DRG-349 proves some state cannot be reproduced through public calls, or the measured restore latency breaks the owner's ceiling. Any waiver must name exact methods, carry a GitNexus impact report and get separate owner sign-off (see the waiver ladder).
8. **Multiplayer: out of scope.** A later multiplayer ADR may reuse the input journal as a lockstep or async exchange format.

### Architecture

```
 UI / CLI / MCP command (ADR-010)
          |
   SessionInputJournal  --record-->  save envelope (side file; not in DecisionLog/SimWorldHash)
          | forward unchanged
   DelegationBridge public API (TryEnqueueHumanOrder, TryIssuePlayerCommand, ...)  [ZERO edits]
          |
   SimulationSession (frozen)  ---  DelegationBridge.Tick (called, never edited)

 Load:  envelope --validate--> SaveResumeDriver --fresh bridge + shared stepper + journal replay--> candidate
        candidate --fingerprint/world-hash/checkpoint match--> swap into host  | mismatch --> discard (active untouched)
```

### Touched symbols per option (DRG-329 AC)

| Option | Edits `DelegationBridge` hotpath? | Edits `SimulationSession`? | Other touched symbols | Waiver |
|---|---|---|---|---|
| (a) Replay-to-tick + journal (**chosen**) | No: calls `Tick`, `TryEnqueueHumanOrder`, `TryIssuePlayerCommand`, `TryEnqueueAttackOption`, `TryTake/ReleaseDirectControl` | No: calls `PauseSim`, `TryResumeSim`, `SetTimeAccelerationFactor`, `Tick`/`TickHeadless` | New: `SessionInputJournal`, `SaveResumeDriver`, save envelope DTO, shared stepper extracted from `BalticReplayHarness.RunCore`; command-façade call sites rerouted through the journal | Not required |
| (b) Full state snapshot | No hotpath line, but bridge-private `_commsTimeline`/`_spoofTimeline`/`_fuelTimeline` need export | **Yes**: restore setters for clock, magazines, FSMs, BDA, salvo override, human queue | `Decision/SeededRng` state export; `AgentController._nextDecisionSimTime` | Required (broad) |
| (c) Hybrid checkpoint + log tail | Same as (b) at checkpoint boundaries | **Yes** (world hydrate) | `ReplayCheckpoint` schema extended with a world blob, which is an ADR-003 checkpoint-schema change | Required (broad) |

## Alternatives Considered

### Alternative 1: Full state snapshot (b)

- **Description**: Serialize every live object and hydrate it in place.
- **Pros**: O(1)-ish restore time; matches the GDD "world-state snapshot" wording.
- **Cons**: Needs load setters on the frozen hub plus private RNG and timeline export. A single missed field looks like a successful load and diverges later. Every new subsystem forever needs serializer upkeep.
- **Rejection Reason**: Violates ZERO-touch without a broad waiver, and carries high determinism risk for a sole developer.

### Alternative 2: Hybrid checkpoint + log tail (c)

- **Description**: Periodic world blob plus a tail of inputs.
- **Pros**: Bounded restore latency.
- **Cons**: Inherits all of (b)'s hydrate surface, and also changes the checkpoint schema (ADR-003 exposure).
- **Rejection Reason**: Deferred. Revisit only if measured O(T) latency breaks the owner's ceiling. It becomes its own ADR amendment plus a waiver.

### Alternative 3: Persist `DecisionLog` and re-apply it as input

- **Description**: Save the full order log and feed it back in.
- **Cons**: It double-applies sim-generated rows, and re-enqueue appends new rows (spike condition 4). The log also lacks approvals, traits, salvo, pause and acceleration.
- **Rejection Reason**: Incorrect. The log is a **checksum**, not an input stream.

## Consequences

### Positive

- No bridge or session edits, and no waiver for the first slice.
- Golden hash unaffected by construction. The save is a side file.
- The journal is a natural basis for a later async or lockstep multiplayer ADR.
- A save is verifiable: a load either reproduces the recorded fingerprints or is rejected.

### Negative

- Restore time grows linearly with session length, and acceleration multiplies pipeline steps (up to 256 per observed tick).
- Every new player-facing command must be routed through the journal, or saves silently lose it. Mitigation: a CI test that enumerates the public command façade.
- Saves are tied to the exact build and catalog. A patch invalidates existing saves.

### Neutral

- GDD `simulation-core-time.md` L126 ("RNG stream counters") and `order-log-replay.md` L80 ("world-state snapshot") need amending to "replay from tick 0 plus input journal; checkpoints verify". That is a docs change under DRG-348.

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|-----------|
| Shared-stepper extraction changes v2/v3 append order, so the golden moves | Medium | High | Refactor-only PR; ReplayGolden 6/6, hash grep, and v3 corpus byte-identical; on any diff, revert. No re-bless. |
| Unjournaled UI action causes a silent desync on load | Medium | High | Fingerprint check rejects the load (RPL-31). A façade enumeration test fails CI when a public command bypasses the journal. |
| Restore latency unacceptable for long sessions | Medium | Medium | Measure in DRG-349. If above the owner's ceiling, open the (c) amendment plus waiver W2. |
| Catalog identity false match (sensor-only hash) | Medium | High | Envelope pins the whole-catalog snapshot id and hash. Mismatch is a hard fail. |
| Observed vs pipeline tick split under acceleration | Medium | Medium | Envelope stores both ticks. Differential tests run at factors 1 and >1. |
| Hidden non-public state found during DRG-349 | Low–Med | High | Stop. Return to the owner with W1 (named accessors only). No reflection. |

## Performance Implications

Load = O(T) full ticks plus O(journal rows). No Baltic ms/tick figure exists yet: the swarm 2000 ms soft gate is a different workload and is not a proxy. Saving is O(journal) plus hashing and is cheap. Journaling adds one append per player command and nothing to the tick hotpath. DRG-349 must record restore wall-clock at several values of T, at acceleration factors 1 and >1.

## Replay-hash plan (DRG-329 AC)

- **Preserve.** v2 `17144800277401907079` and ReplayGolden 6/6 are unchanged by design: side-file envelope, new test scenario ids for save tests, no `DecisionLog`, `SimWorldHash`, ADR-004 order, or v2 JSON edits.
- The stepper extraction PR must show identical `WORLD_HASH` and log fingerprints for all `tests/regression/replay-golden-baltic-v2-*` and `baltic-v3-*` goldens. The hash must remain in the 18 `tests/` and `data/` paths (grep count on `434b3cf6`).
- **No re-bless under this ADR.** Any golden diff means the change is wrong. A deliberate re-bless needs a separate ADR amendment and owner sign-off.

## Catalog

Saves pin the catalog snapshot identity and are read-only against `DbSnapshotStore` and `CatalogSnapshotBinder`. They never call `CatalogWriteGate`. Load aborts if the identity does not match (ADR-006).

## Presentation

After a successful candidate swap, the host re-publishes snapshots and projections. `MapPlaceholderPanelHost` and `GlobeMapProductHost` rebind through their existing projection inputs (ADR-007 Phase A, ADR-010 §2–3, ADR-001), with no edits to either host. The save and load UI issues commands through the façade only.

## Waiver ladder (answer in DRG-328)

| Level | Grants | When |
|---|---|---|
| **W0 — No waiver (proposed)** | Nothing. Zero-touch holds through Release v1. | Default for H6 slice 1 |
| **W1 — Narrow, conditional** | Named read-only accessors on `SimulationSession` or `Decision/SeededRng` (for example, an RNG draw counter or clock state). No `Tick`/hotpath change. GitNexus impact and separate owner ack per method. | Only if DRG-349 proves a state is not reproducible |
| **W2 — Broad** | Load/hydrate API on `SimulationSession` for (b)/(c) | Only if the latency ceiling is broken. Separate ADR amendment. |

## Contingencies if DRG-328 picks another option

- **Option A (save only, multiplayer never in v1):** this ADR is unchanged. Delete decision item 8's forward reference.
- **Option C (save + hot-seat in H6):** add a second human side. `TryEnqueueHumanOrder` requires `HumanController` per slot, and fog-of-war and handoff need per-side projections. Expect W1 or higher and a presentation scope change. This needs a separate section and re-review.
- **Option D (networked multiplayer in H6):** out of this ADR. Lockstep over the journal is plausible (Req 08 §1: "custom deterministic lockstep first"), but the comms-delay recomputation inside `TryEnqueueHumanOrder` (L186–195) and authority per side touch CMD-31/32/34 seams and the deploy model. That needs a new ADR and a waiver.

## Migration Plan

Additive. First slice (DRG-349), Surface: `src/ProjectAegis.Delegation.UnityAdapter/Save/` (new `SaveEnvelope`, `SessionInputJournal`, `SaveResumeDriver`), the shared stepper extracted from `src/ProjectAegis.Delegation.UnityAdapter/Baltic/BalticReplayHarness.cs`, and tests under `src/ProjectAegis.Delegation.UnityAdapter.Tests/Save/`. No `unity/` edits in slice 1. GitNexus impact on `BalticReplayHarness` and the façade call sites is required before the PR.

**Rollback plan**: Remove the `Save/` folder and revert the stepper extraction. Goldens are unaffected because they were never re-blessed.

## Validation Criteria

- [ ] DRG-328 decided; Option B (or a contingency) recorded.
- [ ] Owner states the waiver level (W0/W1/W2) explicitly.
- [ ] Stepper extraction: ReplayGolden 6/6, v2 hash grep, and v3 goldens byte-identical.
- [ ] Differential proof: continuous N+M ticks equals save at N, restore, then M ticks, at ≥3 interruption points, including queued approvals, delayed orders, in-flight engagements and acceleration >1.
- [ ] Corrupt, truncated, wrong-build and wrong-catalog loads are rejected with before/after equality of the active session.
- [ ] Restore latency measured and within the owner's ceiling.
- [ ] `git diff` shows zero lines in `DelegationBridge` hotpath methods and in `SimulationSession`.

## GDD Requirements Addressed

| GDD Document | System | Requirement | How This ADR Satisfies It |
|---|---|---|---|
| `Game-Requirements/requirements/17-Replay-AAR-And-Order-Log.md` | Replay/Save | RPL-29 deterministic resume | Replay-to-tick plus journal, with differential proof |
| same | Replay/Save | RPL-30 compatibility | Versioned identity envelope validated before construction |
| same | Replay/Save | RPL-31 non-mutating failure | Isolated candidate session, swapped only after verification |
| `design/gdd/simulation-core-time.md` L126 | Sim core | Save/load restores tick, seed, RNG | Satisfied by re-simulation. Text amendment proposed (Neutral consequence). |
| `Game-Requirements/requirements/08-Agentic-Architecture.md` §1 | Architecture | Lockstep first when multiplayer is in scope | Journal is lockstep-compatible. Multiplayer is deferred to its own ADR. |

## Related

- [DRG-328](https://linear.app/drgamtd-workspace/issue/DRG-328), [DRG-329](https://linear.app/drgamtd-workspace/issue/DRG-329), [DRG-325](https://linear.app/drgamtd-workspace/issue/DRG-325), [DRG-330](https://linear.app/drgamtd-workspace/issue/DRG-330), [DRG-348](https://linear.app/drgamtd-workspace/issue/DRG-348), [DRG-349](https://linear.app/drgamtd-workspace/issue/DRG-349)
- `production/agentic/h6-save-model-spike-2026-09-27.md`, `production/agentic/h6-save-model-review-2026-10-01.md`
- `docs/architecture/adr-simulation-session-frozen-hub-spirit1-2026-06-20.md`, ADR-001, ADR-003, ADR-004, ADR-006, ADR-007, ADR-010
- `docs/engineering/determinism-and-replay.md`
