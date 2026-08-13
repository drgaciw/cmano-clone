# C2 command issuance — developer guide

This is the **input** side of the C2 loop: how a player action in the tactical UI (a toolbar button,
an agent-roster directive, a hotkey) becomes a **queued human order** that the sim executes. It is the
mirror image of the read-model layer that draws the tactical picture
([c2-projection-layer.md](c2-projection-layer.md)) — issuance turns intent *into* the order log; the
projection layer reads the order log *out*.

The subsystem is deliberately split into a **pure, engine-agnostic resolution/validation layer**
(`ProjectAegis.Delegation/Input/`) and a thin **enqueue façade** on the Unity adapter
(`C2PlayerCommandBridge` + `DelegationBridge.TryIssuePlayerCommand`). The pure layer has *no* sim or
bridge side effects — it maps a command-id string to an `OrderKind` and returns a structured pass/fail.
The façade does the stateful part: replay/controller/registry checks, then enqueue via the existing
`TryEnqueueHumanOrder` path. Nothing here is on the `DelegationBridge.Tick` hotpath.

This runtime landed in **S108** (CMD-31 command issuance + unit-order toolbar; CMD-37 agent-roster
directives) and was extended by the logistics launch/boat commands (**LOG-08 / LOG-09…11**). S120
(DRG-155) confirmed it as the product issuance path and flagged this developer guide as the remaining
docs gap. It is verified against source and pinned by the tests listed at the end.

- **Pure resolution/validation:** [`src/ProjectAegis.Delegation/Input/`](../../src/ProjectAegis.Delegation/Input/)
  — `C2CommandIssuance` (toolbar/hotkey command-id → `OrderKind`), `AgentDirectiveIssuance`
  (agent-roster directive-id → mode change *or* order), and `C2InputActions` (remappable input-action
  stub IDs for keyboard/a11y parity).
- **Enqueue façade:** [`C2PlayerCommandBridge`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/C2PlayerCommandBridge.cs)
  and the thin non-Tick wrapper
  [`DelegationBridge.TryIssuePlayerCommand`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs).
- **Enqueue mechanics:** `DelegationBridge.TryEnqueueHumanOrder` → `HumanController.Enqueue` →
  [`PlayerOrderExecutionQueue`](../../src/ProjectAegis.Delegation/Decision/PlayerOrderExecutionQueue.cs)
  (comms-delayed release) + `DecisionLog.AppendPlayerOrder`
  ([`PlayerOrderRecord`](../../src/ProjectAegis.Delegation/Decision/PlayerOrderRecord.cs)).
- **Doctrine override (a sibling command):**
  [`DoctrineOverrideCommand`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/DoctrineOverrideCommand.cs)
  — a headless ROE override that logs a `PolicyUpdateRecord` instead of enqueuing an order.
- **Related:** the **arbitration** that decides *who* controls a target (take control / detach) is a
  different subsystem — see [direct-control-override-runtime.md](direct-control-override-runtime.md).
  The comms delay applied to issued orders is [comms-degradation-runtime.md](comms-degradation-runtime.md).
  The gate that authorizes *agent* decisions is [autonomy-roe-gating.md](autonomy-roe-gating.md) — note
  player orders **do not** re-run through it (see the invariants).

---

## Design invariants — never break these

These are load-bearing and enforced by tests. Preserve them when extending issuance.

