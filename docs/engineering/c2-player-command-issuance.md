# C2 player command issuance — command IDs → human orders (CMD-31 / CMD-37)

The `Input/` folder in [`ProjectAegis.Delegation`](../../src/ProjectAegis.Delegation/Input/)
is the **write side** of the human-in-the-loop C2 loop: the pure, engine-agnostic seam that
turns a UI command string (a toolbar click, hotkey, or agent-roster button) into a validated
[`OrderKind`](../../src/ProjectAegis.Delegation/Core/Order.cs) or a controller mode change. It is
the counterpart to the [c2-projection-layer.md](c2-projection-layer.md) *read* side: projections
answer *"what does the UI draw?"*, this seam answers *"what happens when the player clicks?"*.

This guide explains the deliberate **two-layer** design (pure validation vs. bridge enqueue), the
command / directive ID vocabularies, the structured failure-reason catalog UIs surface as
tooltips, the replay-safety contract, and how to add a new command without breaking goldens.

> **Scope.** CMD-31 (player toolbar/hotkey commands), CMD-37 (agent-roster directives), and the
> req 20 §Keyboard / a11y §6.3 remappable input-action stub IDs. The validation helpers are
> **pure and side-effect-free**; all sim mutation goes through the bridge, which is the only layer
> that touches the orchestrator. Presentation boundary per **ADR-010** (headless-first,
> command-driven UI) and **ADR-003** (order-log schema). UI is a *client*, never sim authority.

Related: [c2-projection-layer.md](c2-projection-layer.md) (read side) ·
[autonomy-roe-gating.md](autonomy-roe-gating.md) (what happens to the order after enqueue) ·
[direct-control-override-runtime.md](direct-control-override-runtime.md) (take/return control
mechanics behind `take_control` / `return_to_agent`) ·
[determinism-and-replay.md](determinism-and-replay.md) ·
[Delegation README](../../src/ProjectAegis.Delegation/README.md) ·
[UnityAdapter README](../../src/ProjectAegis.Delegation.UnityAdapter/README.md) ·
[ADR-010 headless-first command-driven UI](../architecture/adr-010-headless-first-command-driven-ui.md).

---

## The two-layer model

Command issuance is split so the pure vocabulary/validation is testable without a bridge, and the
only stateful mutation lives in one thin façade over the orchestrator:

| Layer | Type | Assembly | Role |
|-------|------|----------|------|
| **1. Pure validation** | [`C2CommandIssuance`](../../src/ProjectAegis.Delegation/Input/C2CommandIssuance.cs), [`AgentDirectiveIssuance`](../../src/ProjectAegis.Delegation/Input/AgentDirectiveIssuance.cs) | `ProjectAegis.Delegation` (`Input/`) | `static` pure helpers. Resolve a command/directive ID to an `OrderKind` (or a mode-change request) and pre-validate selection/agent state. **No sim, bridge, or I/O.** |
| **2. Bridge enqueue** | [`C2PlayerCommandBridge`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/C2PlayerCommandBridge.cs) → [`DelegationBridge`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs) | `ProjectAegis.Delegation.UnityAdapter` (`Bridge/`) | Resolves the ID, then applies runtime guards (replay-attached, unit exists, unit is human-controlled) and enqueues a `PlayerOrderRecord` on the orchestrator. |

A UI host typically uses **both**: layer 1 at hover/paint time to grey out invalid buttons and
build tooltips, and layer 2 at click time to actually enqueue. Layer 1 never enqueues; layer 2
never mutates control slots (the unit must already be under a `HumanController`).

```
UI click "hold" ── layer 1 (C2CommandIssuance.Validate) ─▶ enable/tooltip only
                └─ layer 2 (DelegationBridge.TryIssuePlayerCommand)
                       └─ C2PlayerCommandBridge.TryIssue
                              └─ DelegationBridge.TryEnqueueHumanOrder ─▶ PlayerOrderRecord
```

---

## `C2CommandIssuance` — toolbar / hotkey commands (CMD-31)

`TryResolve(commandId, out OrderKind, out reason)` maps a **case- and whitespace-insensitive** ID
(`commandId.Trim().ToLowerInvariant()`) to an `OrderKind`. `Validate(commandId, hasSelection)`
wraps it with a selection check and returns a `C2CommandResult(bool Ok, string? FailureReason,
OrderKind? Kind)`.

