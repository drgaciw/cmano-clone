# Message log — the order log as a CMANO-style event feed (DRG-248 / DRG-402)

> **ADR:** this is **read-only presentation** of the order log, not a second event store and not an
> order path ([`adr-010-headless-first-command-driven-ui.md`](../architecture/adr-010-headless-first-command-driven-ui.md) §2–3,
> [`adr-007-c2-map-presentation.md`](../architecture/adr-007-c2-map-presentation.md),
> [`adr-001-sim-assembly-boundary.md`](../architecture/adr-001-sim-assembly-boundary.md)). It folds the
> append-only [`DecisionLog`](order-log-runtime.md) into a flat, scannable list of **message log lines** —
> the CMANO-style running feed of *what just happened* (a launch, an abort, a kill, a contact change, a
> mode change, a fuel/comms/damage event) — for the live HUD panel, replay/AAR, and the compact combat
> strip. It **never** mutates the log, enqueues an order, or touches sim state.

The message log answers one C2 question — *give me the running narration of events, in order* — and derives
it entirely from the order log, so there is no duplicate storage and no way for the feed to drift from the
replay truth. The subsystem is one pure projection plus a thin binder:

1. [`MessageLogProjection`](../../src/ProjectAegis.Delegation/Projection/MessageLogProjection.cs)
   (`ProjectAegis.Delegation/Projection/`) — the sim-agnostic fold of
   [`OrderLogEntry`](order-log-runtime.md) rows into [`MessageLogLine`](../../src/ProjectAegis.Delegation/Projection/MessageLogLine.cs)
   DTOs. No Unity, no orchestrator, no I/O.
2. [`MessageLogPanelBinder`](../../src/ProjectAegis.Delegation/Projection/MessageLogPanelBinder.cs)
   (`ProjectAegis.Delegation/Projection/`) — turns those DTOs into UI-Toolkit-friendly
   `MessageLogDisplayRow`s (prefixed display line + per-category USS class), tagged with the order-log
   view they came from.

The [`MessageLogBridge`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/MessageLogBridge.cs) facade
wires both to the Unity host; the projection and binder themselves carry no `UnityEngine` reference.

| Piece | File | Role |
|-------|------|------|
| [`MessageLogProjection`](../../src/ProjectAegis.Delegation/Projection/MessageLogProjection.cs) | `Delegation/Projection/` | The pure fold: `Project(DecisionLog)`, `ProjectLive(DecisionLog, PlayerInfoModel)`, and the core `Project(IReadOnlyList<OrderLogEntry>)`. |
| [`MessageLogLine`](../../src/ProjectAegis.Delegation/Projection/MessageLogLine.cs) | `Delegation/Projection/` | One feed line DTO (`SequenceId`, `SimTime`, `Category`, `Text`, `UnitId?`). Projected, never stored. |
| [`MessageLogPanelBinder`](../../src/ProjectAegis.Delegation/Projection/MessageLogPanelBinder.cs) | `Delegation/Projection/` | `Bind(lines[, source])` → `MessageLogPanelState`. Formats `[CATEGORY] text` and stamps the USS class. |
| [`MessageLogPanelState`](../../src/ProjectAegis.Delegation/Projection/MessageLogPanelState.cs) | `Delegation/Projection/` | The bound panel: `Rows` (`MessageLogDisplayRow`) + the `MessageLogBindingSource` the rows came from. |
| [`MessageLogCategoryClassMap`](../../src/ProjectAegis.Delegation/Projection/MessageLogCategoryClassMap.cs) | `Delegation/Projection/` | Single source of truth: category → USS state class (ASSET-006), so hosts never duplicate the tint table. |
| [`MessageLogPanelSelection`](../../src/ProjectAegis.Delegation/Projection/MessageLogPanelSelection.cs) | `Delegation/Projection/` | Headless row selection / deep-link helpers (CMD-05): by index, by `SequenceId`, by `UnitId`. |
| [`MessageLogBridge`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/MessageLogBridge.cs) | `UnityAdapter/Bridge/` | Headless/Unity facade: `ProjectLive` / `BindLive` for the HUD, `ProjectReplay` / `BindReplay` for AAR, `ProjectCombatMessages` for the combat strip. |

---

## The projection — `MessageLogProjection`

`Project(entries)` walks the order log **in order** and emits a line per recognised entry; an unrecognised
kind projects nothing (the `default` arm returns `null` and is skipped). Three entry points share that one
core:

| Entry point | View | Use |
|-------------|------|-----|
| `Project(DecisionLog)` | Full log, including fogged agent decisions. | Replay / AAR. |
| `Project(IReadOnlyList<OrderLogEntry>)` | Whatever entries you hand it. | The shared core; also drives single-entry reuse (e.g. the commit-refusal bark). |
| `ProjectLive(DecisionLog, PlayerInfoModel)` | The live player view. | The HUD. |

