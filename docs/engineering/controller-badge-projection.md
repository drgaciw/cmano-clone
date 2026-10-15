# Controller-badge projection — who controls each unit, on the map and OOB (DEL-01 / DRG-374 / DRG-375)

> **ADR:** this is a **presentation read-model**, not a control path
> ([`adr-010-headless-first-command-driven-ui.md`](../architecture/adr-010-headless-first-command-driven-ui.md) §2–3,
> [`adr-007-c2-map-presentation.md`](../architecture/adr-007-c2-map-presentation.md),
> [`adr-001-sim-assembly-boundary.md`](../architecture/adr-001-sim-assembly-boundary.md)). It reads the
> agent-roster output only; it **never** touches a `ControllerSlot`, suspends/resumes a controller, or
> mutates sim state. It answers one question for the tactical picture: *who is flying this unit right
> now — the player, an agent, or a suspended agent?*

A unit's map symbol and its Order-of-Battle (OOB) row both need a compact, colour-independent marker of
its controller. The **controller-badge projection** derives that marker from the same
[`AgentRosterEntry`](../../src/ProjectAegis.Delegation/Projection/AgentRosterEntry.cs) rows the roster
panel already builds (CMD-37). It is a **pure** fold
([`ControllerBadgeProjection.Project`](../../src/ProjectAegis.Delegation/Projection/ControllerBadgeProjection.cs))
over read-only roster facts: one [`ControllerBadge`](../../src/ProjectAegis.Delegation/Projection/ControllerBadgeProjection.cs)
per row, carrying shape, short text, accessible label, and a colour token as **independent channels** so
colour is never the only signal. The Unity-side
[`ControllerBadgeBinder`](../../src/ProjectAegis.Delegation.UnityAdapter/Presentation/ControllerBadgeBinder.cs)
turns those badges into bindings the map and OOB hosts paint.

| Piece | File | Role |
|-------|------|------|
| [`ControllerBadgeProjection`](../../src/ProjectAegis.Delegation/Projection/ControllerBadgeProjection.cs) | `Delegation/Projection/` | The pure fold: `Project` / `FromRosterEntry` + `ParseKind` / `FormatKind`. |
| [`ControllerBadgeKind`](../../src/ProjectAegis.Delegation/Projection/ControllerBadgeProjection.cs) | `Delegation/Projection/` | `None` / `Human` / `Agent` / `AgentSuspended`. |
| [`ControllerBadge`](../../src/ProjectAegis.Delegation/Projection/ControllerBadgeProjection.cs) | `Delegation/Projection/` | Immutable per-unit badge: `Shape`, `ShortText`, `AccessibleLabel`, `ColorToken`, `AgentId`, `HasHeldAgent`. |
| [`ControllerBadgeBinder`](../../src/ProjectAegis.Delegation.UnityAdapter/Presentation/ControllerBadgeBinder.cs) | `UnityAdapter/Presentation/` | Roster → `ControllerBadgeBinding` lookup + map/OOB helpers + rebind/dirty tracking. |
| [`ControllerBadgeBinding`](../../src/ProjectAegis.Delegation.UnityAdapter/Presentation/ControllerBadgeBinder.cs) | `UnityAdapter/Presentation/` | Applied binding: `ShortText`, `Tooltip`, `ColorClass`, `ShapeClass`, `Visible`, `OobSuffix`. |
| [`AgentRosterProjection`](../../src/ProjectAegis.Delegation/Projection/AgentRosterProjection.cs) / [`AgentRosterApplyState`](../../src/ProjectAegis.Delegation/Projection/AgentRosterApplyState.cs) | `Delegation/Projection/` | The upstream roster rows; `AgentRosterApplyState` also carries a `Badge` per display row (DRG-375). |

---

## The fold — `FromRosterEntry`

Each badge is derived from one roster entry's `ModeLabel`. `ParseKind` maps the label
(case-insensitive, trimmed) to a kind — `"Human"` → `Human`, `"Agent"` → `Agent`,
`"AgentSuspended"` → `AgentSuspended`, and **anything unrecognised → `None`** (never a throw). The
upstream roster emits exactly those four `ModeLabel` values from
[`AgentRosterProjection.MapStatusAndMode`](../../src/ProjectAegis.Delegation/Projection/AgentRosterProjection.cs);
an `Overloaded` attention status changes the *status* label but keeps the controller `ModeLabel`, so an
overloaded agent still badges as `Agent`.

