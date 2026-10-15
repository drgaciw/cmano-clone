# Controller-badge chrome — the "who controls this unit?" badge on the roster, OOB, and map (DEL-01 / DRG-252 / DRG-374 / DRG-375)

> **ADR:** this is the **presentation / read-model seam**
> ([`adr-010-headless-first-command-driven-ui.md`](../architecture/adr-010-headless-first-command-driven-ui.md) §2–3
> — a binder is a **client**, not sim authority), with
> [`adr-007-c2-map-presentation.md`](../architecture/adr-007-c2-map-presentation.md) (C2 presentation) and
> [`adr-001-sim-assembly-boundary.md`](../architecture/adr-001-sim-assembly-boundary.md) (the adapter boundary).

The **controller-badge chrome** answers one narrow question wherever a unit appears: *who is flying it right
now — the player, an agent, or a suspended agent?* It folds the already-projected agent-roster rows into a
small, non-color-coded badge (`HUM` / `AGT` / `AGT-SUSP`, or nothing) that three C2 surfaces drop next to a
unit — the **agent-roster** list, the **OOB tree** rows, and the **map** symbols. It is split across two
files, one per layer:

1. [`ProjectAegis.Delegation/Projection/ControllerBadgeProjection.cs`](../../src/ProjectAegis.Delegation/Projection/ControllerBadgeProjection.cs)
   — the pure, engine-agnostic projection (`ControllerBadgeKind`, `ControllerBadge`, `Project`, `FromRosterEntry`, `ParseKind`/`FormatKind`).
2. [`ProjectAegis.Delegation.UnityAdapter/Presentation/ControllerBadgeBinder.cs`](../../src/ProjectAegis.Delegation.UnityAdapter/Presentation/ControllerBadgeBinder.cs)
   — the adapter binder (`BindRoster`, `TryGet`, `DecorateOobLine`, `TryBindMapSymbol`, `RosterForBadges`, the rebind guard) + the `ControllerBadgeBinding` DTO.

It is strictly a **read-model**: it reads only `AgentRosterEntry` rows that
[`AgentRosterProjection`](../../src/ProjectAegis.Delegation/Projection/AgentRosterProjection.cs) already
produced from the controller slots. It **never** touches a `ControllerSlot`, suspends or resumes an agent,
takes direct control, queries the sim, or issues an order, and it is **not** part of the Baltic v2
`SimWorldHash`.

---

## Input — one `AgentRosterEntry` per unit

The sole input is the roster the OOB/CMD-37 panel already builds. The badge is derived entirely from one
field, `ModeLabel`:

```csharp
public sealed record AgentRosterEntry(
    string AgentId, string UnitId, string AutonomyLabel,
    string StatusLabel, string AttentionLabel, string ModeLabel);
```

`ControllerBadgeProjection.ParseKind(entry.ModeLabel)` maps the (trimmed, case-insensitive) mode string onto a
`ControllerBadgeKind`; **any unknown value falls back to `None`** (`Unknown_mode_label_falls_back_to_None`):

| `ModeLabel` | `ControllerBadgeKind` |
|-------------|-----------------------|
| `Human` | `Human` |
| `Agent` | `Agent` |
| `AgentSuspended` | `AgentSuspended` |
| anything else | `None` |

A changing *status* (e.g. attention `Overloaded`) never changes the badge kind — only the controller mode does
(`Attention_status_keeps_controller_kind`). `FormatKind` is the inverse map, used by tests to build roster
rows.

`AgentId` is normalised to `null` when it is blank or the `AgentRosterProjection.MissingAgentId` sentinel
(`—`), so a human-held unit with no suspended agent reads cleanly.

---

## The four badge kinds — three independent channels

```csharp
public sealed record ControllerBadge(
    string UnitId, ControllerBadgeKind Kind,
    string Shape, string ShortText, string AccessibleLabel,
    string ColorToken, string? AgentId, bool HasHeldAgent);
```

`Shape`, `ShortText`, and `AccessibleLabel` each carry the state **independently of `ColorToken`**, so colour
is never the only channel (`Badges_are_distinguishable_without_colour` asserts all four of
shape/text/color/label are distinct across the kinds and none is empty):

| Kind | `Shape` | `ShortText` | `ColorToken` | `AccessibleLabel` |
|------|---------|-------------|--------------|-------------------|
| `None` | `none` | `—` | `badge-none` | `No controller` |
| `Human` | `square` | `HUM` | `badge-human` | `Human control` |
| `Agent` | `diamond` | `AGT` | `badge-agent` | `Agent control (<agentId>)` |
| `AgentSuspended` | `diamond-dashed` | `AGT-SUSP` | `badge-agent-suspended` | `Agent suspended (<agentId>)` |

### Human-with-held-agent

When the player takes direct control of a unit whose own agent is parked, the roster row stays `Human` **and**
carries the suspended `AgentId`. The badge then sets `HasHeldAgent = true` and names the held agent in the
accessible label — `Human control (agent <id> suspended)`
(`Human_with_suspended_agent_names_held_agent_in_text`). This is the controller-arbitration state owned by the
[direct-control-override runtime](direct-control-override-runtime.md); the badge only reports it.

