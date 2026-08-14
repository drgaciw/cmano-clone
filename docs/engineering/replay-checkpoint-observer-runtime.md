# Replay checkpoints & observer-attach — developer guide

The sim is a pure function of `(scenario, seed)`, so a run can be **verified**, **scrubbed**, and
**observed** without ever storing a serialized world. The primitives that turn that guarantee into
tooling live in [`ProjectAegis.Delegation/Replay/`](../../src/ProjectAegis.Delegation/Replay/) plus
the orchestrator's `AttachReplayViewer` flag: a monotonic **checkpoint store** (world-hash +
order-log-fingerprint boundaries emitted at a fixed tick interval), the **SHA-256 fingerprint** over
the canonical order-log text, and the read-only **observer / replay-viewer mode** that blocks the
listed human-order / enqueue paths (`DoctrineOverrideCommand` is a documented exception). This page
documents that runtime as a subsystem — what each piece is, how the Baltic harness drives it, and
how to extend it without moving the golden hash.

This is the *replay-leg tooling* reference. The two hashes it records are documented as building
blocks in [deterministic-hashing-and-rng.md](deterministic-hashing-and-rng.md) (the layered
`SimWorldHash`) and [order-log-runtime.md](order-log-runtime.md) (the canonical
`ComputeFingerprint()` text); the rules and golden workflow are
[determinism-and-replay.md](determinism-and-replay.md); the harness that produces checkpoints is
[baltic-replay-harness.md](baltic-replay-harness.md). The observer mode is the enforcement half of
[direct-control-override-runtime.md](direct-control-override-runtime.md) and
[c2-command-issuance-runtime.md](c2-command-issuance-runtime.md). Source of truth for the row schema
is [ADR-003](../architecture/adr-003-order-log-schema.md); the feature spec is the
[order-log-replay GDD](../../design/gdd/order-log-replay.md) and the
[checkpoints epic](../../production/epics/order-log-replay-checkpoints-slice/EPIC.md). Verified
against source and pinned by the tests at the end.

- **Checkpoint boundary:** [`ReplayCheckpoint`](../../src/ProjectAegis.Delegation/Replay/ReplayCheckpoint.cs) —
  `readonly record` of `(SimTick, WorldHash, LogFingerprint, LastSequenceId)`.
- **Checkpoint store:** [`ReplayCheckpointStore`](../../src/ProjectAegis.Delegation/Replay/ReplayCheckpointStore.cs) —
  append-only, strictly-monotonic `Record` + read-only `FindAtOrBefore`.
- **Fingerprint hash:** [`OrderLogReplayFingerprint.ComputeSha256Hex`](../../src/ProjectAegis.Delegation/Replay/OrderLogReplayFingerprint.cs) —
  SHA-256 over `IOrderLog.ComputeFingerprint()`.
- **Observer flag:** [`DelegationOrchestrator.AttachReplayViewer`](../../src/ProjectAegis.Delegation/Orchestration/DelegationOrchestrator.cs)
  (delegated by `SimulationSession` and `DelegationBridge`).
- **Interval knob:** [`ScenarioReplaySettings.CheckpointIntervalTicks`](../../src/ProjectAegis.Sim/Scenario/ScenarioReplaySettings.cs)
  — from the scenario `replay` block (default `300`).

---

## Design invariants — never break these

Load-bearing and enforced by tests / the golden gate. Preserve them when touching any piece here.