The `AgentId` is carried through, with the roster's `"—"` missing-id sentinel
([`AgentRosterProjection.MissingAgentId`](../../src/ProjectAegis.Delegation/Projection/AgentRosterProjection.cs))
normalised to `null`. `HasHeldAgent` is true only when a **`Human`** badge still names a suspended
agent — i.e. the player has taken direct control of a unit whose agent is parked.

Every kind gets a distinct, independently-set set of channels:

| Kind | `Shape` | `ShortText` | `ColorToken` | `AccessibleLabel` |
|------|---------|-------------|--------------|-------------------|
| `Human` | `square` | `HUM` | `badge-human` | `"Human control"` — or `"Human control (agent <id> suspended)"` when `HasHeldAgent`. |
| `Agent` | `diamond` | `AGT` | `badge-agent` | `"Agent control (<id>)"`. |
| `AgentSuspended` | `diamond-dashed` | `AGT-SUSP` | `badge-agent-suspended` | `"Agent suspended (<id>)"`. |
| `None` | `none` | `—` | `badge-none` | `"No controller"`. |

Shape, short text, colour token, and accessible label are all distinct across the four kinds, so the
badge is legible without colour vision — the accessibility contract pinned by
`Badges_are_distinguishable_without_colour`.

---

## Binding — `ControllerBadgeBinder`