**`ProjectLive` honors the player-info fog.** It runs the entries through
[`PlayerInfoFilter.FilterLiveEntries`](../../src/ProjectAegis.Delegation/Orchestration/PlayerInfoFilter.cs)
first, so agent decisions hidden by [`PlayerInfoModel`](../../src/ProjectAegis.Sim/Scenario/PlayerInfoModel.cs)
`DelegationFog` / `TieredByAutonomy` are omitted — matching
`DelegationOrchestrator.GetLiveOrderLogView()`. It **does not** modify the log: a `ProjectLive` call
followed by a full `Project` still reads every decision (`FullTransparency` makes the two identical). This
is the DRG-248 live-vs-full split: the live HUD must not leak decisions the player is not entitled to see,
while replay/AAR stays complete.

### Non-combat lines — the direct `TryProject` map

Most entry kinds map one-to-one to a line. The category is a stable string token; the text is the
human-readable summary; `UnitId` (when present) is the deep-link target.

| Order-log kind | Category | Example text |
|----------------|----------|--------------|
| `PolicyDenial` | `POLICY_DENIAL` | `Fire denied for hostile-1: WeaponsTight (Engage)` |
| `ContactChange` | `CONTACT` | `Contact c7 Detected → Tracked (hostile-1)` |
| `MagazineChange` | `MAGAZINE` | `Magazine u1 mount M1: -2 (SALVO)` |
| `ModeChange` | `MODE` | `Mode Agent → Human` |
| `PlayerOrder` | `PLAYER_ORDER` | `Player ordered Engage for u1` |
| `CommsStateChange` | `COMMS` | `Comms node-3: Up → Degraded (jammed)` |
| `FuelStateChange` / `FuelBurn` | `FUEL` | `Fuel u1: Nominal → Bingo (420 kg)` |
| `OrdnanceStateChange` | `ORDNANCE` | `Ordnance u1: Nominal → Winchester (rem 0)` |
| `PolicyUpdate` | `POLICY_UPDATE` | `Policy RoeLevel: Tight → Free` |
| `AgentDecision` | `AGENT_DECISION` | `Agent a1 chose Engage for hostile-1: …` |
| `MissionTransition` | `MISSION` | `Mission ev-2 → PhaseB` |
| `EventFired` | `EVENT` | `Event ev-9: RECON_SPOTTED` |
| `PlatformDamageChange` | `DAMAGE` | `Damage u1: 100% → 60% (missile-hit)` |
| `ControllerChange` | `CONTROLLER` | `Controller u1: Agent → Human` |

`AgentDecision` accepts both the current `AgentDecisionPayload` and the legacy `DecisionRecord` payload, so
old logs still project.

### Combat lines are gated by the combat-event snapshot (DRG-402)

Launch, abort, and terminal-outcome lines are **not** read straight off the engagement rows. Instead the
projection builds a combat sub-log of only the `Engagement` / `EngagementOutcome` entries and runs it
through [`CombatEventLogProjection.Build`](combat-event-log-projection.md), then collects the
`CorrelationId`s that reached each phase:

- `Firing` + `OutcomeLaunch` → the launch set (→ `WEAPON_LAUNCH`, `Unit {shooter} launched engagement {id}`).
- `AuthorizationRefused` → the abort set (→ `ENGAGE_ABORT`, `Engagement aborted: {AbortReasonCode}`).
- `TerminalOutcome` → the terminal set (→ the outcome line, below).

A launch/abort line is emitted only when that engagement's `SequenceId` is in the matching set, and an
outcome line only when `TryTakeTerminal` can **consume** its correlated terminal. This is the keystone
honesty rule: **a combat line exists only when the combat-event snapshot recorded the launch, abort, or
terminal.** An `EngagementOutcome` with no corresponding combat event never invents a `KILL_CONFIRMED`, and
a `PolicyDenial` stays a `POLICY_DENIAL` line rather than doubling as an `ENGAGE_ABORT`. The
`MessageLogCombatEventSourceFenceTests` pin this by asserting the source goes through
`CombatEventLogProjection.Build` + `CorrelationId` and never re-reads the raw `.Engagements` /
`.EngagementOutcomes` collections (and never `RemoveAt`).

Terminal outcomes map to stable combat categories:

