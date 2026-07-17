# Comms degradation & spoof runtime — developer guide

Scenarios can model **contested C2** (the cyber-comms GDD, req 19) with two authored, engine-agnostic
timelines under the policy JSON, both driven at tick time:

1. **Comms-state timeline** — a locked-order list of comms-quality transitions
   (`Nominal → Degraded → Denied`, and back) that fire at fixed sim ticks (`CommsTimelineSimulator`).
   The *current* comms state then gates four downstream behaviours (order delay, contact staleness,
   datalink sharing, and new engagements).
2. **Spoof-track timeline** — a list of timed "this contact id is now a spoof" activations
   (`SpoofTrackTimelineSimulator`). Once active, engaging a spoofed contact is aborted at the
   kill-chain gate.

Both are **inputs to the decision/engage tick, not part of it**: they run at the top of the
[`DelegationBridge` / Baltic replay harness](baltic-replay-harness.md) tick loop, append entries to
the order log, and set state that the decision, detection, and engagement layers read **the same
tick**. They are pure and deterministic (no RNG, no wall-clock) so they never perturb replay
goldens.

- **Source:** [`src/ProjectAegis.Delegation/Comms/`](../../src/ProjectAegis.Delegation/Comms/)
  (the runtimes) with the scenario-side DTOs in
  [`src/ProjectAegis.Sim/Scenario/`](../../src/ProjectAegis.Sim/Scenario/).
- **Related:** how to *author* the `comms`, `commsDisplay`, and `spoofTracks` JSON — including
  fields, defaults, and clamps — is in [`scenario-policy-authoring.md`](scenario-policy-authoring.md).
  The contact staleness and datalink-share effects are documented in depth by the
  [detection pipeline](detection-pipeline.md); the spoof / comms-denied aborts land in the
  [engagement pipeline](engagement-pipeline.md); the order log they emit is folded into the replay
  fingerprint per [determinism-and-replay.md](determinism-and-replay.md). This page documents what
  the comms runtimes actually **do** at tick time and how to extend them without breaking goldens.

---

## Where it lives

| File | Role |
|------|------|
| [`CommsState.cs`](../../src/ProjectAegis.Delegation/Comms/CommsState.cs) | The `Nominal(0)` / `Degraded(1)` / `Denied(2)` quality enum. |
| [`CommsTimelineSimulator.cs`](../../src/ProjectAegis.Delegation/Comms/CommsTimelineSimulator.cs) | `Drain(simTick, simTime)` — releases due comms transitions in locked order; exposes `CurrentState`. |
| [`CommsTrackStaleness.cs`](../../src/ProjectAegis.Delegation/Comms/CommsTrackStaleness.cs) | `StaleThresholdDivisor(state, display)` — how much faster contacts go stale when contested. |
| [`CommsOrderDelay.cs`](../../src/ProjectAegis.Delegation/Comms/CommsOrderDelay.cs) | `ComputeExecuteSimTick(queuedSimTick, state, display)` — player-order execution delay under degraded comms. |
| [`SpoofTrackTimelineSimulator.cs`](../../src/ProjectAegis.Delegation/Comms/SpoofTrackTimelineSimulator.cs) | `Advance(simTick)` latches active spoof ids; `IsSpoofed(contactId)` queries them. |

The scenario-side DTOs and order-log payload the runtimes read/emit:

| File | Role |
|------|------|
| [`ScenarioCommsTransition.cs`](../../src/ProjectAegis.Sim/Scenario/ScenarioCommsTransition.cs) | One authored transition `(AtTick, NewState, NodeId, Reason)`. |
| [`ScenarioSpoofTransition.cs`](../../src/ProjectAegis.Sim/Scenario/ScenarioSpoofTransition.cs) | One authored spoof activation `(AtTick, ContactId, Reason)`. |
| [`ScenarioCommsDisplaySettings.cs`](../../src/ProjectAegis.Sim/Scenario/ScenarioCommsDisplaySettings.cs) | The degraded-comms knobs: `DegradedOrderDelayTicks`, `DegradedStaleThresholdDivisor`, lag / ghost-offset (validated + clamped). |
| [`CommsStateChangeRecord.cs`](../../src/ProjectAegis.Delegation/Decision/CommsStateChangeRecord.cs) | `CommsStateChange` order-log payload `(SequenceId, SimTime, SimTick, NodeId, PreviousState, NewState, Reason)`. |
| [`CommsStateProjection.cs`](../../src/ProjectAegis.Delegation/Projection/CommsStateProjection.cs) | Rebuilds the comms HUD state from the order log (authoritative for replay); `BlocksNewEngagement`, `FormatTopBar`. |

