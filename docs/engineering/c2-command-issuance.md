# C2 command issuance — player intent → human orders (CMD-31 / CMD-37)

The [`Input/`](../../src/ProjectAegis.Delegation/Input/) folder in `ProjectAegis.Delegation`
is the **write side** of the C2 (command-and-control) UI: the small, pure layer that turns a
toolbar click, hotkey, or agent-roster button into a validated *intent* — a resolved
[`OrderKind`](../../src/ProjectAegis.Delegation/Core/Order.cs) or a controller mode change —
**before** anything is enqueued on the sim. It is the mirror image of the
[c2-projection-layer](c2-projection-layer.md) read side: projections turn the order log into what
the UI *draws*; these helpers turn what the operator *does* into an order the bridge can enqueue.

The whole folder is **pure validation + id-string mapping**: no simulation, bridge, or RNG side
effects, no wall-clock reads. Hosts call the resolver, get back a structured result, and only then
enqueue via [`DelegationBridge`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs).
This keeps command issuance testable under `dotnet test` and keeps UI input off the deterministic
hot path.

> **Boundary (ADR-010 / ADR-007):** nothing in `Input/` mutates the sim, the catalog, or the
> `DecisionLog`. The only mutation path is the host handing a resolved `OrderKind` to
> `DelegationBridge.TryEnqueueHumanOrder`, which appends a `PlayerOrderRecord` and enqueues on the
> unit's `HumanController`. Command *resolution* is a pure function of its string input, so it is
> replay-neutral; the *enqueue* participates in the order log exactly like any other human order.

---

## The three pieces

| Type | Kind | Role |
|------|------|------|
| [`C2CommandIssuance`](../../src/ProjectAegis.Delegation/Input/C2CommandIssuance.cs) | `static` | Toolbar/hotkey command id → `OrderKind` (CMD-31). `TryResolve` + selection-aware `Validate`. |
| [`AgentDirectiveIssuance`](../../src/ProjectAegis.Delegation/Input/AgentDirectiveIssuance.cs) | `static` | Agent-roster directive id → structured `AgentDirectiveRequest` (mode change **or** order) (CMD-37). |
| [`C2InputActions`](../../src/ProjectAegis.Delegation/Input/C2InputActions.cs) | `static` | Remappable keyboard-action id constants (req 20 §Keyboard, a11y §6.3). |

---

## `C2CommandIssuance` — command id → `OrderKind`

`TryResolve(string? commandId, out OrderKind kind, out string? reason)` maps a normalized
(`Trim().ToLowerInvariant()`) command id to an `OrderKind`. It is **case- and
whitespace-insensitive**; a null/blank/unknown id fails with `ReasonUnknownCommand`
(`"UNKNOWN_COMMAND"`).

| Command id(s) | `OrderKind` | Tracked as |
|---------------|-------------|------------|
| `hold` | `Hold` | CMD-31 |
| `rtb` | `ReturnToBase` | CMD-31 |
| `move`, `plot_course` | `Move` (`plot_course` is a course-plotting alias) | CMD-31 |
| `engage` | `Engage` | CMD-31 |
| `set_emcon` | `SetEmcon` | CMD-31 |
| `set_sensors` | `SetSensors` | CMD-31 |
| `launch`, `launch_aircraft` | `LaunchAircraft` | LOG-08 / CMD-24 |
| `abort_launch`, `abort_launch_aircraft` | `AbortLaunchAircraft` | LOG-08 / CMD-24 |
| `launch_boat` | `LaunchBoat` | LOG-09…11 / CMD-25 |
| `recover_boat` | `RecoverBoat` | LOG-09…11 / CMD-25 |
| `abort_boat_launch` | `AbortBoatLaunch` | LOG-09…11 / CMD-25 |

`Validate(string? commandId, bool hasSelection)` is the full pre-issue check used by hosts. It
returns a `C2CommandResult(bool Ok, string? FailureReason, OrderKind? Kind)`:

1. no selection ⇒ `Ok = false`, `FailureReason = ReasonNoSelection` (`"NO_SELECTION"`);
2. else resolve the id — unknown ⇒ `Ok = false`, `FailureReason = ReasonUnknownCommand`;
3. else `Ok = true`, `Kind` set.

