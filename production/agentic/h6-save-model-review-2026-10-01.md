# H6 save-model spike review — 2026-10-01

**Verdict:** CHANGES REQUIRED for an implementation-ready feasibility claim; useful read-only input to DRG-329/DRG-348. Owner acceptance pending.

**Reviewed:** [PR 685](https://github.com/drgaciw/cmano-clone/pull/685), cloud head `e3054dfd18c64bc6fe4962e232460afdbca9ac16`, [September 27 spike](https://github.com/drgaciw/cmano-clone/blob/e3054dfd18c64bc6fe4962e232460afdbca9ac16/production/agentic/h6-save-model-spike-2026-09-27.md). Consolidation only; no cloud commit copied or cherry-picked.

**Source baseline:** clean worktree `.worktrees/planning-next-20261001` at `1902dc1299678ea17f75014406fd9c616e5576bd` before planning writes. GitNexus registry identified the same-revision index at `C:\Users\dgorn\My Projects\cmano-clone\.claude\worktrees\project-requirements-review-f115ae`; path-qualified contexts corroborated Session and CheckpointStore below. The new worktree has no separately verified index. Graph query returned checkpoint test definitions, no resume execution flow; source inspection supplies the behavioral evidence.

## Verified inventory and corrections

| State / claim | Source evidence at baseline | Restore consequence |
| --- | --- | --- |
| Clock, pause, acceleration, watch state | [SimulationSession.cs](https://github.com/drgaciw/cmano-clone/blob/1902dc1299678ea17f75014406fd9c616e5576bd/src/ProjectAegis.Delegation/Orchestration/SimulationSession.cs#L130) | Pause is also caused by watch attention; the spike's “sim does not set it” wording needs correction. Ack/dismiss affects resume gating and needs an explicit persistence decision. |
| Observed time versus pipeline clock | [Session tick/cadence](https://github.com/drgaciw/cmano-clone/blob/1902dc1299678ea17f75014406fd9c616e5576bd/src/ProjectAegis.Delegation/Orchestration/SimulationSession.cs#L242) | Delegation runs before the interactive pause check. Acceleration repeats pipeline steps, not the whole delegation/logistics cadence. Save must distinguish both clocks and input timing. |
| Controllers, pending orders, approvals, agent scheduling/traits | [DelegationOrchestrator.cs](https://github.com/drgaciw/cmano-clone/blob/1902dc1299678ea17f75014406fd9c616e5576bd/src/ProjectAegis.Delegation/Orchestration/DelegationOrchestrator.cs#L87), [AgentController.cs](https://github.com/drgaciw/cmano-clone/blob/1902dc1299678ea17f75014406fd9c616e5576bd/src/ProjectAegis.Delegation/Controllers/AgentController.cs#L15) | Approval/rejection and trait changes need capture beyond executed player rows. Private decision timing and mutable delegation RNG cannot be inferred from a hash checkpoint. |
| Player log versus enqueue inputs | [PlayerOrderRecord.cs](https://github.com/drgaciw/cmano-clone/blob/1902dc1299678ea17f75014406fd9c616e5576bd/src/ProjectAegis.Delegation/Decision/PlayerOrderRecord.cs#L6), [DelegationBridge.cs](https://github.com/drgaciw/cmano-clone/blob/1902dc1299678ea17f75014406fd9c616e5576bd/src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs#L167) | Enqueue requires human control, recomputes communications delay, appends a row, and rejects replay-viewer attachment. The row lacks risk override and salvo payload. Replaying outcomes as inputs would duplicate authority changes. |
| Engagement/resource/lifecycle state | [Session fields](https://github.com/drgaciw/cmano-clone/blob/1902dc1299678ea17f75014406fd9c616e5576bd/src/ProjectAegis.Delegation/Orchestration/SimulationSession.cs#L622) | Magazines, killed targets, fuel, readiness, air/boat FSMs, damage/BDA, withdraw trials and one-shot salvo override belong in the inventory; bridge/harness timeline and sensor state also need coverage. This is not an exhaustive serialization schema. |
| Checkpoints and catalog identity | [ReplayCheckpoint.cs](https://github.com/drgaciw/cmano-clone/blob/1902dc1299678ea17f75014406fd9c616e5576bd/src/ProjectAegis.Delegation/Replay/ReplayCheckpoint.cs#L4), [CatalogSnapshotHasher.cs](https://github.com/drgaciw/cmano-clone/blob/1902dc1299678ea17f75014406fd9c616e5576bd/src/ProjectAegis.Data/Snapshots/CatalogSnapshotHasher.cs#L8) | Checkpoints contain hashes/tick/sequence metadata, not hydrateable state. CatalogSnapshotHasher covers sensor rows, not complete weapons/mounts/damage/mobility inputs. |

## Unverified feasibility and zero-touch gaps

Replay-to-tick is a candidate architecture, not demonstrated interactive save/load. Existing BalticReplayHarness.Run accepts seed/scenario/tick count but no human-input stream or resume envelope. Calling it again proves re-simulation capability, not a shipped save/load feature. Complete input capture, identical setup/append ordering, safe candidate-session replacement, and restore latency remain unproven. Work scales with replayed ticks; neither checkpoint spacing nor unrelated swarm timing establishes a latency budget.

Keep SimulationSession's frozen-hub boundary and DelegationBridge hotpath unchanged. A future external driver/shared stepper must demonstrate parity without duplicating divergent orchestration; zero-touch feasibility has not been proven for every interactive state. Do not use reflection/private hydration to bypass the boundary. Preserve v2 hash `17144800277401907079`, independent v3 fixtures, and existing CatalogWriteGate paths. Presentation follows ADR-010 §2–3, ADR-007, ADR-001.

## Minimum same-build contract and ADR inputs

DRG-328 must settle supported save boundaries, interactive state inclusion, maximum restore workload and acceptable latency. DRG-329/DRG-348 must choose reconstruction versus snapshot ownership and define:

1. Versioned envelope: exact build, scenario/catalog/policy identities and full input checksums, seed, observed/pipeline tick positions, ordered external inputs, and verification fingerprints.
2. Complete authority-state inventory: RNG/scheduling, commands/events, controller/approval state, missions, engagements/resources and relevant lifecycle state. Each field is serialized or demonstrably reconstructed; omitted state needs explicit scope justification.
3. Isolated candidate restore: corruption, incompatibility or missing input fails without changing active world, RNG, queues or log; commit only after validation.
4. Differential proof: uninterrupted versus resumed runs with identical subsequent inputs match state/order fingerprints at multiple future ticks, including queued approvals, delayed orders and engagements. Product/UI/owner acceptance remains separate.

RPL-29–31 remain proposed. Multiplayer needs a separate scope/architecture decision. No tests, performance measurements, runtime changes, ADR approval or owner signoff were performed by this review.
