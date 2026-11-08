# Pending-approval queue — developer guide

The player commands theater-level forces and *delegates* tactical decisions to AI agents, but at
low autonomy the human stays in the loop: an agent may *decide* to engage, yet that order must not
fire until a human clicks **APPROVE**. This page documents the **pending-approval queue** — the
session-local buffer (DRG-66) that holds agent-decided orders which the [autonomy
gate](autonomy-roe-gating.md) routed to `QueueForApproval`, and the approve/reject/drain lifecycle
that turns them into executed orders (or discards them).

It is the "queue" half of the [autonomy / ROE gating](autonomy-roe-gating.md) verdict: that page
explains *why* an order is queued (the `Manual` / `Assisted` × risk table); this page explains
*where the queued order goes*, how the player acts on it, and how the approved order re-enters the
tick loop deterministically.

- **Source:**
  [`src/ProjectAegis.Delegation/Orchestration/PendingApprovalQueue.cs`](../../src/ProjectAegis.Delegation/Orchestration/PendingApprovalQueue.cs)
  (the queue itself),
  [`DelegationOrchestrator`](../../src/ProjectAegis.Delegation/Orchestration/DelegationOrchestrator.cs)
  (owns the queue, exposes the player API, drains it each `Tick`),
  the enqueue call in
  [`AgentController.TryDecide`](../../src/ProjectAegis.Delegation/Controllers/AgentController.cs),
  the gate that produces the verdict in
  [`AutonomyGate`](../../src/ProjectAegis.Delegation/Orchestration/AutonomyGate.cs), and the read-model
  in
  [`PendingApprovalProjection`](../../src/ProjectAegis.Delegation/Projection/PendingApprovalProjection.cs).
- **Pinned by:**
  [`PendingApprovalQueueTests`](../../src/ProjectAegis.Delegation.Tests/Orchestration/PendingApprovalQueueTests.cs)
  (queue unit tests + orchestrator integration) and
  [`PendingApprovalProjectionTests`](../../src/ProjectAegis.Delegation.Tests/Projection/PendingApprovalProjectionTests.cs).
- **Related:** the two-stage authorization that feeds the queue is the
  [autonomy / ROE gating runtime](autonomy-roe-gating.md); the human-takes-the-wheel counterpart
  (swap an agent for a `HumanController` on a whole unit) is the
  [direct-control override runtime](direct-control-override-runtime.md); the queue's read-model is
  part of the [C2 projection layer](c2-projection-layer.md).

---

## Mental model

Every tick, each agent-controlled target runs `AgentController.TryDecide`, which always logs a
`DecisionRecord` **before** the gate runs, then asks the `AutonomyGate` what to do with the resulting
`Order`. The gate returns a `GateResult` of three independent bools:

| Verdict | What happens to the order |
|---------|---------------------------|
| `ExecuteNow` | Added to the agent's issued-order buffer → drained into `ExecutedOrders` **this** tick. |
| `QueueForApproval` | Handed to `PendingApprovalQueue.Enqueue` — held, does **not** execute. |
| `Rejected` (ROE) | Dropped; a `PolicyDenialRecord` is logged. It is **not** queued (a human cannot approve past ROE). |

