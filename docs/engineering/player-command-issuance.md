# Player command issuance — developer guide

Everything the player does through the C2 UI — clicking a toolbar button, pressing a hotkey, picking
an item off the agent-roster menu — has to become either a **queued order** or a **controller
mode change**, and it has to be *validated the same way* whether the click came from a mouse, a
remapped key, or a headless test. That validation lives in
[`ProjectAegis.Delegation/Input/`](../../src/ProjectAegis.Delegation/Input/): three small **pure**
types that turn a command/directive id string into a structured, checked result. The Unity host then
executes that result through the bridge.

This is the "player command intent → order" seam (CMD-31 toolbar commands, CMD-37 agent directives,
req-20 keyboard/a11y). It is deliberately engine-agnostic and side-effect-free so the same rules run
under `dotnet test` and under Unity.

- **Pure source:** [`Input/C2InputActions.cs`](../../src/ProjectAegis.Delegation/Input/C2InputActions.cs)
  (remappable action ids), [`Input/C2CommandIssuance.cs`](../../src/ProjectAegis.Delegation/Input/C2CommandIssuance.cs)
  (command → `OrderKind`), [`Input/AgentDirectiveIssuance.cs`](../../src/ProjectAegis.Delegation/Input/AgentDirectiveIssuance.cs)
  (directive → mode change / order).
- **Host bridge:** [`C2PlayerCommandBridge`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/C2PlayerCommandBridge.cs)
  in the Unity adapter — the only side-effecting step, over
  [`DelegationBridge.TryEnqueueHumanOrder`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/).
- **Related:** the mode-change side of directives (take/return control) runs through the
  [direct-control override runtime](direct-control-override-runtime.md); the order kinds resolved
  here are gated downstream by the [decision/ROE pipeline](autonomy-roe-gating.md); the remappable
  action ids also appear in the [C2 projection layer](c2-projection-layer.md) alert/lifecycle notes.

> **Pure validation, host execution.** None of the `Input/` types touch the sim, the order log, the
> bridge, or any UI state. They only *decide* — `(ok, reason, kind/request)`. The host is responsible
> for the effect (enqueue an order or change a controller slot). That split keeps the rules unit-
> testable with plain values and keeps determinism/replay concerns entirely on the host side of the
> boundary (ADR-010 headless-first: the UI is a client, not sim authority).

---

## Where it lives

| File | Role |
|------|------|
| [`C2InputActions`](../../src/ProjectAegis.Delegation/Input/C2InputActions.cs) | The remappable **action-id constants** (`input.cycle_unit`, `input.focus_primary_threat`, `input.cancel`) — the single source of truth the sim resolves at session start; UI hosts bind default keys to these ids. |
| [`C2CommandIssuance`](../../src/ProjectAegis.Delegation/Input/C2CommandIssuance.cs) | Command-id → [`OrderKind`](../../src/ProjectAegis.Delegation/Core/Order.cs) resolution + selection validation. Returns `C2CommandResult(Ok, FailureReason, Kind)`. |
| [`AgentDirectiveIssuance`](../../src/ProjectAegis.Delegation/Input/AgentDirectiveIssuance.cs) | Directive-id → `AgentDirectiveRequest` (a **mode change** or an **order**) + selection/agent-state validation. Returns `AgentDirectiveResult`. |
| [`C2PlayerCommandBridge`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/C2PlayerCommandBridge.cs) | Host facade: resolve a command, run the ingress guards, and enqueue the human order via the bridge. |

---

## Toolbar / hotkey commands (`C2CommandIssuance`, CMD-31)

`C2CommandIssuance` maps a lowercase command id to an `OrderKind`. `TryResolve(commandId, out kind,
out reason)` is the raw map; `Validate(commandId, hasSelection)` wraps it with the **selection-first**
rule and returns the structured `C2CommandResult`:

```csharp
var result = C2CommandIssuance.Validate(commandId, hasSelection: selection.HasUnit);
if (!result.Ok) ShowTooltip(result.FailureReason);   // "NO_SELECTION" | "UNKNOWN_COMMAND"
else            EnqueueVia(result.Kind!.Value);
```

The command → `OrderKind` table (verified against source; ids are trimmed + lower-cased first):