| Invariant | Rule |
|-----------|------|
| **Checkpoints are append-only & strictly monotonic** | `ReplayCheckpointStore.Record` drops any tick that is `<=` the last recorded tick, so the list is strictly increasing and free of duplicates. `FindAtOrBefore` relies on that ordering (it `break`s past the target). Never insert, reorder, or back-date a checkpoint. |
| **Checkpoint hashes are on-disk golden contract** | The `(SimTick, WorldHash, LastSequenceId)` triples are pinned by the `replay-golden-baltic-*-checkpoints-*.txt` fixtures. The `baltic-patrol-replay` tick-4 checkpoint carries the production Baltic v2 hash `17144800277401907079` — the same value the [hard invariants](../../AGENTS.md#hard-invariants--never-break-these) protect. Changing a checkpoint hash means you changed the sim; regenerate goldens only through the [replay-verify](../../.claude/skills/replay-verify/SKILL.md) workflow. |
| **The store holds hashes, not a serialized world** | A checkpoint is a *verification boundary* (hash + last sequence id), not a snapshot you can deserialize. "Scrub-to-tick" means re-simulating deterministically to the boundary and confirming the hash matches — the determinism invariants are what make the reconstructed state identical. Do not add a state blob to `ReplayCheckpoint`. |
| **`AttachReplayViewer` blocks the listed human-order paths** | When set, these return without appending: `TryTakeDirectControl` / `TryReleaseDirectControl` (orchestrator + bridge), `TryEnqueueHumanOrder` (bridge), and `C2PlayerCommandBridge` (`REPLAY_ATTACHED`). That is **not** every write seam: `DoctrineOverrideCommand.TryApply` does **not** currently check `AttachReplayViewer`. New human-order / enqueue paths must add the guard as their **first** check; do not assume the doctrine override path is already blocked. |
| **SHA-256 is over the canonical text, unchanged** | `ComputeSha256Hex` hashes the UTF-8 bytes of `log.ComputeFingerprint()` and emits lower-hex. It adds no salt, sort, or normalization of its own — the canonicalization already happened in `ComputeFingerprint()`. Keep it a thin, pure hash so the SHA and the raw text agree. |
| **Off the `Tick` hotpath** | Checkpoints are recorded by the harness after each tick body, and `AttachReplayViewer` is a read-only property check. Neither adds work to `DelegationBridge.Tick` — keep the [zero-touch DelegationBridge hotpath](../../AGENTS.md#hard-invariants--never-break-these) intact. |

---

## The checkpoint boundary

```csharp
public sealed record ReplayCheckpoint(
    ulong SimTick,          // tick this boundary was taken at
    ulong WorldHash,        // SimWorldHash.Combine(simHash, detectionHash, 0)
    string LogFingerprint,  // IOrderLog.ComputeFingerprint() at this tick
    ulong LastSequenceId);  // SequenceId of the last order-log entry so far
```

`WorldHash` is the sim-side state fold (see
[deterministic-hashing-and-rng.md](deterministic-hashing-and-rng.md)); `LogFingerprint` is the
decision-side fold (see [order-log-runtime.md](order-log-runtime.md)). Together they pin **both**
halves of the reproducibility story at the same instant, and `LastSequenceId` records how far the
append-only log had advanced — enough to localize a divergence to a tick window and know which log
rows belong to it.

### Recording cadence

`ReplayCheckpointStore` is deliberately tiny:

```csharp
public void Record(ulong simTick, ulong worldHash, string logFingerprint, ulong lastSequenceId)
{
    if (_checkpoints.Count > 0 && _checkpoints[^1].SimTick >= simTick) return; // monotonic guard
    _checkpoints.Add(new ReplayCheckpoint(simTick, worldHash, logFingerprint, lastSequenceId));
}

public ReplayCheckpoint? FindAtOrBefore(ulong simTick)  // nearest boundary <= simTick, or null
```

The [`BalticReplayHarness`](baltic-replay-harness.md) owns the one live store. Inside its tick loop,
after the per-tick appliers run, it records on the interval boundary:

```csharp
var checkpointInterval = profile?.ReplaySettings.CheckpointIntervalTicks ?? 0; // 0 = disabled
...
if (checkpointInterval > 0 && simTick % (ulong)checkpointInterval == 0)
{
    var worldHashTick = SimWorldHash.Combine(simHash, detectionHash, 0);
    checkpointStore.Record(
        simTick,
        worldHashTick,
        bridge.Orchestrator.DecisionLog.ComputeFingerprint(),
        bridge.Orchestrator.DecisionLog.ChronologicalEntries().LastOrDefault()?.SequenceId ?? 0);
}
```

The interval comes from scenario data, so it is a content decision, not a code constant:

```jsonc
// data/scenarios/baltic-patrol-replay.policy.json
"replay": {
  "checkpointIntervalTicks": 2   // dense cadence so a 4-tick run yields checkpoints at 2 and 4
}
```

`ScenarioReplaySettings.Default` is **300** ticks; the JSON loader clamps to `Math.Max(1, …)`. When a
run has no scenario profile at all, `checkpointInterval` falls back to `0` and no checkpoints are
recorded (the run still produces its final hashes). The completed store is handed out on the
harness `Result.Checkpoints` list.

---

## The SHA-256 fingerprint

`OrderLogReplayFingerprint.ComputeSha256Hex(IOrderLog)` is the compact, fixed-width companion to the
canonical order-log text:

```csharp
var bytes = Encoding.UTF8.GetBytes(log.ComputeFingerprint()); // canonical text (order-log-runtime.md)
using var sha = SHA256.Create();
return ToHexLower(sha.ComputeHash(bytes));                     // 64-char lower-hex digest
```

It carries no logic of its own — all determinism-critical canonicalization (sequence sort, float
formatting via `FingerprintFloat`, etc.) already lives in `ComputeFingerprint()`. The harness surfaces
it as `Result.FingerprintSha256` next to the raw `Result.Fingerprint`, so gates can compare a short
digest while humans can still diff the full text.

---

## Observer / replay-viewer mode

`AttachReplayViewer` is a single boolean on `DelegationOrchestrator` (req 03 AvA observer attach).
`SimulationSession` and `DelegationBridge` expose it as a passthrough property, so a UI host can flip
one flag and put the listed human-order paths into read-only scrub mode. The table below is the
guarded set — **not** every write seam. Each listed site checks the flag **first** and bails before
mutating:

| Guard site | Path | On `AttachReplayViewer == true` |
|------------|------|---------------------------------|
| `DelegationOrchestrator.TryTakeDirectControl` | take control of a unit | returns `false`, no `ControllerChange` logged |
| `DelegationOrchestrator.TryReleaseDirectControl` | hand a unit back to its agent | returns `false` |
| `DelegationBridge.TryTakeDirectControl` / `TryReleaseDirectControl` | Unity-adapter ingress | returns `false` before touching the orchestrator |
| `DelegationBridge.TryEnqueueHumanOrder` | queue a human order | returns `false`, nothing appended |
| `C2PlayerCommandBridge` (toolbar/hotkey) | player command issuance | fails with reason `REPLAY_ATTACHED` |
| `DoctrineOverrideCommand.TryApply` | ROE override (sibling write) | **not gated** — still applies / logs `PolicyUpdateRecord` |

Because none of these append to the order log, an observer session produces the **same** fingerprint
as the underlying run — you are watching, not altering. The complementary arbitration mechanics
(suspend/resume, detach/rejoin) are in
[direct-control-override-runtime.md](direct-control-override-runtime.md); the full player-command
failure-reason catalog is in [c2-command-issuance-runtime.md](c2-command-issuance-runtime.md).

---

## Producer / consumer map

| Role | Type | What it does |
|------|------|--------------|
| **Producer** | `BalticReplayHarness` (tick loop) | Records one `ReplayCheckpoint` per interval boundary into the run's `ReplayCheckpointStore`. |
| **Producer** | `BalticReplayHarness` (result fold) | Computes `Result.FingerprintSha256` via `OrderLogReplayFingerprint`. |
| **Producer** | UI / test host | Sets `AttachReplayViewer` on the session/bridge to enter observer mode. |
| **Consumer** | `ReplayGoldenBaltic{,Intercept,Kill}CheckpointTests` | Assert `Result.Checkpoints` matches the pinned `REPLAY_CHECKPOINT=` fixtures. |
| **Consumer** | `BalticReplayHarness.DiagnoseDivergence` | Walks paired `Checkpoints` to report the **first mismatch tick** (S36-05 divergence localization). |
| **Consumer** | `ProjectAegis.Delegation.Demo` CLI | Prints `REPLAY_CHECKPOINT=tick:worldHash:lastSeq` lines for the gate to parse. |
| **Consumer** | Orchestrator / bridge guards | Read `AttachReplayViewer` to block human ingress. |

---

## Golden fixtures

Checkpoint goldens live under `tests/regression/` alongside the other replay goldens:

```
# tests/regression/replay-golden-baltic-replay-checkpoints-2026-06-02.txt
# baltic-patrol-replay seed=42 ticks=4 checkpointInterval=2
REPLAY_CHECKPOINT=2:5155818736020725847:10
REPLAY_CHECKPOINT=4:17144800277401907079:14
```

Format is one `REPLAY_CHECKPOINT=<simTick>:<worldHash>:<lastSequenceId>` line per boundary; the
`#` header comment records the run parameters. The three fixtures cover the replay, intercept, and
kill Baltic slices. `DiagnoseDivergence` uses the same triples at runtime to point at the earliest
diverging tick instead of only reporting a final-hash mismatch.

---

## Runbooks

### Enable / tune checkpoints for a scenario

Add (or edit) the `replay` block in the scenario's `*.policy.json`:

```jsonc
"replay": { "checkpointIntervalTicks": 60 }
```

No code change is required — the harness reads it via `ScenarioReplaySettings`. Choose an interval
that divides your tick budget so boundaries land on round ticks. If you add a **new** checkpoint
golden, run [`/replay-verify`](../../.claude/skills/replay-verify/SKILL.md), capture the emitted
`REPLAY_CHECKPOINT=` lines, and commit them as a dated `replay-golden-*-checkpoints-*.txt` fixture.
Never hand-edit a hash to make a test pass — a changed hash means the sim changed.

### Add a new human-ingress path

Any new method that mutates controllers or appends a human order **must** short-circuit on the
observer flag as its first statement, mirroring the existing guards:

```csharp
public bool TryDoSomethingHumanly(...)
{
    if (AttachReplayViewer) return false; // (or set REPLAY_ATTACHED and return false)
    ...
}
```

Keeping the guard first is what preserves the observer-mode invariant and the comms-delay queue
(see the "already human" no-op note in `TryTakeDirectControl`). Add an adversarial test alongside the
existing `TryTakeDirectControl_returns_false_when_AttachReplayViewer_enabled` fixtures.

### Extend the checkpoint payload

`ReplayCheckpoint` fields feed the goldens, so treat them as append-only: adding a field forces a
golden regen and a fixture-format bump. Prefer deriving new diagnostics from the existing
`(WorldHash, LogFingerprint, LastSequenceId)` triple. Never add a serialized world blob — that would
break the "hashes, not state" invariant and bloat every fixture.

---

## Pinned by tests

| Test | Guards |
|------|--------|
| `ReplayGoldenBalticCheckpointTests` | `baltic-patrol-replay` checkpoints match the golden triples (incl. tick-4 v2 hash). |
| `ReplayGoldenBalticInterceptCheckpointTests` / `ReplayGoldenBalticKillCheckpointTests` | Intercept / kill slice checkpoint goldens. |
| `BalticReplayHarnessReplayTests` | Re-running a seed yields the **same** checkpoint count, ticks, and world hashes. |
| `OrderLogReplayFingerprintSha256Tests` | SHA-256 is stable for identical logs and differs for changed logs. |
| `OrchestratorOverrideTests` | `AttachReplayViewer` blocks `TryTakeDirectControl` / `TryReleaseDirectControl`. |
| `SimulationSessionObserverTests` | `SimulationSession.AttachReplayViewer` delegates to the orchestrator. |
| `DelegationBridgeTests` / `C2PlayerCommandBridgeTests` | Bridge ingress returns `false` / `REPLAY_ATTACHED` under observer mode. |
