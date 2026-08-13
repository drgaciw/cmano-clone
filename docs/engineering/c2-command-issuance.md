# C2 player command issuance — developer guide

When a human commander clicks a unit-order toolbar button, presses a hotkey, or picks a roster
directive, that intent becomes a **queued human order** on the delegation pipeline — without the UI
ever mutating the simulation directly. This is the C2 command-issuance path (`CMD-31…37`, landed in
S108; confirmed as the product path by the S120 residual-scope audit). It is a textbook example of
the presentation boundary: **the UI is a client, not sim authority** (ADR-010 §2–3, ADR-007,
ADR-001).

The path is deliberately split into a **pure, engine-agnostic validation core** and a **thin
UnityAdapter facade** over the existing human-order enqueue:

```text
UI toolbar / hotkey / roster host  (Unity C2 chrome)
  → C2CommandIssuance.Validate / TryResolve        (pure; ProjectAegis.Delegation/Input)
    · AgentDirectiveIssuance.Validate / TryResolve  (roster directives — CMD-37)
  → C2PlayerCommandBridge.TryIssue                 (structured reasons + HumanController gate)
  → DelegationBridge.TryEnqueueHumanOrder          (existing enqueue — NOT the decision Tick)
  → HumanController.Enqueue(order, executeTick)
  → DecisionLog.AppendPlayerOrder(PlayerOrderRecord)
```

- **Source:** [`src/ProjectAegis.Delegation/Input/`](../../src/ProjectAegis.Delegation/Input/) (the
  pure helpers) and
  [`C2PlayerCommandBridge`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/C2PlayerCommandBridge.cs)
  / [`DelegationBridge`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs)
  (the adapter facade + enqueue).
- **Related:** the enqueued order is drained and gated by the
  [autonomy/ROE gate](autonomy-roe-gating.md); taking/releasing control (which issuance requires but
  never performs) is the [direct-control override runtime](direct-control-override-runtime.md); the
  execute-tick delay under contested comms is the
  [comms degradation runtime](comms-degradation-runtime.md). This page documents what the issuance
  path **does** and how to add a command without breaking the boundary.

> **Boundary discipline.** The `Input/` helpers are **pure** (`static`, no sim/bridge side effects):
> a host validates first, then enqueues via the bridge. Issuance appends to the human order queue —
> it does **not** run inside `DelegationOrchestrator.Tick` and does **not** touch the
> `DelegationBridge` hot path (a hard invariant). It also never takes control: the unit must
> **already** be under a `HumanController`.

---

## Where it lives

| File | Role |
|------|------|
| [`Input/C2CommandIssuance.cs`](../../src/ProjectAegis.Delegation/Input/C2CommandIssuance.cs) | Pure command-id → `OrderKind` map (`TryResolve`) + `Validate(commandId, hasSelection)`; `C2CommandResult(Ok, FailureReason, Kind)`. Reasons `UNKNOWN_COMMAND`, `NO_SELECTION`. |
| [`Input/AgentDirectiveIssuance.cs`](../../src/ProjectAegis.Delegation/Input/AgentDirectiveIssuance.cs) | Pure roster-directive validation (`CMD-37`): `AgentDirectiveAction`, `AgentDirectiveRequest`, `AgentDirectiveResult`, two `Validate` overloads. Reasons `NO_SELECTION`, `NO_AGENT`, `NO_ACTIVE_AGENT`, `NO_SUSPENDED_AGENT`, `UNKNOWN_DIRECTIVE`. |
| [`Input/C2InputActions.cs`](../../src/ProjectAegis.Delegation/Input/C2InputActions.cs) | Remappable input-action stub IDs (req 20 keyboard / a11y §6.3): `input.cycle_unit`, `input.focus_primary_threat`, `input.cancel`. Single source of truth for action IDs the sim resolves at session start. |
| [`Bridge/C2PlayerCommandBridge.cs`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/C2PlayerCommandBridge.cs) | Static facade `TryIssue(bridge, entity, commandId, simTime, out failureReason)` — resolve + gates + enqueue. Reasons `REPLAY_ATTACHED`, `NOT_HUMAN_CONTROL`, `UNKNOWN_UNIT`, `ENQUEUE_FAILED`. |
| [`Bridge/DelegationBridge.cs`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs) | `TryEnqueueHumanOrder(entity, kind, simTime, risk?)` (the enqueue) and the thin `TryIssuePlayerCommand(entity, commandId, simTime, out reason)` wrapper. |
| [`Decision/PlayerOrderRecord.cs`](../../src/ProjectAegis.Delegation/Decision/PlayerOrderRecord.cs) | The `DecisionLog` player-order row `(SequenceId, SimTime, SimTick, UnitId, Kind, Source, ExecuteSimTick)`. |
| [`Roe/DefaultRiskClassifier.cs`](../../src/ProjectAegis.Delegation/Roe/DefaultRiskClassifier.cs) | `Classify(kind)` → `Engage` = `High`, everything else `Low`. |