| Command id(s) | `OrderKind` | Notes |
|---------------|-------------|-------|
| `hold` | `Hold` | |
| `rtb` | `ReturnToBase` | |
| `move`, `plot_course` | `Move` | `plot_course` is a semantic alias for course plotting |
| `engage` | `Engage` | The only fire-gated kind (ROE/autonomy downstream) |
| `set_emcon` | `SetEmcon` | CMD-31 |
| `set_sensors` | `SetSensors` | CMD-31 |
| `launch`, `launch_aircraft` | `LaunchAircraft` | LOG-08 / CMD-24 |
| `abort_launch`, `abort_launch_aircraft` | `AbortLaunchAircraft` | |
| `launch_boat` | `LaunchBoat` | LOG-09…11 / CMD-25 |
| `recover_boat` | `RecoverBoat` | |
| `abort_boat_launch` | `AbortBoatLaunch` | |

Two failure reasons are exposed as constants: `ReasonNoSelection` (`"NO_SELECTION"`) and
`ReasonUnknownCommand` (`"UNKNOWN_COMMAND"`). An empty / whitespace / unrecognized id fails with
`UNKNOWN_COMMAND` rather than throwing — a new toolbar button that isn't wired yet degrades to a
tooltip, not a crash.

> **`OrderKind` is append-only.** The enum
> ([`Core/Order.cs`](../../src/ProjectAegis.Delegation/Core/Order.cs)) is extended at the end with
> `// append-only (do not reorder above)` markers — reordering would shift the order-log
> `OrderActionMapper` mapping and move replay fingerprints. Add new commands by appending a kind and
> a `case`, never by reordering.

---

## Agent-roster directives (`AgentDirectiveIssuance`, CMD-37)

Directives come off the agent-roster menu and split into two kinds: **mode changes** (take/return
control) and **orders** (which reuse the human enqueue path). `TryResolve` builds an
`AgentDirectiveRequest(DirectiveId, Action, IsModeChange, OrderKind?)`:

| Directive id | `AgentDirectiveAction` | `IsModeChange` | `OrderKind` |
|--------------|------------------------|----------------|-------------|
| `take_control` | `TakeControl` | `true` | — |
| `return_to_agent` | `ReturnToAgent` | `true` | — |
| `hold` | `Hold` | `false` | `Hold` |
| `rtb` | `Rtb` | `false` | `ReturnToBase` |

Validation has two overloads, both **selection-first**:

- **`Validate(directiveId, hasSelection, hasAgent)`** — the basic form: mode-change directives
  additionally require a related agent (`ReasonNoAgent` / `"NO_AGENT"`); order directives only need
  a selection.
- **`Validate(directiveId, hasSelection, hasActiveAgent, hasSuspendedAgent)`** — the
  controller-state-aware form: **`take_control` requires an active agent** (`ReasonNoActiveAgent`)
  and **`return_to_agent` requires a suspended agent** (`ReasonNoSuspendedAgent`). This mirrors the
  `ControllerSlot` (`Active` + `SuspendedAgent`) semantics in the
  [direct-control override runtime](direct-control-override-runtime.md) — you can only *take* control
  from a live agent and only *return* it to a parked one.

Failure reason constants: `ReasonNoSelection`, `ReasonNoAgent`, `ReasonNoActiveAgent`,
`ReasonNoSuspendedAgent`, `ReasonUnknownDirective`. The host reads `request.IsModeChange` to decide
whether to call the take/return-control path or the order-enqueue path.

---

## Remappable action ids (`C2InputActions`, req-20 / a11y §6.3)

`C2InputActions` is just three `const string` ids — the **single source of truth** the sim resolves
at session start so a host and the remap table never disagree on names:

| Constant | Id | Default key | Meaning |
|----------|----|-------------|---------|
| `CycleUnit` | `input.cycle_unit` | N / P | Cycle to next / previous friendly unit (UX spec §6) |
| `FocusPrimaryThreat` | `input.focus_primary_threat` | F | Centre camera on the primary hostile (a11y §6.3) |
| `Cancel` | `input.cancel` | Esc | Close modal / cancel intent preview / cancel weapons-release gate (a11y §6.3) |

UI hosts bind default keys to these ids; the remap table stores the resolved binding. (Source note:
`input.cycle_unit` is defined here per the UX spec but is not yet listed in
`accessibility-requirements.md` §6.3 — a documented cascade note asks that it be added there so the
constant and the a11y doc agree.)

---

## Host wiring (`C2PlayerCommandBridge`)

The Unity adapter turns a resolved command into an actual queued order. `C2PlayerCommandBridge.TryIssue`
is the single side-effecting choke point, and its order of guards matters:

1. `C2CommandIssuance.TryResolve(commandId, …)` — resolve id → `OrderKind` (else `UNKNOWN_COMMAND`).
2. **Replay guard:** refuse if `bridge.AttachReplayViewer` (`REPLAY_ATTACHED`) — no player ingress
   while a replay/observer is attached, the same choke point the override runtime relies on.