| Invariant | Rule |
|-----------|------|
| **Pure layer has no side effects** | `C2CommandIssuance` and `AgentDirectiveIssuance` are `static` and return a result struct — they never touch the bridge, orchestrator, registry, or clock. Hosts resolve first, then enqueue/apply. This is what lets UI code preview a command (enable/disable a button, show a tooltip) without mutating sim state. |
| **Issuance requires human control** | A command only enqueues if the target's active `ControllerSlot` is a `HumanController`. An agent-controlled unit returns `NOT_HUMAN_CONTROL`; take control first via the arbitration path. The façade never mutates control slots. |
| **Replay viewer is read-only** | If `AttachReplayViewer` is set, every issue attempt fails with `REPLAY_ATTACHED` and nothing is appended. You cannot inject orders into a replay. |
| **Player orders bypass the AutonomyGate** | Human-issued orders drain **directly** from the `HumanController` queue in the orchestrator tick; they are *not* re-run through the `AutonomyGate`/ROE approval flow that gates agent decisions. Weapon release for an `Engage` order is still gated downstream in the sim engagement resolver (ROE/WRA), but the *delegation-layer* gate is agent-only. |
| **`OrderKind` ordinals are append-only** | Command ids resolve to `OrderKind` by *value*. The enum is part of the fingerprinted order log, so existing ordinals are frozen (`Move=0 … AbortBoatLaunch=11`). Add new kinds at the end only. (`OrderKind_append_preserves_existing_ordinals`.) |
| **Issuance is a non-Tick wrapper** | `TryIssuePlayerCommand` and `TryEnqueueHumanOrder` are host-facing seams, **not** part of `DelegationBridge.Tick`. Adding issuance surface must not touch the Tick hotpath (Baltic v2 hash `17144800277401907079` unchanged; ZERO `DelegationBridge` Tick edits). |
| **Every failure has a stable reason** | Both layers return machine-readable string reasons (`UNKNOWN_COMMAND`, `NO_SELECTION`, `NOT_HUMAN_CONTROL`, …) so UI status labels and tests key off codes, not free text. Don't invent ad-hoc reasons; extend the catalog below. |

---

## The two-layer model

```
                       PURE LAYER (Delegation/Input, engine-agnostic, no side effects)
UI toolbar / hotkey ──▶ C2CommandIssuance.Validate(commandId, hasSelection)
                          │  NO_SELECTION → UNKNOWN_COMMAND → OrderKind
                          ▼
                       FAÇADE (UnityAdapter/Bridge, stateful)
                       C2PlayerCommandBridge.TryIssue(bridge, entity, commandId, simTime)
                          │  UNKNOWN_COMMAND → REPLAY_ATTACHED → UNKNOWN_UNIT
                          │  → NOT_HUMAN_CONTROL → ENQUEUE_FAILED
                          ▼
                       DelegationBridge.TryEnqueueHumanOrder(entity, kind, simTime)
                          │  risk = DefaultRiskClassifier.Classify(kind)
                          │  executeTick = CommsOrderDelay.ComputeExecuteSimTick(...)
                          ▼
                       HumanController.Enqueue(order, executeTick)   ← PlayerOrderExecutionQueue
                       DecisionLog.AppendPlayerOrder(PlayerOrderRecord)
                          ▼
                       (next orchestrator tick) HumanController.DrainIssuedOrders(simTick)
                          → executed → OrderDispatcher → sink
```

`DelegationBridge.TryIssuePlayerCommand(entity, commandId, simTime, out reason)` is just the one-liner
that forwards to `C2PlayerCommandBridge.TryIssue(this, …)`, so Unity hosts (`DelegationBridgeHost`,
`AgentRosterPanelHost`) have a single call site.

### `C2CommandIssuance` — toolbar/hotkey commands (CMD-31)

Pure static helpers. `TryResolve` maps a command-id string (case- and whitespace-insensitive,
`Trim().ToLowerInvariant()`) to an `OrderKind`; `Validate` layers a selection check on top.

```csharp
public readonly record struct C2CommandResult(bool Ok, string? FailureReason, OrderKind? Kind);

public static class C2CommandIssuance
{
    public const string ReasonUnknownCommand = "UNKNOWN_COMMAND";
    public const string ReasonNoSelection    = "NO_SELECTION";

    public static bool TryResolve(string? commandId, out OrderKind kind, out string? reason);
    public static C2CommandResult Validate(string? commandId, bool hasSelection);
}
```

Command-id map (verified against source; `plot_course`, `launch`, `abort_launch` are aliases):

| Command id(s) | `OrderKind` | Notes |
|---------------|-------------|-------|
| `hold` | `Hold` | |
| `rtb` | `ReturnToBase` | |
| `move`, `plot_course` | `Move` | `plot_course` is the course-plotting alias. |
| `engage` | `Engage` | The only fire-risk order (see risk classification below). |
| `set_emcon` | `SetEmcon` | |
| `set_sensors` | `SetSensors` | |
| `launch`, `launch_aircraft` | `LaunchAircraft` | LOG-08 / CMD-24 Phase N air-ops. |
| `abort_launch`, `abort_launch_aircraft` | `AbortLaunchAircraft` | |
| `launch_boat` | `LaunchBoat` | LOG-09…11 / CMD-25 embarked-craft ops. |
| `recover_boat` | `RecoverBoat` | |
| `abort_boat_launch` | `AbortBoatLaunch` | |
| anything else / null / blank | — | fails with `UNKNOWN_COMMAND`. |

