# Player-information fog — live-view order-log visibility (req 02/03)

> **ADR:** the live view is a **read-model seam**
> ([`adr-010-headless-first-command-driven-ui.md`](../architecture/adr-010-headless-first-command-driven-ui.md) §2–3):
> fog filters only what the *live HUD reads*, never what the sim records. The complete
> [`DecisionLog`](../../src/ProjectAegis.Delegation/Decision/) stays authoritative for replay and the
> AAR. Player-information visibility is **requirement 02/03**.

During live play a scenario may choose to **hide the agent decisions the player has fully delegated**
— once a unit is at `FullAutonomous`, re-reading every autonomous decision in the message log is
noise, not command value. The **player-information fog** is the single, pure filter that implements
that choice. It drops only `AgentDecision` rows from the *live view* of the order log; every other
entry (engagements, outcomes, policy denials, contacts, mode/comms/fuel changes…) is always shown,
and the underlying log is never modified — so the same run still replays bit-for-bit and the AAR
still sees everything.

Two small pieces, plus one resolver:

| Piece | File | Role |
|-------|------|------|
| [`PlayerInfoModel`](../../src/ProjectAegis.Sim/Scenario/PlayerInfoModel.cs) | `ProjectAegis.Sim/Scenario/` | The visibility enum stored on the scenario policy. |
| [`PlayerInfoFilter`](../../src/ProjectAegis.Delegation/Orchestration/PlayerInfoFilter.cs) | `ProjectAegis.Delegation/Orchestration/` | The pure `FilterLiveEntries` fold (and the `IsDecisionVisibleInLiveView` predicate). |
| [`LoopPolicyGate.ResolvePlayerInfoModel`](../../src/ProjectAegis.Delegation/Orchestration/LoopPolicyGate.cs) | `ProjectAegis.Delegation/Orchestration/` | Resolves the effective model from the scenario policy (default `FullTransparency`). |

---

## The model — `PlayerInfoModel`

```csharp
public enum PlayerInfoModel
{
    FullTransparency, // default — show every entry live
    DelegationFog,    // hide FullAutonomous agent decisions live
    TieredByAutonomy, // hide FullAutonomous agent decisions live
}
```

- **`FullTransparency`** (the default) — the live view equals the full log; `FilterLiveEntries`
  returns the input list unchanged (same reference, no copy).
- **`DelegationFog`** / **`TieredByAutonomy`** — hide agent decisions made at `AutonomyLevel`
  `FullAutonomous`; everything below (`Manual` / `Assisted` / `SemiAutonomous`) stays visible.

> **Honest note — the two fog models are presently identical.** Both resolve through the same
> `autonomy < AutonomyLevel.FullAutonomous` cut in
> [`PlayerInfoFilter.IsDecisionVisibleInLiveView`](../../src/ProjectAegis.Delegation/Orchestration/PlayerInfoFilter.cs).
> `TieredByAutonomy` is reserved for a future per-tier policy but currently behaves exactly like
> `DelegationFog`. Do not document a behavioural difference the code does not yet implement.

[`AutonomyLevel`](../../src/ProjectAegis.Delegation/Core/AutonomyLevel.cs) is ordered
`Manual(1) < Assisted(2) < SemiAutonomous(3) < FullAutonomous(4)`, so `< FullAutonomous` is "anything
the player still has a hand in."

### How a scenario selects a model

`PlayerInfoModel` is a property of
[`ScenarioPolicyProfile`](../../src/ProjectAegis.Sim/Scenario/ScenarioPolicyProfile.cs) (default
`FullTransparency`), authored as `playerInfoModel` in the scenario policy JSON — see
[scenario-policy-authoring.md](scenario-policy-authoring.md). The orchestrator reads it through the
null-safe resolver:

```csharp
// LoopPolicyGate
public static PlayerInfoModel ResolvePlayerInfoModel(ScenarioPolicyProfile? policy) =>
    policy?.PlayerInfoModel ?? PlayerInfoModel.FullTransparency;
```