3. **Unit known:** `bridge.Registry.TryGetBinding(entity, …)` else `UNKNOWN_UNIT`.
4. **Human control:** the binding's `Slot.Active` must be a `HumanController` else `NOT_HUMAN_CONTROL`
   — this facade issues orders; it does **not** mutate control slots (that is a directive/mode change).
5. `bridge.TryEnqueueHumanOrder(entity, kind, simTime)` else `ENQUEUE_FAILED`.

All failures come back as structured `failureReason` strings (`REPLAY_ATTACHED`, `NOT_HUMAN_CONTROL`,
`UNKNOWN_UNIT`, `ENQUEUE_FAILED`) for UI tooltips / status labels — the pure layer's
`UNKNOWN_COMMAND` / `NO_SELECTION` plus these host-side reasons form the full player-facing set.

```text
UI click / hotkey  →  C2CommandIssuance.Validate (pure: selection + id → OrderKind)
                         │  ok
                         ▼
                   C2PlayerCommandBridge.TryIssue (host guards: replay / unit / HumanController)
                         │  ok
                         ▼
                   DelegationBridge.TryEnqueueHumanOrder  →  order log / decision tick
```

Agent directives follow the same shape: `AgentDirectiveIssuance.Validate` first, then the host either
enqueues (`IsModeChange == false`) via the same bridge path or performs a take/return-control mode
change through the [override runtime](direct-control-override-runtime.md).

---

## Determinism & safety notes

- **Pure and allocation-light** — the `Input/` helpers use no RNG, no wall-clock, no sim/log access,
  so they never affect replay; determinism concerns live entirely behind the bridge.
- **Append-only `OrderKind`** — extend at the end; reordering breaks the order-log mapping and moves
  fingerprints.
- **Fail-closed ids** — unknown/empty command or directive ids return a reason, never throw.
- **The replay/`AttachReplayViewer` guard is load-bearing** — it is the single place player ingress is
  blocked during replay/observer playback; do not add a command path that bypasses it.
- **Issuance ≠ control change** — `C2PlayerCommandBridge` requires an already-`HumanController` unit
  and never mutates slots; taking/returning control is a directive handled by the override runtime.

---

## Common pitfalls

- **Adding a toolbar command without a kind.** Wire the id into `C2CommandIssuance.TryResolve` *and*
  append the `OrderKind` — otherwise it silently resolves to `UNKNOWN_COMMAND`.
- **Skipping selection validation.** Always go through `Validate(...)` (not raw `TryResolve`) so the
  `NO_SELECTION` rule is applied uniformly.
- **Using the wrong directive overload.** For real take/return-control UX use the four-arg
  `Validate(..., hasActiveAgent, hasSuspendedAgent)` so `NO_ACTIVE_AGENT` / `NO_SUSPENDED_AGENT` are
  enforced; the three-arg form only checks "has *an* agent".
- **Issuing orders to expect a control change.** The command bridge won't take control for you; issue
  a `take_control` directive first.
- **Hard-coding action-id strings** in a host instead of referencing `C2InputActions` — the remap
  table and the sim will drift.

---

## Tests

| Test file | Covers |
|-----------|--------|
| [`C2CommandIssuanceTests`](../../src/ProjectAegis.Delegation.Tests/Input/C2CommandIssuanceTests.cs) | Full command → `OrderKind` table, alias handling, `NO_SELECTION` / `UNKNOWN_COMMAND`. |
| [`AgentDirectiveIssuanceTests`](../../src/ProjectAegis.Delegation.Tests/Input/AgentDirectiveIssuanceTests.cs) | Directive resolution, mode-change vs order, both `Validate` overloads and their reason codes. |
| [`C2PlayerCommandBridgeTests`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/C2PlayerCommandBridgeTests.cs) | Host guard order (replay / unknown unit / not-human) and the enqueue path. |

Run the delegation + adapter suites after any change here:

```bash
dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj -v minimal
```

---

## See also

| Topic | Doc |
|-------|-----|
| Take/return control mode changes the directives drive | [direct-control-override-runtime.md](direct-control-override-runtime.md) |
| Where issued `Engage` orders are ROE/autonomy-gated | [autonomy-roe-gating.md](autonomy-roe-gating.md) |
| The decision tick that consumes queued human orders | [agent-decision-pipeline.md](agent-decision-pipeline.md) |
| Read-model / alert & lifecycle contracts (and `C2InputActions`) | [c2-projection-layer.md](c2-projection-layer.md) |
| Delegation core, the bridge, and the order log | [`src/ProjectAegis.Delegation/README.md`](../../src/ProjectAegis.Delegation/README.md) |