`Validate(commandId, hasSelection)` checks selection **first** (`NO_SELECTION`) then resolution
(`UNKNOWN_COMMAND`), returning `C2CommandResult(true, null, kind)` on success.

### `AgentDirectiveIssuance` — agent-roster directives (CMD-37)

The agent-roster panel issues *directives*, which are either **mode changes** (take/return control) or
**orders** (reusing the same `OrderKind` enqueue path — no new enums). `TryResolve` produces a
structured `AgentDirectiveRequest`:

```csharp
public enum AgentDirectiveAction { TakeControl, ReturnToAgent, Hold, Rtb }

public sealed record AgentDirectiveRequest(
    string DirectiveId, AgentDirectiveAction Action, bool IsModeChange, OrderKind? OrderKind);
```

| Directive id | Action | `IsModeChange` | `OrderKind` |
|--------------|--------|:--------------:|-------------|
| `take_control` | `TakeControl` | true | — (host performs a control mode change) |
| `return_to_agent` | `ReturnToAgent` | true | — |
| `hold` | `Hold` | false | `Hold` (enqueues via the human path) |
| `rtb` | `Rtb` | false | `ReturnToBase` |

Two `Validate` overloads gate on controller state:

- `Validate(directiveId, hasSelection, hasAgent)` — selection required (`NO_SELECTION`), resolve
  (`UNKNOWN_DIRECTIVE`); **mode-change** directives additionally require a related agent (`NO_AGENT`).
  Order directives (`hold`/`rtb`) are allowed without an agent.
- `Validate(directiveId, hasSelection, hasActiveAgent, hasSuspendedAgent)` — the precise variant:
  `take_control` needs an **active** agent (`NO_ACTIVE_AGENT`); `return_to_agent` needs a **suspended**
  agent (`NO_SUSPENDED_AGENT`). This mirrors the `OverrideService` suspend/resume semantics in
  [direct-control-override-runtime.md](direct-control-override-runtime.md).

Hosts execute mode-change requests through the arbitration API (`TryTakeDirectControl` /
`TryReleaseDirectControl`) and order requests through `TryEnqueueHumanOrder`.

### `C2InputActions` — remappable action IDs

Constant IDs that are the single source of truth for the keyboard actions the sim resolves at session
start (req 20 keyboard parity / accessibility §6.3): `CycleUnit` (`input.cycle_unit`),
`FocusPrimaryThreat` (`input.focus_primary_threat`), `Cancel` (`input.cancel`). UI hosts bind default
keys to these IDs; the remap table stores the resolved binding. (Cascade note carried in source:
`input.cycle_unit` is not yet listed in accessibility-requirements.md §6.3.)

---

## Enqueue mechanics (`TryEnqueueHumanOrder`)

Once a command resolves and the façade's gates pass, `DelegationBridge.TryEnqueueHumanOrder` does the
deterministic enqueue:

1. **Replay + control re-check** — bails to `false` if `AttachReplayViewer`, or if the entity has no
   binding, or its active controller isn't a `HumanController`.
2. **Risk classification** — `risk ?? DefaultRiskClassifier.Classify(kind)`. The classifier is trivial
   and load-bearing: `Engage → High`, everything else → `Low`. (Risk feeds the downstream engagement
   gate, not the delegation gate.)
3. **Comms delay** — `executeTick = CommsOrderDelay.ComputeExecuteSimTick(simTick, CurrentCommsState,
   commsDisplay)`. Under `Degraded` comms the order is held `DegradedOrderDelayTicks` extra ticks;
   `Nominal`/`Denied` add `0`. (Note: a `Denied` *new engagement* is blocked separately in the sim —
   see [comms-degradation-runtime.md](comms-degradation-runtime.md).)