A scenario with **no** policy (or a policy that leaves the field unset) therefore gets
`FullTransparency` — fog is strictly opt-in, so no scenario loses information by accident.

---

## The filter — `PlayerInfoFilter.FilterLiveEntries`

```csharp
IReadOnlyList<OrderLogEntry> FilterLiveEntries(
    IReadOnlyList<OrderLogEntry> entries, PlayerInfoModel model);
```

- `FullTransparency` → returns `entries` unchanged.
- Otherwise → `entries.Where(IsVisibleInLiveView(…, model))`, a new array; the input list is never
  mutated.

`IsVisibleInLiveView` only ever drops **`AgentDecision`** entries (`OrderLogEntryKind.AgentDecision`),
reading the autonomy level from either payload shape the order log carries:

- [`AgentDecisionPayload`](../../src/ProjectAegis.Delegation/Decision/AgentDecisionPayload.cs) (the
  typed C1 payload), or
- [`DecisionRecord`](../../src/ProjectAegis.Delegation/Decision/DecisionRecord.cs) (the legacy
  record, auto-wrapped by `OrderLogEntry`).

Every **non-`AgentDecision`** entry — and any `AgentDecision` whose payload is neither shape —
**fails open to visible** (`_ => true`). A new order-log kind can never be accidentally fogged; only
agent decisions are ever hidden, and only at `FullAutonomous`.

`IsDecisionVisibleInLiveView(AutonomyLevel autonomy, PlayerInfoModel model)` is exposed publicly so a
caller can ask the visibility question for a single decision without building an entry list.

---

## Entry points — one filter, two folds, same result

The fog is applied at exactly one place (`FilterLiveEntries`); callers reach it by two equivalent
paths, and both match each other and `GetLiveOrderLogView()`. Neither path mutates or ticks anything.

```text
ScenarioPolicyProfile.PlayerInfoModel
        │  LoopPolicyGate.ResolvePlayerInfoModel (default FullTransparency)
        ▼
DelegationOrchestrator.GetLiveOrderLogView()
        = PlayerInfoFilter.FilterLiveEntries(DecisionLog entries, model)
        │
        ├── DelegationBridge.GetLiveOrderLogView()                 (Unity facade passthrough)
        │        └── MessageLogBridge.ProjectLive / BindLive(bridge)
        │                 = MessageLogProjection.Project(bridge.GetLiveOrderLogView())
        │
        └── MessageLogProjection.ProjectLive(log, model)           (projection-level convenience)
                 = Project(PlayerInfoFilter.FilterLiveEntries(log.ChronologicalEntries(), model))
```

| Entry point | Who calls it | Notes |
|-------------|--------------|-------|
| [`DelegationOrchestrator.GetLiveOrderLogView()`](../../src/ProjectAegis.Delegation/Orchestration/DelegationOrchestrator.cs) | Core / headless | The canonical live view. Resolves the model from `ScenarioPolicy` itself. |
| [`DelegationBridge.GetLiveOrderLogView()`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs) | Unity host | Thin passthrough to the orchestrator. |
| [`MessageLogBridge.ProjectLive` / `BindLive`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/MessageLogBridge.cs) | Unity live HUD (DRG-248) | Projects the bridge's live view; `BindLive` tags the panel `LiveOrderLogView`. |
| [`MessageLogProjection.ProjectLive(log, model)`](../../src/ProjectAegis.Delegation/Projection/MessageLogProjection.cs) | Tests / direct projection (DRG-402) | Takes the model explicitly; filters then projects in one call. |