| `OutcomeCode` | Category | Text |
|---------------|----------|------|
| `Kill` | `KILL_CONFIRMED` | `Hostile destroyed: {victim} (engagement {id})` |
| `Intercept` | `INTERCEPT_SUCCESS` | `Threat neutralized (intercept): {victim} — target remains operational` |
| `Hit` | `HIT` | `Weapon hit {victim} (engagement {id})` |
| `Miss` | `MISS` | `Weapon missed {victim} (engagement {id})` |
| *(other)* | `COMBAT` | `Outcome {code} vs {victim}` |

### Terminal correlation — `IndexOutcomeCorrelations`

Display fields are deliberately **not** identity. `IndexOutcomeCorrelations` maps each outcome's
`SequenceId` to the `SequenceId` of the launch of *its own* engagement: it groups launches by
`(ShooterId, EngagementId)`, orders them, and for each outcome picks the owning launch whose sequence
window `(launch, nextLaunch]` contains the outcome — disambiguating by victim id when a launch names one.
`TryTakeTerminal` then **removes** that correlation from the terminal set, so a second unmatched outcome
with the same shooter/target/code/sim-time cannot consume another engagement's terminal. Repeated
engagement ids on the same shooter stay isolated.

---

## The binder — `MessageLogPanelBinder` and the USS class map

`Bind(lines)` defaults the view tag to `MessageLogBindingSource.FullDecisionLog`; `Bind(lines, source)`
records the view explicitly. Each line becomes a `MessageLogDisplayRow`:

- `DisplayLine` is `"[" + Category + "] " + Text` — the category is always visible in the text, so the feed
  reads on a monochrome panel.
- `CategoryCssClass` comes from
  [`MessageLogCategoryClassMap.CssClassFor`](../../src/ProjectAegis.Delegation/Projection/MessageLogCategoryClassMap.cs)
  (ASSET-006) — the **single** place category tints live, so the Unity host never re-encodes the table.
  `SequenceId` and `UnitId` ride along for deep-linking.

`CssClassFor` trims and upper-cases, then maps: `KILL_CONFIRMED` / `HIT` and `DAMAGE` → `--kill`,
`WEAPON_LAUNCH` → `--weapon`, `MAGAZINE` → `--magazine`, `COMMS` → `--comms`,
`CONTACT` / `CONTACT_CHANGE` → `--contact`, `MISSION` / `MISSION_TRANSITION` / `EVENT` → `--mission`,
`POLICY_DENIAL` / `POLICY_UPDATE` → `--policy`; anything else → `null` (no dedicated tint). The base row
class is `message-log-row` and the selectable modifier is `message-log-row--selectable`.

### Binding source — `MessageLogBindingSource`

`FullDecisionLog` (`0`) is the complete, unfiltered log for replay/AAR — under `delegationFog` /
`tieredByAutonomy` it includes agent decisions the player must not see live. `LiveOrderLogView` (`1`) is the
player-info-filtered live HUD view. The tag travels on `MessageLogPanelState.Source` so a surface can assert
it bound the right view.

### Selection / deep-link — `MessageLogPanelSelection`

The headless selection helper (CMD-05 depth) returns a `MessageLogSelection(SequenceId, UnitId?, RowIndex)`:
`SelectRow(state, index)` (bounds-checked), `SelectBySequenceId`, and `SelectByUnitId` (the **latest** —
highest-index — row matching, case-sensitive unit ids). `TryFindRowIndexBySequenceId` /
`TryFindRowIndexByUnitId` are the `out int index` forms the host uses to scroll/focus a row and hand the
unit id to `DelegationBridgeHost.SelectUnit`.

---

## The facade — `MessageLogBridge`

The adapter facade keeps the host off the projection internals and consumes a `DecisionLog` / bridge only —
no live ECS or session write handle:

| Method | View | Returns |
|--------|------|---------|
| `ProjectLive(bridge)` / `BindLive(bridge)` | `bridge.GetLiveOrderLogView()` (player-info filtered) | lines / panel tagged `LiveOrderLogView` |
| `ProjectReplay(log)` / `BindReplay(log)` (and legacy `ProjectFrom`) | the complete `DecisionLog` | lines / panel tagged `FullDecisionLog` |
| `ProjectCombatMessages(log)` | full log, filtered | the compact combat strip: `KILL_CONFIRMED` / `INTERCEPT_SUCCESS` / `HIT` / `MISS` / `MAGAZINE` |

All overloads throw `ArgumentNullException` on a null log/bridge and never tick or mutate the bridge.

---

## Consumers