---

## 1. Command resolution (`C2CommandIssuance`, CMD-31)

`TryResolve(commandId, out OrderKind, out reason)` maps a toolbar/hotkey id to an
[`OrderKind`](../../src/ProjectAegis.Delegation/Core/Order.cs). Ids are trimmed and lower-cased, so
matching is **case- and whitespace-insensitive**; an empty or unknown id fails with
`UNKNOWN_COMMAND`.

| Command id(s) | `OrderKind` | Notes |
|---------------|-------------|-------|
| `hold` | `Hold` | |
| `rtb` | `ReturnToBase` | |
| `move`, `plot_course` | `Move` | `plot_course` is a semantic alias for course plotting |
| `engage` | `Engage` | the only order classified `High` risk |
| `set_emcon` | `SetEmcon` | CMD-31 append-only |
| `set_sensors` | `SetSensors` | CMD-31 append-only |
| `launch`, `launch_aircraft` | `LaunchAircraft` | LOG-08 / CMD-24 |
| `abort_launch`, `abort_launch_aircraft` | `AbortLaunchAircraft` | |
| `launch_boat` | `LaunchBoat` | LOG-09…11 / CMD-25 |
| `recover_boat` | `RecoverBoat` | |
| `abort_boat_launch` | `AbortBoatLaunch` | |

`Validate(commandId, hasSelection)` is the full pre-issue check: it returns `NO_SELECTION` when no
unit is selected, otherwise resolves the command and returns a `C2CommandResult` carrying the
`OrderKind`.

> **`OrderKind` is append-only.** New commands add enum members at the end (`CMD-31` / `CMD-24` /
> `CMD-25` comments in `Order.cs` mark the do-not-reorder boundary) so existing ordinals stay stable
> for the order-log replay stream — pinned by `OrderKind_append_preserves_existing_ordinals`.

## 2. Roster directives (`AgentDirectiveIssuance`, CMD-37)

Roster/agent directives split into **mode changes** and **orders**:

| Directive id | Action | Kind |
|--------------|--------|------|
| `take_control` | `TakeControl` | mode change (no `OrderKind`) |
| `return_to_agent` | `ReturnToAgent` | mode change (no `OrderKind`) |
| `hold` | `Hold` | order → `OrderKind.Hold` |
| `rtb` | `Rtb` | order → `OrderKind.ReturnToBase` |

`TryResolve` produces a structured `AgentDirectiveRequest(DirectiveId, Action, IsModeChange, OrderKind?)`.
Two `Validate` overloads enforce presence rules — the host executes the mode change **or** enqueues
the order accordingly:

- `Validate(directiveId, hasSelection, hasAgent)` — mode-change directives require a related agent
  (`NO_AGENT`); the `hold` / `rtb` order directives need only a selection.
- `Validate(directiveId, hasSelection, hasActiveAgent, hasSuspendedAgent)` — controller-state aware:
  `take_control` requires an **active** agent (`NO_ACTIVE_AGENT`), `return_to_agent` requires a
  **suspended** agent (`NO_SUSPENDED_AGENT`).

The order directives deliberately **reuse existing `OrderKind`s** (no new enum members), so they
route through the same enqueue path as toolbar commands
(`hold_and_rtb_reuse_existing_OrderKinds_without_new_enums`).

## 3. The adapter facade (`C2PlayerCommandBridge.TryIssue`, CMD-31)

`TryIssue` is the single entry point a UI host calls to issue a command. It runs an **ordered gate
chain**, returning the first failure reason for a tooltip / status label:

| Order | Gate | Failure reason |
|-------|------|----------------|
| 1 | `bridge` is null | `UNKNOWN_UNIT` |
| 2 | command id resolves (`C2CommandIssuance.TryResolve`) | `UNKNOWN_COMMAND` |
| 3 | a replay viewer is **not** attached (`bridge.AttachReplayViewer`) | `REPLAY_ATTACHED` |
| 4 | the entity has a registry binding | `UNKNOWN_UNIT` |
| 5 | the unit's active controller **is** a `HumanController` | `NOT_HUMAN_CONTROL` |
| 6 | `TryEnqueueHumanOrder` succeeds | `ENQUEUE_FAILED` |