`ControllerBadgeProjection.Project` emits one badge per roster row, **preserving roster order**; a null or
empty roster returns an empty list (`Project_null_or_empty_returns_empty`).

---

## The binder — roster → per-unit lookup

```csharp
IReadOnlyDictionary<string, ControllerBadgeBinding> BindRoster(IReadOnlyList<AgentRosterEntry>? roster)
```

`BindRoster` projects the roster and folds it into a `UnitId → ControllerBadgeBinding` dictionary (ordinal
keys, later rows win on a repeat, empty input → empty map). Each binding adds the UI-Toolkit-ready channels:

```csharp
public sealed record ControllerBadgeBinding(
    string UnitId, string ShortText, string Tooltip,
    string ColorClass, string ShapeClass, bool Visible, string OobSuffix);
```

- `ColorClass` is the badge's `ColorToken`; `ShapeClass` is `"badge-shape-" + Shape` (e.g. `badge-shape-diamond-dashed`).
- `Tooltip` is the `AccessibleLabel`.
- `Visible` is **false for `None`** — a unit with no controller is kept in the map but never painted.
- `OobSuffix` is `" [HUM]"` / `" [AGT]"` / `" [AGT-SUSP]"` when visible, else the empty string.

`TryGet(bindings, unitId, out binding)` is the lookup; missing ids and null inputs return `false`.

### OOB rows — `DecorateOobLine`

```csharp
string DecorateOobLine(string? displayLine, ControllerBadgeBinding? binding)
```

Appends the visible `OobSuffix` to a row's text (`"u1 READY" → "u1 READY [HUM]"`); a hidden (`None`) or missing
binding leaves the line unchanged, and a null line becomes just the suffix
(`DecorateOobLine_appends_visible_short_text_and_leaves_hidden_rows_unchanged`).

### Map symbols — friendly-only, word-boundary gated

```csharp
bool TryBindMapSymbol(string? symbolId, string? styleClass,
    IReadOnlyDictionary<string, ControllerBadgeBinding>? bindings, out ControllerBadgeBinding? binding)
```

A map symbol gets a badge **only** when it is friendly (or friendly-destroyed) *and* the roster badge is
visible. `IsFriendlyMapSymbol` matches the `map-symbol--friendly` / `map-symbol--friendly-dead` tokens on a
**word boundary** (split on spaces), so `map-symbol--friendlyextra` does **not** match. Hostile/neutral
symbols, unknown units, and `None` badges all return `false`
(`Map_symbol_bind_accepts_friendly_and_friendly_dead_only`, `None_badge_does_not_bind_a_friendly_symbol`). The
element name for a symbol's badge child is `ControllerBadgeBinder.ElementName(unitId)` =
`"controller-badge-" + unitId`.

### Historical review — no future facts

```csharp
IReadOnlyList<AgentRosterEntry>? RosterForBadges(bool inspecting, IReadOnlyList<AgentRosterEntry>? liveRoster, IReadOnlyList<AgentRosterEntry>? asOfRoster)
```

While commanding, badges use the **live** roster. During a timeline/replay inspection they must use the roster
captured **as of that event's tick** — and because replay frames do not retain controller slots, a `null`
as-of roster **hides** badges rather than falling back to the live one (no future facts leaking into the past:
`Historical_review_uses_as_of_roster_or_hides_when_unknown`).

### Rebind guard — stamp, then confirm

To avoid rebuilding bindings every frame, the hosts gate on `NeedsRebind(hasApplied, appliedStamp, applied,
next)`:

1. `RosterStamp` is a 64-bit content stamp over `(count, UnitId, ModeLabel, AgentId)` in roster order.
2. A **matching stamp still re-confirms** with `SameRoster` (field-by-field structural equality), so a 64-bit
   polynomial collision can never keep a stale badge/tooltip
   (`Colliding_32_bit_mix_still_rebinds_when_agent_ids_differ`, `RosterStamp_tracks_content_not_list_identity`).

---

## Consumers — three hosts, one source of truth

All three render in their panel refresh/bind path, **never** on the `DelegationBridge.Tick` hot path, and none
touches `IOrderSink` / `CatalogWriteGate` / `SimulationSession` (asserted by `Hosts_bind_badges_without_opening_sim_authority`):