| Surface | Host / caller | Path |
|---------|---------------|------|
| HUD message log panel | [`MessageLogPanelHost`](../../unity/ProjectAegis/Assets/Scripts/Runtime/MessageLogPanelHost.cs) | Binds `DelegationBridgeHost.LastMessageLog` via `MessageLogPanelBinder.Bind`, tails to `maxRows`, and skips the list rebuild while the chrome is collapsed. |
| Replay / AAR | [`BalticReplayHarness`](baltic-replay-harness.md) | `MessageLogProjection.Project(bridge.Orchestrator.DecisionLog)`. |
| Combat-domains hot-tick HUD | [`CombatDomainsHotTickTracker`](combat-domains-hot-tick-hud.md) | Consumes the `MessageLogLine.Category` stream to drive the six-domain overlay. |
| Commit-refusal bark | [`CommitRefusalBarkBinder`](../../src/ProjectAegis.Delegation.UnityAdapter/Presentation/CommitRefusalBarkBinder.cs) | Reuses `MessageLogProjection.Project` on a single abort entry so the bark reason matches the feed verbatim. |

---

## Invariants

- **Projection, not storage** — every line is derived from an existing `OrderLogEntry`; the message log keeps
  no state of its own and cannot drift from the replay truth.
- **Read-only presentation** — never mutates the `DecisionLog`, enqueues an order, or touches sim state; the
  bridge holds no live ECS / session write handle.
- **Combat lines are snapshot-gated** — launch / abort / kill lines exist only when
  `CombatEventLogProjection` recorded the event (DRG-402); an orphan outcome never fabricates a kill and a
  terminal is consumed at most once per correlated launch.
- **Live view honors the fog** — `ProjectLive` omits agent decisions hidden by the scenario
  `PlayerInfoModel`; the full projection stays complete, and neither mutates the log.
- **One category→class table** — `MessageLogCategoryClassMap` is the single source of truth for tints
  (ASSET-006); the category token is always in the display text so colour is never the only signal.
- **Pure / deterministic** — a `static` fold over the entry list; no RNG, no wall clock, no I/O, no
  `UnityEngine` in the projection or binder. Same log ⇒ same lines.
- **Off the sim fingerprint** — presentation chrome; it reads the order log the sim already owns and the
  Baltic v2 replay hash is untouched.

---

## See also

| Topic | Doc |
|-------|-----|
| The append-only `DecisionLog` + the `OrderLogEntryKind` payloads this folds | [order-log-runtime.md](order-log-runtime.md) |
| The combat-event snapshot that gates the launch/abort/kill lines | [combat-event-log-projection.md](combat-event-log-projection.md) |
| The six-domain hot-tick HUD that consumes `MessageLogLine.Category` | [combat-domains-hot-tick-hud.md](combat-domains-hot-tick-hud.md) |
| General read-model / binder / host rules and the live-vs-full split | [c2-projection-layer.md](c2-projection-layer.md) |

---

## Tests

[`MessageLogProjectionTests`](../../src/ProjectAegis.Delegation.Tests/Projection/MessageLogProjectionTests.cs),
[`MessageLogCombatEventSourceFenceTests`](../../src/ProjectAegis.Delegation.Tests/Projection/MessageLogCombatEventSourceFenceTests.cs),
and [`MessageLogLiveBindingTests`](../../src/ProjectAegis.Delegation.Tests/Projection/MessageLogLiveBindingTests.cs)
pin the per-kind category map, the combat-event source gate (no fabricated kill, policy-denial stays a
policy line, single-abort still projects for the bark), and the live-vs-full fog split;
[`MessageLogPanelBinderTests`](../../src/ProjectAegis.Delegation.Tests/Projection/MessageLogPanelBinderTests.cs),
[`MessageLogCategoryClassMapTests`](../../src/ProjectAegis.Delegation.Tests/Projection/MessageLogCategoryClassMapTests.cs),
[`MessageLogPanelSelectionTests`](../../src/ProjectAegis.Delegation.Tests/Projection/MessageLogPanelSelectionTests.cs),
and [`MessageLogPanelSelectionDepthTests`](../../src/ProjectAegis.Delegation.Tests/Projection/MessageLogPanelSelectionDepthTests.cs)
pin the `[CATEGORY]` prefix, the USS class table, and the index/sequence/unit selection; the UnityAdapter
[`MessageLogBridgeTests`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/MessageLogBridgeTests.cs),
[`MessageLogLiveBridgeTests`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/MessageLogLiveBridgeTests.cs),
[`MessageLogAsset006WireTests`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/MessageLogAsset006WireTests.cs),
and [`MessageLogPanelBinderIntegrationTests`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/MessageLogPanelBinderIntegrationTests.cs)
pin the facade's live-vs-replay binding, the combat-strip subset, and the ASSET-006 USS wiring. All green
(47 in `ProjectAegis.Delegation.Tests`, 20 in `ProjectAegis.Delegation.UnityAdapter.Tests`).