The pending-approval queue is the home for the middle case. It is a plain in-memory buffer owned by
the `DelegationOrchestrator` for the life of the session — not fingerprinted, not part of the replay
hash (see [Determinism](#determinism--replay-safety)).

Two lists live inside it:

| List | Meaning |
|------|---------|
| `_pending` | Orders awaiting a player decision, ordered by enqueue time. Exposed read-only as `Pending`. |
| `_approved` | Orders the player has approved but that have not yet been injected into a tick. Drained (and cleared) once per `Tick`. |

The lifecycle is a one-way street with two exits:

```
agent decides ──▶ gate = QueueForApproval ──▶ Enqueue ──▶ _pending
                                                            │
                            player: TryApprove(orderId) ────┤────▶ _approved ──▶ DrainApproved() ──▶ ExecutedOrders (next Tick)
                            player: TryReject(orderId)  ────┘────▶ discarded (never executes)
```

Approved orders **skip the AutonomyGate on the way back in**. `DrainApproved` prepends them to
`ExecutedOrders`; they are not re-evaluated as `playerApproved: true`.

---

## Which orders get queued

The queue never decides *whether* to queue — that is the `AutonomyGate`'s job. `TryDecide` always
calls the gate with `playerApproved: false` (the agent is deciding, not the human), so the
autonomy × risk matrix **on the agent path** collapses to:

| Autonomy | Order risk | Gate verdict | Queued? |
|----------|-----------|--------------|---------|
| `Manual` | any | `QueueForApproval` | **Yes** |
| `Assisted` | `Low` | `ExecuteNow` | No |
| `Assisted` | `High` | `QueueForApproval` | **Yes** |
| `SemiAutonomous` | any | `ExecuteNow` | No |
| `FullAutonomous` | any | `ExecuteNow` | No |
| *(any)* | ROE `Reject` | `Rejected` | No (denied, not queued) |
| unrecognized `AutonomyLevel` | any | `QueueForApproval` | **Yes** (`Evaluate` default arm) |

> **`playerApproved` is only false on this path.** `AutonomyGate.Evaluate(Manual|Assisted-High, …,
> playerApproved: true)` would return `ExecuteNow`. `TryDecide` never passes `true`. The approve
> API does **not** go back through `Evaluate` — it promotes via `TryApprove` → `DrainApproved`.

Order risk comes from `DefaultRiskClassifier.Classify`: only `OrderKind.Engage` is `High`; every
other kind (`Move`, `Hold`, `SetEwPosture`, `LaunchAircraft`, …) is `Low`. So in practice, an
`Assisted` agent auto-executes movement/posture orders but parks **weapon-release** orders for human
sign-off, while a `Manual` agent parks everything.

> The ROE filter runs **first** inside the gate. An order that violates ROE is `Rejected` outright
> and never reaches the queue — there is no "approve past ROE" path. This preserves the load-bearing
> [player-approval-can't-override-ROE invariant](autonomy-roe-gating.md).

---

## Public API

### On the queue (`PendingApprovalQueue`)

| Member | Contract |
|--------|----------|
| `IReadOnlyList<PendingApprovalEntry> Pending` | Orders awaiting decision, in enqueue order. |
| `void Enqueue(Order order)` | Adds the order. **Idempotent** — a duplicate `OrderId` is silently ignored, so re-enqueuing the same order does not create a second row. |
| `bool TryApprove(OrderId id)` | Moves the matching pending order into the approved buffer; returns `false` if no match. |
| `bool TryReject(OrderId id)` | Removes and discards the matching pending order; returns `false` if no match. |
| `IReadOnlyList<Order> DrainApproved()` | Returns all approved orders **and clears** the approved buffer. Returns an empty array when nothing is approved. |

`PendingApprovalEntry` is a one-field record (`Order Order`) — the wrapper exists so the queue can
grow per-entry metadata (e.g. enqueue tick, requesting agent) later without changing the row type.

### On the orchestrator (`DelegationOrchestrator`)

The orchestrator owns a single private `PendingApprovalQueue` and re-exports the player-facing
surface:

| Member | Contract |
|--------|----------|
| `IReadOnlyList<PendingApprovalEntry> PendingApprovals` | Live view of what is awaiting approval (delegates to `queue.Pending`). |
| `bool TryApprovePendingOrder(OrderId id)` | Approves; the order is injected into `ExecutedOrders` on the **next** `Tick` that is not `Planning`. |
| `bool TryRejectPendingOrder(OrderId id)` | Rejects and discards; the order never executes. |

There is intentionally **no** "approve all" or "clear" convenience method — approval is a per-order
human action by design. `DelegationBridge` is not on this write path (zero-touch hotpath).

---

## How it wires into the tick loop

Approve/reject are **out-of-band** calls the player makes *between* ticks. The queue only touches
the deterministic loop at two well-defined points inside `DelegationOrchestrator.Tick(ObservedState)`:

0. **Planning short-circuit.** If `Phase == SimulationPhase.Planning`, `Tick` sets
   `ExecutedOrders = []` and **returns before** `DrainApproved`. An approval made during Planning
   stays in `_approved` until the first `BeginExecution()` tick.

1. **Drain first.** Before any agent decides, the orchestrator prepends previously approved orders:

   ```csharp
   // Drain any orders previously approved by the player (DRG-66).
   executed.AddRange(_pendingApprovalQueue.DrainApproved());
   ```

   This means an order the player approved during the pause between tick *N* and *N+1* executes at
   the **start** of tick *N+1*, ahead of that tick's fresh agent decisions.

2. **Enqueue during decision.** Each `AgentController.TryDecide` receives the queue and enqueues
   when the gate says `QueueForApproval`:

   ```csharp
   else if (gateResult.QueueForApproval && pendingQueue != null)
   {
       pendingQueue.Enqueue(order);
   }
   ```

### End-to-end example

```csharp
var orchestrator = new DelegationOrchestrator(
    globalSeed: 7,
    policyEvaluator: new PolicyEvaluator(_ => EffectivePolicy.DefaultFree));

var unit = new UnitTarget(new TargetId("u1"));
var agent = orchestrator.CreateAgent(
    new AgentId("a1"),
    PersonalityCatalog.All[0].Traits,
    AutonomyLevel.Manual);        // Manual → every order queues
unit.Slot.SetActive(agent);
orchestrator.Register(unit);
orchestrator.BeginExecution();

// Tick until the Manual agent parks an order for approval.
for (var i = 0; i < 15 && orchestrator.PendingApprovals.Count == 0; i++)
{
    orchestrator.Tick(state with { SimTime = i });
}

var orderId = orchestrator.PendingApprovals[0].Order.Id;
orchestrator.TryApprovePendingOrder(orderId);   // human clicks APPROVE

orchestrator.Tick(state with { SimTime = 20 }); // approved order drains here
// orchestrator.ExecutedOrders now contains orderId.
```

Rejecting instead (`TryRejectPendingOrder(orderId)`) removes the entry and it never appears in
`ExecutedOrders`.

---

## Presenting the queue: `PendingApprovalProjection`

The C2 UI never mutates the queue from a projection. `PendingApprovalProjection` is a pure, static
converter from `PendingApprovalEntry` rows to display rows (ADR-010 §2–3 / ADR-007 / ADR-001
presentation seam — not sim authority):

| Member | Produces |
|--------|----------|
| `Project(entries)` | `IReadOnlyList<PendingApprovalRow>` — one row per pending order, each with `OrderId`, `TargetId`, a `SummaryLine` (`"{KIND} → {target}"` via `Kind.ToString().ToUpperInvariant()`), a `RiskLabel` (`"RISK: HIGH"` / `"RISK: LOW"`), and an `IsHighRisk` flag for styling. Empty list when nothing is pending. |
| `FormatBadge(entries)` | A single HUD/top-bar string: `"PENDING: —"`, `"PENDING: 3"`, or `"PENDING: 3 (2 HIGH)"`. |
| `EmptyStateLine` / `HeaderLine` | The `"No orders pending approval."` empty state and `"PENDING APPROVAL"` panel header constants. |

The `OrderId` on each row is the token the panel passes back to
`TryApprovePendingOrder` / `TryRejectPendingOrder`. This is the standard read-only
[projection layer](c2-projection-layer.md) shape: projections *describe* state, they never mutate
it, and the only mutation path is the explicit approve/reject call.

Play Mode smoke mentions a `PendingApprovalPanelHost` UXML name. That is Unity chrome, not a
`DelegationBridge` hotpath write and not a `*Bridge` façade in `UnityAdapter/Bridge/`.

---

## Determinism & replay safety

The queue is deliberately **outside** the fingerprinted sim state, and this is load-bearing:

- **The decision is logged; the approval is not.** `TryDecide` appends the `DecisionRecord` to the
  `DecisionLog` *before* the gate runs, so the agent's choice is always in the replay-hashed log
  regardless of the gate verdict. Whether that order later *executes* depends on an out-of-band human
  approval, which is **not** part of the `DecisionLog` and therefore not part of the replay hash.
- **No RNG, no wall-clock.** Enqueue/approve/reject/drain are pure list operations; the queue draws
  no seeded RNG and reads no `DateTime`.
- **Session-local.** The queue is created per `DelegationOrchestrator` and is never serialized into
  golden fixtures.

The practical consequence: **headless replay never approves anything.** With no player in the loop,
`Manual` / `Assisted`-`High` orders simply accumulate in `Pending` and never execute — which is why
the Baltic v2 replay goldens run `PersonalityCatalog.All[0]` at `AutonomyLevel.FullAutonomous`
(`BalticReplayHarness`) whose orders take the `ExecuteNow` path and never touch the queue. Adding the
queue did **not** move the replay golden hash `17144800277401907079`, and extending it must keep that
true.

---

## Add-a-feature runbook

**To surface the queue in a new UI/host:** call `orchestrator.PendingApprovals`, feed it to
`PendingApprovalProjection.Project` (or `FormatBadge` for a badge), render the rows, and wire the
APPROVE/REJECT buttons to `TryApprovePendingOrder` / `TryRejectPendingOrder` with the row's
`OrderId`. Do not read or mutate the queue from the render path any other way. Do not add this to
`DelegationBridge.Tick`.

**To carry extra per-order context (e.g. enqueue tick, deadline):** add fields to the
`PendingApprovalEntry` record and populate them at `Enqueue`. Keep the change additive so existing
projection callers keep compiling, and extend `PendingApprovalProjection` to expose the new fields.

**To change *which* orders queue:** edit the `AutonomyGate` verdict logic (or the
`DefaultRiskClassifier` risk mapping), **not** the queue. The queue is a dumb buffer; the policy
lives in the gate — see [autonomy / ROE gating](autonomy-roe-gating.md).

### Pitfalls

- **Don't approve past ROE.** ROE `Reject` never enters the queue; there is no order there to
  approve. Trying to add one would break the ROE invariant.
- **Drain is destructive.** `DrainApproved()` clears the approved buffer, so it is called exactly
  once per non-Planning tick by the orchestrator. Don't call it from UI/read paths — you'd steal
  orders from the tick loop.
- **Approvals resolve at the next execution tick, not immediately.** `TryApprovePendingOrder` only
  moves the order to the approved buffer; it executes when the next non-Planning `Tick` drains it.
- **`Enqueue` is idempotent by `OrderId`.** Re-submitting the same order won't duplicate it, but it
  also won't refresh it — order ids are monotonic per session (`_orderIdSequence`), so this only
  matters if you manually construct orders.