The consumers that read the runtime state each tick:

| Consumer | What it reads |
|----------|---------------|
| [`DelegationBridge`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs) | Drives both timelines at the top of `Tick`; applies the order delay when a player order is queued; wires spoof into the engage context. |
| [`BalticReplayHarness`](../../src/ProjectAegis.Delegation.UnityAdapter/Baltic/BalticReplayHarness.cs) | Pushes `CurrentCommsState` into the detection staleness divisor and maps it to the datalink share state each tick. |
| [`PdDetectionContactSimulator`](../../src/ProjectAegis.Sim/Sensors/PdDetectionContactSimulator.cs) | `SetCommsStaleThresholdDivisor` shortens the contact stale threshold. |
| [`PlayerOrderExecutionQueue`](../../src/ProjectAegis.Delegation/Decision/PlayerOrderExecutionQueue.cs) | Holds a delayed player order until its `ExecuteSimTick`. |
| [`SimulationSession`](../../src/ProjectAegis.Delegation/Orchestration/SimulationSession.cs) | `Denied` blocks new engagements; spoofed victims set `TrackSpoofed`. |
| [`MvpEngagementResolver`](../../src/ProjectAegis.Sim/Engage/MvpEngagementResolver.cs) | Aborts a spoofed engage with `EngagementAbortReason.TrackSpoofed`. |

---

## 1. Comms-state timeline (`CommsTimelineSimulator`)

### What it does

`CommsTimelineSimulator` is the deterministic comms clock. It is built once from
`profile.CommsTransitions` (via `TryCreate`, which returns `null` when no transitions are authored),
**sorted by `AtTick` ascending**, and it holds a single `CurrentState` (starting at `Nominal`).

Each tick, `Drain(simTick, simTime)` releases every transition whose `AtTick <= simTick` through a
monotonic cursor (`_nextIndex`, so each transition fires exactly once), and for each **actual state
change** emits a `CommsStateChangeRecord`:

```csharp
while (_nextIndex < _transitions.Length && _transitions[_nextIndex].AtTick <= simTick)
{
    var transition = _transitions[_nextIndex++];
    var next = ParseState(transition.NewState);
    if (next == _current) continue;              // no-op transitions are dropped
    emitted.Add(new CommsStateChangeRecord(0, simTime, simTick, transition.NodeId, _current, next, transition.Reason));
    _current = next;
}
```

- **Tolerant parse.** `ParseState` is case-insensitive and falls back to `Nominal` for any
  unrecognised string, so a typo'd `newState` silently degrades to nominal rather than throwing.