Gate 5 is the load-bearing rule: **issuance never mutates control slots.** The unit must already be
under direct human control (see [direct-control-override-runtime.md](direct-control-override-runtime.md));
issuing a command does not take control for you. `DelegationBridge.TryIssuePlayerCommand` is a thin
wrapper that forwards to this facade.

## 4. Enqueue (`DelegationBridge.TryEnqueueHumanOrder`)

Once validated, the order is enqueued on the unit's `HumanController` (it is **not** executed inline
and never enters the decision `Tick`):

1. Reject when a replay viewer is attached, or when the entity has no binding / is not under a
   `HumanController`.
2. Resolve risk via `DefaultRiskClassifier.Classify(kind)` unless the caller passed one (`Engage` →
   `High`, else `Low`).
3. Compute the execute tick with `CommsOrderDelay.ComputeExecuteSimTick(simTick, CurrentCommsState,
   commsDisplay)` — under **degraded** comms the player order's execution is delayed (see
   [comms-degradation-runtime.md](comms-degradation-runtime.md)).
4. `HumanController.Enqueue(order, executeTick)` and append a `PlayerOrderRecord` (`Source =
   "player"`, with `ExecuteSimTick`) to `DecisionLog` via `AppendPlayerOrder`.

The queued order is later drained by the orchestrator and passes the normal ROE-first / autonomy gate
before it can execute — issuance authorizes nothing on its own (see
[autonomy-roe-gating.md](autonomy-roe-gating.md)).

## 5. Input-action IDs (`C2InputActions`)

`C2InputActions` holds the remappable keyboard action **stub IDs** (req 20 keyboard parity / a11y
§6.3) that UI hosts bind default keys to and the sim resolves at session start:
`input.cycle_unit` (cycle friendly units, default `N`/`P`), `input.focus_primary_threat`
(centre on the primary hostile, default `F`), and `input.cancel` (close modal / cancel intent
preview or weapons-release gate, default `Esc`). These are identifiers only — the binding table and
key handling live in the UI layer.

> **Cascade note (from source):** `input.cycle_unit` is defined here per the UX interaction map but
> is not yet listed in `accessibility-requirements.md` §6.3; the constant's doc comment flags adding
> it so the code and the a11y doc agree.

---

## Boundary & determinism invariants

- **Pure validation, thin adapter.** `C2CommandIssuance` / `AgentDirectiveIssuance` are `static` and
  side-effect-free; the Unity adapter only adds the replay/binding/controller gates and calls the
  existing enqueue. UI is a client (ADR-010 §2–3, ADR-007, ADR-001) — never sim authority, and never
  ADR-018 (that is sensor side-picture / datalink).
- **No hot-path touch.** Issuance enqueues onto the human order queue; it is not part of the decision
  `Tick`, preserving the `DelegationBridge` zero-touch invariant.
- **No implicit control transfer.** A command requires an already-`HumanController`-owned unit
  (`NOT_HUMAN_CONTROL` otherwise); control changes are a separate runtime.
- **Replay-attached lockout.** With a replay viewer attached, both the facade (`REPLAY_ATTACHED`) and
  the enqueue refuse to issue, so scrubbing a recorded run cannot inject live orders.
- **Append-only order kinds.** New commands extend `OrderKind` at the end to keep order-log ordinals
  stable.

## Extending the path

1. **New toolbar command.** Append an `OrderKind` member (at the end), add the id case(s) to
   `C2CommandIssuance.TryResolve`, and confirm `DefaultRiskClassifier` gives the intended risk (add a
   case only if it is not `Low`). No bridge change is needed — `TryIssue` picks it up.
2. **New roster directive.** Add an `AgentDirectiveAction` + id constant and a `TryResolve` case; set
   `IsModeChange` correctly so hosts route it to a mode change vs. an order enqueue, and pick the
   right `Validate` overload for its agent-state precondition.
3. **New input action.** Add a stub id to `C2InputActions` and bind it in the UI; update
   `accessibility-requirements.md` §6.3 if it is an a11y-relevant action.

Verification for issuance changes (from repo root):

```bash
dotnet build ProjectAegis.sln
dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj \
  --filter "FullyQualifiedName~Input"
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "FullyQualifiedName~C2PlayerCommandBridge"
```

### Pinned by

| Area | Tests |
|------|-------|
| Command resolution / validation | [`C2CommandIssuanceTests`](../../src/ProjectAegis.Delegation.Tests/Input/C2CommandIssuanceTests.cs) |
| Roster directives | [`AgentDirectiveIssuanceTests`](../../src/ProjectAegis.Delegation.Tests/Input/AgentDirectiveIssuanceTests.cs) |
| Adapter facade gates | [`C2PlayerCommandBridgeTests`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/C2PlayerCommandBridgeTests.cs) |