| Host | Surface | Path |
|------|---------|------|
| [`AgentRosterPanelHost`](../../unity/ProjectAegis/Assets/Scripts/Runtime/AgentRosterPanelHost.cs) | the CMD-37 agent-roster list | binds the projection's badge **directly** off each `AgentRosterDisplayRow.Badge` (via `AgentRosterApplyState.Apply` / `ApplyWithAttention`) — no second binder — reading `Shape`/`ShortText`/`ColorToken`/`AccessibleLabel` onto the `.agent-roster-badge` element (USS legibility ≥ 12 px). |
| [`OobTreePanelHost`](../../unity/ProjectAegis/Assets/Scripts/Runtime/OobTreePanelHost.cs) | the OOB tree rows | `DecorateOobLine` for the text, tooltip from the visible binding, then `OobBadgeClass` (`oob-controller-badge`) + the color/shape classes. |
| [`MapPlaceholderPanelHost`](../../unity/ProjectAegis/Assets/Scripts/Runtime/MapPlaceholderPanelHost.cs) | friendly map symbols | `ApplyControllerBadges()` (re-run after symbol sync, since the symbol pool clears children on a row change) — `NeedsRebind`-gated `BindRoster`, then per friendly symbol a `controller-badge-<unitId>` child `Label` with `MapBadgeClass` (`map-controller-badge`) + the color/shape classes; the roster stamp is deliberately **not** an `IsDirty` input. |

> `MapPlaceholderPanelHost` and the `DelegationBridge` hot path are zero-touch invariants — this chrome only
> *reads* their output. Both USS stylesheets
> ([`MapPlaceholderPanel.uss`](../../unity/ProjectAegis/Assets/UI/MapPlaceholder/MapPlaceholderPanel.uss),
> [`OobTreePanel.uss`](../../unity/ProjectAegis/Assets/UI/OobTree/OobTreePanel.uss),
> [`AgentRosterPanel.uss`](../../unity/ProjectAegis/Assets/UI/AgentRoster/AgentRosterPanel.uss)) declare the
> `.badge-human` / `.badge-agent` / `.badge-agent-suspended` colour classes and the
> `.badge-shape-{square,diamond,diamond-dashed,none}` shape classes.

The headless apply path is `AgentRosterApplyState`, which attaches
`ControllerBadgeProjection.FromRosterEntry(entry)` to every `AgentRosterDisplayRow.Badge` so the roster host can
bind the badge without re-formatting; `ApplyWithAttention` keeps the badge pinned to the controller mode even
as the attention label changes (`ApplyWithAttention_keeps_badge_from_mode_while_attention_changes`).

---

## Determinism & invariants

- **Read-model, off the fingerprint.** Built from already-projected roster rows; writes no log, is not part of
  the `SimWorldHash` or order-log fingerprint. The Baltic v2 hash `17144800277401907079` is untouched.
- **Never mutates controller state.** The badge follows assign → take-control → return-to-agent → suspend
  transitions purely by re-reading the roster; it never touches `ControllerSlot.Active` / `SuspendedAgent`
  (`Badge_follows_assign_take_control_and_return_to_agent_without_mutating_slot`).
- **Never color-only.** Shape + short text + accessible label each distinguish the four kinds; colour is an
  additive channel.
- **Friendly-only on the map, word-boundary matched.** Hostile/neutral symbols and larger identifiers never
  match.
- **No future facts.** Historical inspection uses the as-of roster or hides; it never paints the live roster
  onto a past event.
- **Collision-safe rebind.** A matching 64-bit stamp is re-confirmed structurally before a rebind is skipped.
- **Pure.** The projection and binder touch only their arguments — no RNG, no wall clock, no `UnityEngine`.

---

## Tests (pins)

| Fixture | Cases | Covers |
|---------|-------|--------|
| [`ControllerBadgeProjectionTests`](../../src/ProjectAegis.Delegation.Tests/Projection/ControllerBadgeProjectionTests.cs) | 7 | roster-order projection, the colour-independent channels, the held-agent label, status-independence, unknown→`None`, null/empty, and the assign/take-control/return/suspend walk with no slot mutation. |
| [`ControllerBadgeBinderTests`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/ControllerBadgeBinderTests.cs) | 11 | `BindRoster` mapping + hidden `None`, held-agent tooltip, `DecorateOobLine`, friendly/friendly-dead map gating, the collision-safe `RosterStamp`/`SameRoster`/`NeedsRebind` guard, historical as-of vs hide, `ElementName`, and the host-wiring + USS contract (no `DelegationBridge.Tick`). |
| [`AgentRosterBadgeApplyTests`](../../src/ProjectAegis.Delegation.Tests/Projection/AgentRosterBadgeApplyTests.cs) | 5 | `AgentRosterApplyState` attaches the badge to each row, keeps it under `ApplyWithAttention`, re-reads after take-control/return, and pins the roster host wiring + `.agent-roster-badge` legibility/shape/colour USS. |

Part of the ≥1638-test baseline.

---

## See also

- [c2-projection-layer.md](c2-projection-layer.md) — general read-model / presentation-boundary rules; the agent roster is one of the projections it catalogs.
- [c2-presentation-bridges.md](c2-presentation-bridges.md) — the host-feed pattern the OOB/map/roster panels use.
- [direct-control-override-runtime.md](direct-control-override-runtime.md) — the controller-arbitration runtime (`ControllerSlot`, take/return/suspend) whose state this badge reports; the held-agent case is its suspend path.
- [replay-checkpoint-observer-runtime.md](replay-checkpoint-observer-runtime.md) — the inspection/observer mode the as-of-roster "no future facts" rule serves.