- **No-op dedup.** A transition to the state the net is already in is skipped and emits nothing —
  so it produces no order-log entry and **no fingerprint delta** (see [Determinism](#determinism)).

### Order log + C2

Each emission is appended via `DecisionLog.AppendCommsStateChange` as an
`OrderLogEntryKind.CommsStateChange` (`= 14`) entry and folded into the decision-log **fingerprint**
as `{SimTick}|{NodeId}|{PreviousState}|{NewState}|{Reason}`. The C2 top bar rebuilds the current
state purely from the log via `CommsStateProjection.Project` (authoritative for replay), which also
supplies the `COMMS: NOMINAL/DEGRADED/DENIED` label and the `BlocksNewEngagement` predicate below.

---

## 2. What `Degraded` / `Denied` actually do

`CurrentCommsState` is read by four independent gates. None of them fire while comms are `Nominal`
(or when no comms timeline is authored — `CurrentCommsState` is then always `Nominal`).

| Effect | Active state(s) | Mechanism |
|--------|-----------------|-----------|
| **Player-order execution delay** | `Degraded` only | `CommsOrderDelay.ComputeExecuteSimTick` adds `display.DegradedOrderDelayTicks` to the queued tick; the order is enqueued with that `executeTick` and held by `PlayerOrderExecutionQueue` until `currentSimTick >= executeTick`. |
| **Faster contact staleness** | `Degraded` + `Denied` | `CommsTrackStaleness.StaleThresholdDivisor` → `PdDetectionContactSimulator.SetCommsStaleThresholdDivisor`; the effective stale threshold becomes `max(1, staleThresholdTicks / divisor)`. See [detection-pipeline.md](detection-pipeline.md). |
| **Datalink share gating** | `Degraded` + `Denied` | The harness maps `CurrentCommsState` to `DatalinkCommsShareState` and passes it to `DatalinkSidePictureMerger.Merge`: `Denied` shares nothing; `Degraded` is gated. See [detection-pipeline.md](detection-pipeline.md). |
| **New-engagement block** | `Denied` only | `CommsStateProjection.BlocksNewEngagement(Denied)` → `SimulationSession` aborts **every** engage order that tick with a `PolicyDenial` (`FireAbortReason.CommsDenied`, agent `comms-guard`) before any gate runs. |

Note the deliberate asymmetry: `Denied` does **not** add order delay (order delay maps only
`Degraded`) because `Denied` already blocks new engagements outright, and the delay knob would be
moot for them.

### Player-order delay detail

The delay applies **only to player-queued orders** (source `"player"`), at enqueue time in
`DelegationBridge`:

```csharp
var executeTick = CommsOrderDelay.ComputeExecuteSimTick(simTick, CurrentCommsState, commsDisplay);
human.Enqueue(new Order(...), executeTick);
Orchestrator.DecisionLog.AppendPlayerOrder(new PlayerOrderRecord(..., ExecuteSimTick: executeTick));
```

`PlayerOrderRecord.ResolvedExecuteSimTick` (`ExecuteSimTick == 0 ? SimTick : ExecuteSimTick`) is
folded into the fingerprint, so changing the delay changes the golden. Agent-generated orders are
not delayed.

---

## 3. Spoof-track timeline (`SpoofTrackTimelineSimulator`)

### What it does

`SpoofTrackTimelineSimulator` is built from `profile.SpoofTransitions` (via `TryCreate`, `null` when
none authored), sorted by `AtTick`. `Advance(simTick)` walks its monotonic cursor and adds every due
`ContactId` to a `HashSet` of active spoofs. Activation is **latching** — once a contact id becomes
spoofed it stays spoofed for the rest of the run (there is no clear edge). `IsSpoofed(contactId)`
answers membership (null/empty ids are never spoofed).

### How a spoof aborts an engage

`DelegationBridge` wires the spoof runtime into the engage path two ways:

1. `Session.IsContactSpoofed = (contactId, _) => _spoofTimeline.IsSpoofed(contactId)` — so
   `SimulationSession` stamps `TrackSpoofed` on the resolved victim.
2. The bridge's engage-context builder sets `TrackSpoofed` for the primary hostile contact directly.

The kill-chain resolver then aborts early:

```csharp
if (ctx.TrackSpoofed)
    return EngageResult.Aborted(EngagementAbortReason.TrackSpoofed);   // enum value 15
```

`EngagePreviewProjection` surfaces the same condition so the C2 engage preview shows a spoofed track
as un-engageable. Spoof classification is pure id-membership — no RNG, no geometry.

---

## Tick ordering (why comms/spoof take effect the same tick)

Both timelines are driven at the **top** of `DelegationBridge.Tick`, before the decision/engage tick
for that tick (and the harness applies the derived detection/datalink state around the same loop):

1. **`EmitCommsTransitions`** → `CommsTimelineSimulator.Drain` appends any due `CommsStateChange` and
   advances `CurrentCommsState`.
2. **`AdvanceSpoofTimeline`** → `SpoofTrackTimelineSimulator.Advance` latches newly active spoof ids.
3. Fuel/logistics transitions (separate subsystem) are emitted.
4. Harness: `pdSim.SetCommsStaleThresholdDivisor(CommsTrackStaleness.StaleThresholdDivisor(CurrentCommsState, …))`
   and `merger.Merge(…, MapDatalinkCommsShareState(CurrentCommsState))`.
5. The decision + engage tick runs: a `Denied` net blocks new engagements, and a spoofed victim is
   aborted — both using the state set in steps 1–2 **this tick**, with no one-tick lag.

The player-order delay is the one effect that spans ticks by design: the order is *queued* now with a
future `executeTick` and *executed* later when the queue releases it.

---

## Determinism

Both runtimes are pure functions of their inputs and therefore replay-safe:

- **No wall-clock, no RNG.** Neither reads `DateTime` nor draws from a `SeededRng`; timing comes only
  from the passed `simTick` / `simTime`.
- **Fixed evaluation order.** Both transition lists are pre-sorted by `AtTick` at construction; the
  cursors are monotonic (fire-once), and spoof activation is latching.
- **No-op dedup.** A comms transition to the current state emits nothing and leaves the fingerprint
  unchanged. If you author a `Degraded` transition but the net is already `Degraded`, there is no
  `CommsStateChange` entry and no golden delta.
- **Order-log fold.** Every `CommsStateChange` (and the `ResolvedExecuteSimTick` on each delayed
  `PlayerOrder`, and each `CommsDenied` `PolicyDenial`) is folded into the decision-log fingerprint
  and thus into the replay golden. Changing when/whether comms degrade, the delay/divisor knobs, or a
  spoof activation *will* change the golden — treat it like any other golden-affecting change (the
  Baltic v3 `comms-challenged` goldens live under `tests/regression/`; never touch v2 goldens — see
  `AGENTS.md`).

---

## Extending it

- **Add a comms transition:** author it under `comms` (`atTick`, `newState`, `nodeId`, `reason`). No
  C# change needed. Re-generate the affected `*-comms-challenged` replay golden.
- **Tune degradation strength:** adjust `commsDisplay.degradedOrderDelayTicks` (`[0, 10]`) and
  `degradedStaleThresholdDivisor` (`[1, 8]`). Both are validated/clamped in
  `ScenarioCommsDisplaySettings`; see the authoring reference for the full field table and clamps.
- **Add a spoof activation:** add an entry under `spoofTracks` (`atTick`, `contactId`, `reason`).
  Remember activation is latching — there is no "spoof ends" edge today.
- **Add a new comms-gated behaviour:** read `CurrentCommsState` (or, for replay-authoritative UI,
  `CommsStateProjection.Project`) at the boundary where the effect applies; keep the *decision*
  itself pure and gate outside it. Cover it with a `BalticReplayHarnessComms*` test and regenerate
  the comms goldens.

---

## Related

| Doc | What |
|-----|------|
| [scenario-policy-authoring.md](scenario-policy-authoring.md) | Authoring the `comms` / `commsDisplay` / `spoofTracks` JSON, fields + defaults + clamps. |
| [detection-pipeline.md](detection-pipeline.md) | The contact-staleness divisor and datalink comms-share gating in depth. |
| [engagement-pipeline.md](engagement-pipeline.md) | The kill-chain gate that aborts spoofed tracks / comms-denied engages. |
| [agent-decision-pipeline.md](agent-decision-pipeline.md) | The decision tick that runs after the comms/spoof state is set. |
| [baltic-replay-harness.md](baltic-replay-harness.md) | The runner that wires both runtimes into the tick loop. |
| [c2-projection-layer.md](c2-projection-layer.md) | The comms HUD / top-bar projection rebuilt from the order log. |
| [mission-editor-cli.md](mission-editor-cli.md) | The headless `scenario_comms_status` / `scenario_cyber_status` snapshot verbs. |
| [determinism-and-replay.md](determinism-and-replay.md) | Order-log fingerprint / world-state hashing and the golden workflow. |
| [`docs/architecture/`](../architecture/) | ADR-002 (policy evaluator), cyber-comms GDD (req 19). |