| Command ID(s) | `OrderKind` | Notes |
|---------------|-------------|-------|
| `hold` | `Hold` | |
| `rtb` | `ReturnToBase` | |
| `move`, `plot_course` | `Move` | `plot_course` is a semantic alias for course plotting |
| `engage` | `Engage` | the only fire-gated kind downstream (see autonomy/ROE gating) |
| `set_emcon` | `SetEmcon` | |
| `set_sensors` | `SetSensors` | |
| `launch`, `launch_aircraft` | `LaunchAircraft` | LOG-08 / CMD-24 — individual airframe launch |
| `abort_launch`, `abort_launch_aircraft` | `AbortLaunchAircraft` | |
| `launch_boat` | `LaunchBoat` | LOG-09…11 / CMD-25 — embarked craft |
| `recover_boat` | `RecoverBoat` | |
| `abort_boat_launch` | `AbortBoatLaunch` | |

**Failure reasons** (string constants on `C2CommandIssuance`):

| Constant | Value | When |
|----------|-------|------|
| `ReasonUnknownCommand` | `UNKNOWN_COMMAND` | null / empty / whitespace / unrecognised ID |
| `ReasonNoSelection` | `NO_SELECTION` | `Validate` called with `hasSelection: false` |

`Validate` checks selection **first**, then resolution — so an unknown command with no selection
reports `NO_SELECTION`.

```csharp
var result = C2CommandIssuance.Validate("set_sensors", hasSelection: true);
// result.Ok == true, result.Kind == OrderKind.SetSensors, result.FailureReason == null
```

> **`OrderKind` is append-only.** The map above deliberately mirrors the enum's ordinal history —
> new kinds are appended (never reordered), because `OrderKind` ordinals feed the order-log
> fingerprint. `OrderKind_append_preserves_existing_ordinals` pins every ordinal.

---

## `AgentDirectiveIssuance` — agent-roster directives (CMD-37)

Directives come from the agent roster panel and split into **mode changes** (take/return control)
and **orders** (which reuse the existing human enqueue path — no new `OrderKind`). `TryResolve`
returns an `AgentDirectiveRequest(DirectiveId, AgentDirectiveAction Action, bool IsModeChange,
OrderKind? OrderKind)`.

| Directive ID | `AgentDirectiveAction` | `IsModeChange` | `OrderKind` |
|--------------|------------------------|:--------------:|-------------|
| `take_control` | `TakeControl` | ✅ | — |
| `return_to_agent` | `ReturnToAgent` | ✅ | — |
| `hold` | `Hold` | ❌ | `Hold` |
| `rtb` | `Rtb` | ❌ | `ReturnToBase` |

Two `Validate` overloads gate mode changes by controller state (order directives only need a
selection):

- `Validate(directiveId, hasSelection, hasAgent)` — coarse: any mode change needs *some* related
  agent (`ReasonNoAgent`).
- `Validate(directiveId, hasSelection, hasActiveAgent, hasSuspendedAgent)` — precise:
  `take_control` needs an **active** agent (`ReasonNoActiveAgent`); `return_to_agent` needs a
  **suspended** agent (`ReasonNoSuspendedAgent`). This mirrors the suspend/resume model in
  [direct-control-override-runtime.md](direct-control-override-runtime.md).

**Failure reasons** (constants on `AgentDirectiveIssuance`): `ReasonNoSelection` (`NO_SELECTION`),
`ReasonNoAgent` (`NO_AGENT`), `ReasonNoActiveAgent` (`NO_ACTIVE_AGENT`), `ReasonNoSuspendedAgent`
(`NO_SUSPENDED_AGENT`), `ReasonUnknownDirective` (`UNKNOWN_DIRECTIVE`). Note `engage` is **not** a
valid directive here — it is rejected as `UNKNOWN_DIRECTIVE`.

```csharp
var take = AgentDirectiveIssuance.Validate(
    "take_control", hasSelection: true, hasActiveAgent: true, hasSuspendedAgent: false);
// take.Ok == true, take.Request.IsModeChange == true, take.Request.OrderKind == null
```