`BindRoster` projects the roster and returns an **ordinal** `UnitId → ControllerBadgeBinding` dictionary
(null/empty roster → empty map). Rows with a blank `UnitId` are skipped; when a unit id repeats, the
**later** row wins. A `None` badge stays in the map with `Visible == false` (it is a known "no
controller" fact, not a missing one).

`FromBadge` turns a `ControllerBadge` into a `ControllerBadgeBinding`:

- `Visible` = `Kind != None`.
- `ColorClass` = the badge `ColorToken`; `ShapeClass` = `"badge-shape-" + Shape`; `Tooltip` = the
  `AccessibleLabel`.
- `OobSuffix` = `" [" + ShortText + "]"` when visible, else empty.

Consumers:

- **`TryGet`** — lookup by unit id; missing ids and null inputs return false.
- **`DecorateOobLine`** — appends the visible badge suffix (` [HUM]` / ` [AGT]` / ` [AGT-SUSP]`) to an
  OOB display line; hidden and missing bindings leave the line unchanged.
- **`IsFriendlyMapSymbol`** — true only when the space-delimited USS class string contains the exact
  `map-symbol--friendly` or `map-symbol--friendly-dead` token (a substring such as
  `map-symbol--friendlyextra` does **not** match).
- **`TryBindMapSymbol`** — binds a map symbol only when it is friendly (or friendly-destroyed) **and**
  the roster badge is visible. Hostile/neutral symbols and unknown units return false, so the player's
  own side is the only side that shows a controller badge.
- **`ElementName`** — the UI Toolkit child name for a unit: `"controller-badge-" + unitId`.

### Live vs. historical roster — `RosterForBadges`

`RosterForBadges(inspecting, liveRoster, asOfRoster)` chooses which roster to paint: the **live** roster
while commanding, the **as-of** roster captured at an inspected event's tick while reviewing history. A
null as-of roster is *unknown* and deliberately **hides** badges rather than falling back to the live
roster — a past event must not be annotated with present-tense control facts (no future facts).

### Dirty tracking — `RosterStamp` / `SameRoster` / `NeedsRebind`

`RosterStamp` is a 64-bit polynomial content stamp over `(count, UnitId, ModeLabel, AgentId)` per row.
Because a 64-bit stamp can collide, `NeedsRebind` treats a matching stamp as *necessary but not
sufficient*: it confirms with a structural `SameRoster` field-by-field comparison before skipping a
rebind, so a polynomial collision can never pin a stale badge (`Colliding_32_bit_mix_still_rebinds…`).

---

## Host consumers — map symbols and OOB rows

The live consumers are the two Unity panel hosts; both keep the badge roster **off** their `IsDirty`
inputs and reapply badges as a follow-on step.

- **[`MapPlaceholderPanelHost`](../../unity/ProjectAegis/Assets/Scripts/Runtime/MapPlaceholderPanelHost.cs)** —
  `ApplyControllerBadges` runs after each symbol sync (the symbol pool clears a symbol's children when a
  row changes, so the badge `Label` is re-created). For each `map-symbol` child it calls
  `TryBindMapSymbol`, then (re)builds a pick-ignoring `Label` named `ElementName(unitId)`, sets its text
  and tooltip, and applies `MapBadgeClass` (`map-controller-badge`) + the colour and shape classes. A
  hidden/absent binding removes the badge label.
- **[`OobTreePanelHost`](../../unity/ProjectAegis/Assets/Scripts/Runtime/OobTreePanelHost.cs)** — decorates
  each row's display line with `DecorateOobLine` and adds `OobBadgeClass` (`oob-controller-badge`) to the
  row label.

Both hosts read `RosterForBadges(CommandTimeline.IsInspecting, LastAgentRoster, asOfRoster: null)`, so an
inspected event with no captured roster hides badges instead of painting the current one. The USS tokens
(`map-controller-badge` / `oob-controller-badge`, `badge-human` / `badge-agent` /
`badge-agent-suspended`, `badge-shape-square` / `badge-shape-diamond` / `badge-shape-diamond-dashed`)
live in `MapPlaceholderPanel.uss` and `OobTreePanel.uss`.

---

## Invariants

- **Read-only** — a pure fold over roster rows; no `ControllerSlot` mutation, no sim or orchestrator
  write (ADR-010 §2–3, ADR-007, ADR-001). `Badge_follows_assign_take_control_and_return_to_agent…`
  drives an orchestrator through assign → take control → return → suspend and asserts the badge tracks
  the slot **without** the projection ever mutating it.
- **Never colour-only** — shape, short text, colour token, and accessible label are set independently for
  every kind.
- **No silent gap** — a `None` controller is an explicit, hidden-but-present badge, not a dropped row.
- **Friendly-only on the map** — hostile/neutral map symbols never show a controller badge.
- **No future facts in review** — a historical inspection with no as-of roster hides badges.
- **No `UnityEngine`** in the projection; the Unity dependency lives only in the `ControllerBadgeBinder`
  consumers. The projection is off the sim fingerprint — the Baltic v2 replay hash is untouched.

---

## See also

| Topic | Doc |
|-------|-----|
| Controller arbitration (`ControllerSlot`, take/return control) that drives the roster `ModeLabel` | [direct-control-override-runtime.md](direct-control-override-runtime.md) |
| The player command / roster directive seam | [c2-command-issuance-runtime.md](c2-command-issuance-runtime.md) |
| The adapter seam between panel hosts and projections | [c2-presentation-bridges.md](c2-presentation-bridges.md) |
| General read-model / projection rules | [c2-projection-layer.md](c2-projection-layer.md) |

## Tests

- [`ControllerBadgeProjectionTests`](../../src/ProjectAegis.Delegation.Tests/Projection/ControllerBadgeProjectionTests.cs)
  (NUnit, 7 tests) pin roster-order binding, the colour-independent distinctness contract, the held-agent
  label, attention-keeps-kind, the unknown-label → `None` fallback, the null/empty guard, and the
  assign/take/return/suspend round-trip that proves no slot mutation.
- [`ControllerBadgeBinderTests`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/ControllerBadgeBinderTests.cs)
  (NUnit, 11 tests) pin the binding map (visible kinds + hidden `None`), the held-agent tooltip, OOB-line
  decoration, friendly-only map binding, the stamp/`SameRoster`/`NeedsRebind` collision guard, the
  live-vs-as-of roster selection, `ElementName`, and that the map/OOB hosts bind badges (and wire the
  USS tokens) without opening `DelegationBridge.Tick` / `CatalogWriteGate` / `SimulationSession`.