> **`OrderKind` is append-only.** New commands land as new enum members *after* the existing ones
> (`Move=0 … AbortBoatLaunch=11`) and never reorder the block — the ordinals are load-bearing for
> the order-log fingerprint (`OrderKind_append_preserves_existing_ordinals` pins them). To add a
> command: append the `OrderKind`, add a `case` to `TryResolve`, and add a `[TestCase]`.

---

## `AgentDirectiveIssuance` — roster directive → mode change *or* order

Agent-roster buttons (CMD-37) can either change *who controls* a unit or issue a quick order.
`TryResolve` maps a normalized directive id to an `AgentDirectiveRequest(string DirectiveId,
AgentDirectiveAction Action, bool IsModeChange, OrderKind? OrderKind)`:

| Directive id | `AgentDirectiveAction` | `IsModeChange` | `OrderKind` |
|--------------|------------------------|:--------------:|-------------|
| `take_control` | `TakeControl` | `true` | — |
| `return_to_agent` | `ReturnToAgent` | `true` | — |
| `hold` | `Hold` | `false` | `Hold` |
| `rtb` | `Rtb` | `false` | `ReturnToBase` |

Order directives (`hold`/`rtb`) deliberately **reuse the existing `OrderKind`s** rather than adding
new enums (`hold_and_rtb_reuse_existing_OrderKinds_without_new_enums` pins this).

Two `Validate` overloads return an `AgentDirectiveResult(bool Ok, string? FailureReason,
AgentDirectiveRequest? Request)`:

- **`Validate(directiveId, hasSelection, hasAgent)`** — selection required (`ReasonNoSelection`),
  then resolution (`ReasonUnknownDirective`), then: mode-change directives require *any* related
  agent (`ReasonNoAgent`); order directives (`hold`/`rtb`) only need a selection.
- **`Validate(directiveId, hasSelection, hasActiveAgent, hasSuspendedAgent)`** — the
  controller-state-aware overload: `TakeControl` needs an **active** agent (`ReasonNoActiveAgent`),
  `ReturnToAgent` needs a **suspended** agent (`ReasonNoSuspendedAgent`). This is the overload the
  Unity host uses so the roster buttons enable/disable against real controller state.

---

## `C2InputActions` — remappable keyboard-action ids

Stable string constants that are the **single source of truth** for the input actions the sim
resolves at session start (a11y §6.3 stub contract). UI hosts bind default keys to these ids; a
remap table stores the resolved binding.

| Constant | Id | Default key | Source |
|----------|----|-------------|--------|
| `CycleUnit` | `input.cycle_unit` | N / P (next / prev friendly unit) | UX spec §6 |
| `FocusPrimaryThreat` | `input.focus_primary_threat` | F (centre on primary hostile) | a11y §6.3 |
| `Cancel` | `input.cancel` | Esc (close modal / cancel preview / cancel weapons-release gate) | a11y §6.3 |

> **Cascade note (from source):** `input.cycle_unit` is defined here per the UX interaction map but
> is not yet listed in `accessibility-requirements.md` §6.3 — add it there so the constant and the
> a11y doc agree.

---

## The bridge seam — `C2PlayerCommandBridge`

Resolution alone does not enqueue anything. The
[`C2PlayerCommandBridge`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/C2PlayerCommandBridge.cs)
static facade (in the Unity adapter) is what turns a resolved command into a queued human order,
returning structured failure reasons for UI tooltips / status labels:

```
C2PlayerCommandBridge.TryIssue(bridge, entity, commandId, simTime, out failureReason)
  ├─ bridge is null                      → ReasonUnknownUnit  ("UNKNOWN_UNIT")
  ├─ C2CommandIssuance.TryResolve fails   → ReasonUnknownCommand ("UNKNOWN_COMMAND")
  ├─ bridge.AttachReplayViewer            → ReasonReplayAttached ("REPLAY_ATTACHED")
  ├─ Registry.TryGetBinding(entity) fails → ReasonUnknownUnit  ("UNKNOWN_UNIT")
  ├─ Active is not HumanController         → ReasonNotHumanControl ("NOT_HUMAN_CONTROL")
  └─ TryEnqueueHumanOrder fails            → ReasonEnqueueFailed ("ENQUEUE_FAILED")
```