4. **Enqueue + log** — `HumanController.Enqueue(order, executeTick)` parks the order in a
   `PlayerOrderExecutionQueue`, and `DecisionLog.AppendPlayerOrder(new PlayerOrderRecord(…, Kind,
   ExecuteSimTick: executeTick))` records it in the fingerprinted order log.

The order is **not** dispatched immediately. On a subsequent orchestrator tick, the `HumanController`
branch calls `DrainIssuedOrders(simTick)`, which returns every queued order whose `ExecuteSimTick <=
currentSimTick` (insertion order preserved), and those flow to the sink via `OrderDispatcher`. This is
why an order issued under degraded comms visibly lands a few ticks later.

`PlayerOrderRecord` is a first-class order-log row (`Source = "player"`,
`ResolvedExecuteSimTick = ExecuteSimTick == 0 ? SimTick : ExecuteSimTick`), so player orders replay
deterministically alongside agent decisions.

---

## Failure-reason catalog

All reasons are stable string constants; UI keys off them, tests assert them.

| Reason | Layer | When |
|--------|-------|------|
| `NO_SELECTION` | pure (`C2CommandIssuance` / `AgentDirectiveIssuance`) | `Validate` called with no unit selected. |
| `UNKNOWN_COMMAND` | pure (`C2CommandIssuance`) | Unknown/blank command id. |
| `UNKNOWN_DIRECTIVE` | pure (`AgentDirectiveIssuance`) | Unknown/blank directive id (note: `engage` is **not** a valid directive). |
| `NO_AGENT` | pure (`AgentDirectiveIssuance`) | Mode-change directive but selection has no related agent (`hasAgent` overload). |
| `NO_ACTIVE_AGENT` | pure (`AgentDirectiveIssuance`) | `take_control` with no active agent (state-aware overload). |
| `NO_SUSPENDED_AGENT` | pure (`AgentDirectiveIssuance`) | `return_to_agent` with no suspended agent. |
| `REPLAY_ATTACHED` | façade (`C2PlayerCommandBridge`) | `AttachReplayViewer` is set. |
| `UNKNOWN_UNIT` | façade | Null bridge, or entity not in the registry. |
| `NOT_HUMAN_CONTROL` | façade | Target's active controller isn't a `HumanController`. |
| `ENQUEUE_FAILED` | façade | `TryEnqueueHumanOrder` returned `false` (e.g. a race where control changed after the check). |

`C2PlayerCommandBridge.TryIssue` runs these gates in order: resolve command → `REPLAY_ATTACHED` →
registry (`UNKNOWN_UNIT`) → `NOT_HUMAN_CONTROL` → `ENQUEUE_FAILED`, passing the pure-layer
`UNKNOWN_COMMAND` straight through.

---

## Sibling: `DoctrineOverrideCommand` (headless ROE override, req 13 P0 / ADR-010)

Not every C2 command enqueues an order. `DoctrineOverrideCommand.TryApply(orchestrator, unitId,
roeLevelLabel, simTime)` is the doctrine surface that changes a unit's **ROE level** in place:

- Parse `roeLevelLabel` → `RoeLevel` (invalid label → `false`, no-op).
- Resolve the unit's current `EffectivePolicy`; **idempotent** if the ROE is already that level
  (returns `false`, nothing logged).
- Otherwise `PolicySnapshots.Capture` a new `EffectivePolicy(newRoe, currentMaxSalvo)` and append a
  `PolicyUpdateRecord("roe", oldRoe, newRoe)`.

So it mutates the per-unit policy pin (consumed by the engagement gate) and logs a policy update rather
than a player order. It lives in the same `Bridge/` folder because it's a headless command handler with
the same "structured result, no Tick hotpath" shape.

---

## Common pitfalls & constraints

- **Resolve, then enqueue.** Don't call the façade to *test* whether a command is valid — use the pure
  `Validate`/`TryResolve` for UI enable/disable and previews; the façade has side effects (it enqueues).
- **A unit must already be human-controlled.** Issuance never takes control for you. If a button is
  greyed out with `NOT_HUMAN_CONTROL`, the roster directive `take_control` (or the arbitration API) is
  the prerequisite step.