The host reads the returned request: `IsModeChange` directives drive
`DelegationBridge.TryTakeDirectControl` / `TryReleaseDirectControl`; order directives feed the same
enqueue path as `C2CommandIssuance`.

---

## `C2InputActions` — remappable input-action IDs (req 20 §Keyboard / a11y §6.3)

[`C2InputActions`](../../src/ProjectAegis.Delegation/Input/C2InputActions.cs) is the single source
of truth for the string IDs the remap table stores; UI hosts bind default keys to these IDs.

| Constant | ID | Default key | Source |
|----------|----|-------------|--------|
| `CycleUnit` | `input.cycle_unit` | N / P (next / previous friendly) | UX spec §6 |
| `FocusPrimaryThreat` | `input.focus_primary_threat` | F | a11y §6.3 |
| `Cancel` | `input.cancel` | Esc | a11y §6.3 |

> **Known doc-cascade note (from source).** `CycleUnit` is defined here per the UX spec interaction
> map but is **not yet listed** in `accessibility-requirements.md` §6.3. The source `<remarks>`
> flags that `input.cycle_unit` should be added to a11y §6.3 so the constant and the requirement
> agree. Kept as-is here to match source rather than silently "fix" the requirement doc.

---

## Bridge enqueue — `C2PlayerCommandBridge.TryIssue` (CMD-31)

`C2PlayerCommandBridge.TryIssue(bridge, entity, commandId, simTime, out failureReason)` is the
actual enqueue seam. It is a `static` façade over
[`DelegationBridge.TryEnqueueHumanOrder`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs)
and is exposed on the bridge itself as `DelegationBridge.TryIssuePlayerCommand(...)` (a thin
wrapper that does **not** touch the `Tick` hot path).

Guards run in this **exact order** — the first failure wins:

1. **null bridge** → `UNKNOWN_UNIT`.
2. **`C2CommandIssuance.TryResolve`** → `UNKNOWN_COMMAND` on an unrecognised ID. (Resolution runs
   *before* the runtime guards, so a typo always reports `UNKNOWN_COMMAND` regardless of replay or
   selection state.)
3. **`bridge.AttachReplayViewer`** → `REPLAY_ATTACHED`. A viewer/replay session is read-only; no
   player order can be enqueued.
4. **`Registry.TryGetBinding(entity)`** → `UNKNOWN_UNIT` if the entity is not registered.
5. **`binding.Target.Slot.Active is HumanController`** → `NOT_HUMAN_CONTROL` otherwise. The bridge
   never *grants* control — the unit must already be human-held (use `TryTakeDirectControl` first).
6. **`bridge.TryEnqueueHumanOrder`** → `ENQUEUE_FAILED` if the underlying enqueue rejects.

| Constant (`C2PlayerCommandBridge`) | Value |
|------------------------------------|-------|
| `ReasonReplayAttached` | `REPLAY_ATTACHED` |
| `ReasonNotHumanControl` | `NOT_HUMAN_CONTROL` |
| `ReasonUnknownUnit` | `UNKNOWN_UNIT` |
| `ReasonEnqueueFailed` | `ENQUEUE_FAILED` |

On success, `TryEnqueueHumanOrder` classifies risk (`DefaultRiskClassifier.Classify(kind)` unless
overridden), applies the comms-order delay (`CommsOrderDelay.ComputeExecuteSimTick`, see
[comms-degradation-runtime.md](comms-degradation-runtime.md)), enqueues the `Order` on the
`HumanController`, and appends a `PlayerOrderRecord` to the `DecisionLog`. The order is then gated
like any other at decision time — see [autonomy-roe-gating.md](autonomy-roe-gating.md).

```csharp
var bridge = new DelegationBridge(seed: 42, mvpEngagement: false);
var unit = bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
unit.Target.Slot.SetActive(new HumanController());

bridge.TryIssuePlayerCommand(new EntityKey(1), "hold", simTime: 3, out var reason);
// returns true; reason == null; DecisionLog.PlayerOrders has one Hold record
```

### Sibling player-command paths on the bridge

The same `Bridge/` folder exposes related click→order paths that share `TryEnqueueHumanOrder` and
the same replay/human-control guards:

| Bridge method | Purpose |
|---------------|---------|
| `TryEnqueueAttackOption(entity, optionId, snapshot, out reason)` | Interactive attack-menu selection (req 14 / doc 20). Resolves an engage option via `EngageAttackOrderResolver`, applies a per-shot salvo override, then enqueues. |
| `TryTakeDirectControl(entity, simTime)` / `TryReleaseDirectControl(entity, simTime)` | The mode changes behind `take_control` / `return_to_agent`. Detailed in [direct-control-override-runtime.md](direct-control-override-runtime.md). |
| `GetEngagePreviewForUnit(...)` / `GetAttackMenuOptions(...)` | Read-model previews that feed the attack menu (projection side). |

---

## Determinism & replay safety

- **Pure layer.** `C2CommandIssuance` / `AgentDirectiveIssuance` / `C2InputActions` hold no state,
  read no clock, and draw no RNG — they are trivially deterministic.
- **Replay is read-only.** Every enqueue path checks `AttachReplayViewer` and refuses when a
  replay/viewer is attached (`REPLAY_ATTACHED`), so scrubbing a golden can never inject an order.
- **Orders are logged, not the clicks.** Only the resulting `PlayerOrderRecord` enters the
  fingerprinted `DecisionLog`; the string command ID, tooltips, and remap bindings never do.
- **No hot-path change.** `TryIssuePlayerCommand` is a thin wrapper outside `DelegationBridge.Tick`.
  `DelegationBridge.cs` remains zero-touch on the hot path; the Baltic v2 replay hash
  (`17144800277401907079`) is untouched by this seam.

---

## Runbook — add a new player command

1. **Add the `OrderKind`** (if needed) by **appending** to
   [`OrderKind`](../../src/ProjectAegis.Delegation/Core/Order.cs) — never reorder existing members
   (ordinals are fingerprint-load-bearing). Update `OrderKind_append_preserves_existing_ordinals`.
2. **Map the ID** in `C2CommandIssuance.TryResolve` (lower-case, trimmed). Add aliases in the same
   `case` block if the UI uses more than one string.
3. **Cover it** in [`C2CommandIssuanceTests`](../../src/ProjectAegis.Delegation.Tests/Input/C2CommandIssuanceTests.cs)
   (map + case-insensitivity) and in
   [`C2PlayerCommandBridgeTests`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/C2PlayerCommandBridgeTests.cs)
   (end-to-end enqueue producing the expected `PlayerOrders[i].Kind`).
4. **Ensure the downstream path exists** — a new `OrderKind` must be handled by the
   autonomy/ROE gate and executor, or it will queue/execute unexpectedly. Confirm risk
   classification (`DefaultRiskClassifier`) is correct for the new kind.
5. **Do not add sim side effects to the pure layer.** Validation stays pure; enqueue stays in the
   bridge.

Agent directives follow the same shape in `AgentDirectiveIssuance` + `AgentDirectiveIssuanceTests`
— decide up front whether the new directive is a **mode change** (needs agent-state validation) or
an **order** (reuses an existing `OrderKind`).

---

## Tests

| Test file | Covers |
|-----------|--------|
| [`C2CommandIssuanceTests.cs`](../../src/ProjectAegis.Delegation.Tests/Input/C2CommandIssuanceTests.cs) | Full command-ID → `OrderKind` map, case/whitespace insensitivity, `UNKNOWN_COMMAND` / `NO_SELECTION`, `Validate` results, and the `OrderKind` append-only ordinal pin. |
| [`AgentDirectiveIssuanceTests.cs`](../../src/ProjectAegis.Delegation.Tests/Input/AgentDirectiveIssuanceTests.cs) | Directive map, mode-change vs. order split, both `Validate` overloads (`NO_AGENT` / `NO_ACTIVE_AGENT` / `NO_SUSPENDED_AGENT`), and that `hold` / `rtb` reuse existing `OrderKind`s. |
| [`C2PlayerCommandBridgeTests.cs`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/C2PlayerCommandBridgeTests.cs) | End-to-end enqueue for human units, the guard-order failure catalog (`UNKNOWN_COMMAND` / `UNKNOWN_UNIT` / `NOT_HUMAN_CONTROL` / `REPLAY_ATTACHED`), and `plot_course → Move`. |