The **replay / AAR** side deliberately does **not** go through the fog: `MessageLogProjection.Project(log)`
and `MessageLogBridge.ProjectReplay` / `BindReplay` read the complete `DecisionLog`. The message-log
entry point is documented from the panel's perspective in
[c2-projection-layer.md](c2-projection-layer.md#two-entry-points-full-log-vs-live-player-view).

---

## Invariants

- **Read-only.** `FilterLiveEntries` only *reads* the entry list and returns a view; it never writes
  the log, the sim, or the catalog. It is pure (no RNG, no wall clock) and safe to re-run every frame.
- **The log is never edited — only the entry set is narrowed.** Replay and the AAR read the full log,
  so fog cannot change the order-log replay fingerprint or the Baltic v2 `SimWorldHash`
  (`17144800277401907079`). See [determinism-and-replay.md](determinism-and-replay.md).
- **Only `AgentDecision` rows are ever hidden, and only at `FullAutonomous`.** Engagement, outcome,
  and policy-denial entries are identical in the live and full views — which is exactly why the
  combat-event fence over those rows (see
  [combat-event-log-projection.md](combat-event-log-projection.md) /
  [c2-projection-layer.md](c2-projection-layer.md#combat-lines-are-fenced-on-the-combat-event-projection-drg-402))
  behaves the same whether you project the live view or the full log.
- **Fail-open & opt-in.** Unknown models, unknown payloads, and absent scenario policies all resolve
  to "show everything." A new order-log kind is never silently fogged.

---

## Tests (pins)

Part of the ≥1638-test baseline (see [AGENTS.md](../../AGENTS.md)):

[`PlayerInfoFilterTests`](../../src/ProjectAegis.Delegation.Tests/Orchestration/PlayerInfoFilterTests.cs)
— the filter + orchestrator wiring:

| Test | Covers |
|------|--------|
| `FullTransparency_returns_all_entries` | Default model is a passthrough. |
| `DelegationFog_hides_full_autonomous_decisions_but_keeps_alerts` | Drops only the `FullAutonomous` decision; keeps the `Assisted` decision **and** the policy denial. |
| `Orchestrator_GetLiveOrderLogView_uses_scenario_player_info_model` | `GetLiveOrderLogView()` honors `ScenarioPolicy.PlayerInfoModel` end to end. |

[`MessageLogLiveBindingTests`](../../src/ProjectAegis.Delegation.Tests/Projection/MessageLogLiveBindingTests.cs)
— `MessageLogProjection.ProjectLive` (both fog models via `[Theory]`): live projection omits fogged
decisions but keeps alerts, the full projection still reads every decision afterward, `FullTransparency`
matches the full projection, `null` log throws, and the bind source is tagged correctly.

[`MessageLogLiveBridgeTests`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/MessageLogLiveBridgeTests.cs)
— the Unity facade: `ProjectLive` omits fogged decisions while replay keeps them, `ProjectLive`
matches `Project(GetLiveOrderLogView())`, `FullTransparency` matches `ProjectFrom`, `BindLive`/`BindReplay`
document their source, the host tick binds `LastMessageLog` from `ProjectLive`, and null arguments throw.

```bash
dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj \
  --filter "PlayerInfoFilterTests|MessageLogLiveBindingTests" -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter MessageLogLiveBridgeTests -v minimal
```

---

## See also

| Topic | Doc |
|-------|-----|
| Message-log panel & the live-vs-full entry points | [c2-projection-layer.md](c2-projection-layer.md#two-entry-points-full-log-vs-live-player-view) |
| Combat-event fence (identical in both views) | [combat-event-log-projection.md](combat-event-log-projection.md) |
| Authoring `playerInfoModel` in scenario policy JSON | [scenario-policy-authoring.md](scenario-policy-authoring.md) |
| Autonomy levels & decision-time ROE gating | [autonomy-roe-gating.md](autonomy-roe-gating.md) |
| Determinism, fingerprint, replay goldens | [determinism-and-replay.md](determinism-and-replay.md) |
| The `DecisionLog` timeline these entries live on | [order-log-runtime.md](order-log-runtime.md) |