- **`engage` is a command, not a directive.** `AgentDirectiveIssuance` deliberately rejects `engage`
  with `UNKNOWN_DIRECTIVE`; fire orders come through `C2CommandIssuance` / the attack-menu path, so ROE
  applies at the sim layer.
- **Orders don't execute on the issuing tick.** They queue and drain when due. Under degraded comms the
  delay is real and intentional — don't "fix" a perceived lag by dispatching directly.
- **Reasons are an API.** UI tooltips, tests, and telemetry read the string constants. Renaming or
  free-texting a reason is a breaking change.
- **Stay off the Tick hotpath.** Issuance is a host seam. New surface belongs in `Input/` (pure) or the
  façade, never inside `DelegationBridge.Tick`.

---

## Extending without breaking replay

1. **Adding a new toolbar command?** Add the `OrderKind` at the **end** of the enum (never renumber),
   add the id case(s) to `C2CommandIssuance.TryResolve`, and add a `[TestCase]` row plus the ordinal
   assertion. Decide its `DefaultRiskClassifier` risk (only `Engage` is `High` today) and whether the
   sink knows how to dispatch it.
2. **Adding a new agent directive?** Add to `AgentDirectiveAction` + `TryResolve`, and be explicit about
   `IsModeChange` (drives whether the host does a control mode change vs. an enqueue) and which
   `Validate` overload / controller-state reason applies.
3. **Adding a new failure mode?** Add a `const string` reason next to the existing ones and cover it
   with a test; don't return an unnamed string.
4. **Touching the enqueue mechanics?** `TryEnqueueHumanOrder` writes a fingerprinted `PlayerOrderRecord`
   and honors the comms delay — any change there is replay-affecting. Re-baseline goldens only under an
   ADR.
5. **Before landing:** run the suites below plus the full solution suite (`dotnet test`), confirm
   `ReplayGolden 6/6` and the Baltic v2 hash `17144800277401907079` are unchanged, and confirm ZERO
   `DelegationBridge` Tick hotpath edits.

---

## Tests that pin this doc

All green as of writing (S108 / CMD-31 · CMD-37 · LOG-08/09 · S120 residual-scope confirm):

| Test file | Cases | Covers |
|-----------|-------|--------|
| [`C2CommandIssuanceTests.cs`](../../src/ProjectAegis.Delegation.Tests/Input/C2CommandIssuanceTests.cs) | ~34 | Every command-id → `OrderKind` map (incl. `plot_course`/`launch`/boat aliases), case/whitespace insensitivity, `UNKNOWN_COMMAND` for unknown/blank, `NO_SELECTION` gate, and the append-only `OrderKind` ordinal assertion (`Move=0 … AbortBoatLaunch=11`). |
| [`AgentDirectiveIssuanceTests.cs`](../../src/ProjectAegis.Delegation.Tests/Input/AgentDirectiveIssuanceTests.cs) | ~21 | Directive-id → request map, mode-change vs. order directives, `NO_SELECTION` / `NO_AGENT` / `NO_ACTIVE_AGENT` / `NO_SUSPENDED_AGENT` / `UNKNOWN_DIRECTIVE`, `hold`/`rtb` allowed without an agent, and reuse of existing `OrderKind`s. |
| [`C2PlayerCommandBridgeTests.cs`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/C2PlayerCommandBridgeTests.cs) | ~7 | End-to-end enqueue (`hold`/`set_emcon`/`set_sensors`/`plot_course` land in `DecisionLog.PlayerOrders`) and the `UNKNOWN_COMMAND` / `UNKNOWN_UNIT` / `NOT_HUMAN_CONTROL` / `REPLAY_ATTACHED` failure paths (nothing appended on failure). |
| [`DoctrineOverrideCommandTests.cs`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/DoctrineOverrideCommandTests.cs) | 4 | ROE override logs a `PolicyUpdateRecord` + moves the effective policy, idempotent no-op when unchanged, rejects an unknown ROE label, rejects a null orchestrator. |

Run just this subsystem:

```bash
dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj \
  --filter "FullyQualifiedName~Input.C2CommandIssuance|FullyQualifiedName~Input.AgentDirectiveIssuance"
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "FullyQualifiedName~C2PlayerCommandBridge|FullyQualifiedName~DoctrineOverrideCommand"
```