Key contracts:

- **It does not mutate control slots.** The unit must *already* be under a `HumanController`; taking
  control is a separate step (see [direct-control-override-runtime](direct-control-override-runtime.md)).
- **Replay is blocked twice.** `TryIssue` short-circuits on `AttachReplayViewer`, and
  `DelegationBridge.TryEnqueueHumanOrder` independently returns `false` when a replay viewer is
  attached — a replay-mode UI can never inject orders.
- On success, `TryEnqueueHumanOrder` classifies risk (`DefaultRiskClassifier`), applies the comms
  order-execution delay (`CommsOrderDelay.ComputeExecuteSimTick`, see
  [comms-degradation-runtime](comms-degradation-runtime.md)), enqueues the `Order` on the unit's
  `HumanController`, and appends a `PlayerOrderRecord` to the `DecisionLog`.

`DelegationBridge.TryIssuePlayerCommand(entity, commandId, simTime, out reason)` is the same call
exposed as a thin bridge method (used by the boat-command host path).

---

## Unity host wiring

Both C2 hosts live in `unity/…/Runtime/` and are thin: they gather selection state, call a pure
`Validate`/`TryResolve`, then apply the result.

- [`DelegationBridgeHost`](../../unity/ProjectAegis/Assets/Scripts/Runtime/DelegationBridgeHost.cs)
  — `TryIssueSelectedCommand(commandId, out reason)` maps the current selection to an `EntityKey` and
  calls `C2PlayerCommandBridge.TryIssue`; convenience wrappers `TryIssueLaunchAircraft` /
  `TryIssueAbortLaunch` / `TryIssueBoatCommand` pass the fixed command ids. `TryIssueAgentDirective`
  runs the controller-state-aware `AgentDirectiveIssuance.Validate`, then: for `IsModeChange` requests
  it calls `TryTakeControlOfSelected` / `TryReturnToAgentOfSelected`; for order directives it takes
  direct control first if the unit is still agent-driven, then `TryEnqueueHumanOrder`.
- [`AgentRosterPanelHost`](../../unity/ProjectAegis/Assets/Scripts/Runtime/AgentRosterPanelHost.cs)
  — binds the four roster buttons (`directive-take-control`, `directive-return-to-agent`,
  `directive-hold`, `directive-rtb`) to the `AgentDirectiveIssuance.*Id` constants and surfaces the
  failure reason as panel status text.

---

## Determinism & tests

- **No RNG, no clock, no I/O.** Every helper is a pure function of its arguments, so it is safe to
  call every frame and it never touches the replay fingerprint. Only the bridge enqueue writes to
  the (fingerprinted) `DecisionLog`, on the same path as every other human order.
- **Fail-closed reasons.** Every rejection returns a stable machine-readable reason string, so UI
  tooltips and tests assert on constants rather than free text.

| Suite | Location |
|-------|----------|
| `C2CommandIssuanceTests` | [`src/ProjectAegis.Delegation.Tests/Input/C2CommandIssuanceTests.cs`](../../src/ProjectAegis.Delegation.Tests/Input/C2CommandIssuanceTests.cs) |
| `AgentDirectiveIssuanceTests` | [`src/ProjectAegis.Delegation.Tests/Input/AgentDirectiveIssuanceTests.cs`](../../src/ProjectAegis.Delegation.Tests/Input/AgentDirectiveIssuanceTests.cs) |
| `C2PlayerCommandBridgeTests` | [`src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/C2PlayerCommandBridgeTests.cs`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/C2PlayerCommandBridgeTests.cs) |

Related: [c2-projection-layer.md](c2-projection-layer.md) (the read side) ·
[direct-control-override-runtime.md](direct-control-override-runtime.md) (controller arbitration behind mode-change directives) ·
[autonomy-roe-gating.md](autonomy-roe-gating.md) (how the enqueued order is then ROE/autonomy-gated) ·
[comms-degradation-runtime.md](comms-degradation-runtime.md) (the order-execution delay applied on enqueue).
