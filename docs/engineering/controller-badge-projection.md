# Controller badges — who controls a unit (DEL-01 / DRG-252 / DRG-374 / DRG-375)

> **ADR:** this is **presentation chrome**, not a controller seam or an order path
> ([`adr-010-headless-first-command-driven-ui.md`](../architecture/adr-010-headless-first-command-driven-ui.md) §2–3,
> [`adr-007-c2-map-presentation.md`](../architecture/adr-007-c2-map-presentation.md),
> [`adr-001-sim-assembly-boundary.md`](../architecture/adr-001-sim-assembly-boundary.md)). It folds the
> agent-roster read-model into one **controller badge** per unit — *is this unit flown by the player, by
> an agent, or by a suspended agent?* — for the OOB rows, the agent-roster panel, and the friendly map
> symbols. It **never** mutates a `ControllerSlot`, enqueues an order, or touches sim state.

The badge answers a single command-picture question — *who is holding this unit right now* — and answers it
in three independent channels (**shape**, **short text**, **colour**) so the state survives on a
monochrome display or for a colour-blind watch officer. The subsystem is two pure stages:

1. [`ControllerBadgeProjection`](../../src/ProjectAegis.Delegation/Projection/ControllerBadgeProjection.cs)
   (`ProjectAegis.Delegation/Projection/`) — the sim-agnostic fold of an
   [`AgentRosterEntry`](../../src/ProjectAegis.Delegation/Projection/AgentRosterEntry.cs) list into
   `ControllerBadge` DTOs. No Unity, no orchestrator, no I/O.
2. [`ControllerBadgeBinder`](../../src/ProjectAegis.Delegation.UnityAdapter/Presentation/ControllerBadgeBinder.cs)
   (`ProjectAegis.Delegation.UnityAdapter/Presentation/`) — the adapter that turns those DTOs into a
   unit-id → `ControllerBadgeBinding` lookup for map symbols and OOB rows, plus the roster-change caching
   (`RosterStamp` / `SameRoster` / `NeedsRebind`) and the live-vs-historical roster selector.

The binder reads the projection; both read the roster output only. Neither calls back into the
orchestrator or a controller slot — the roster is already the derived picture of who controls what.

| Piece | File | Role |
|-------|------|------|
| [`ControllerBadgeKind`](../../src/ProjectAegis.Delegation/Projection/ControllerBadgeProjection.cs) | `Delegation/Projection/` | The four states: `None` / `Human` / `Agent` / `AgentSuspended`. |
| [`ControllerBadge`](../../src/ProjectAegis.Delegation/Projection/ControllerBadgeProjection.cs) | `Delegation/Projection/` | One badge DTO (`UnitId`, `Kind`, `Shape`, `ShortText`, `AccessibleLabel`, `ColorToken`, `AgentId`, `HasHeldAgent`). Three visual channels are independent fields. |
| [`ControllerBadgeProjection`](../../src/ProjectAegis.Delegation/Projection/ControllerBadgeProjection.cs) | `Delegation/Projection/` | The pure fold: `Project(roster)`, the per-row `FromRosterEntry`, and the `ParseKind` / `FormatKind` mode-label round-trip. |
| [`ControllerBadgeBinding`](../../src/ProjectAegis.Delegation.UnityAdapter/Presentation/ControllerBadgeBinder.cs) | `UnityAdapter/Presentation/` | One *applied* badge (`ShortText`, `Tooltip`, `ColorClass`, `ShapeClass`, `Visible`, `OobSuffix`). The USS-class form the hosts paint. |
| [`ControllerBadgeBinder`](../../src/ProjectAegis.Delegation.UnityAdapter/Presentation/ControllerBadgeBinder.cs) | `UnityAdapter/Presentation/` | `BindRoster` → lookup, `TryGet`, `DecorateOobLine`, the friendly-symbol gate (`IsFriendlyMapSymbol` / `TryBindMapSymbol`), the rebind cache, and `RosterForBadges`. |
| [`AgentRosterApplyState`](../../src/ProjectAegis.Delegation/Projection/AgentRosterApplyState.cs) | `Delegation/Projection/` | The roster-panel apply path; stamps `AgentRosterDisplayRow.Badge` with `FromRosterEntry` so the panel host binds the DTO without a second binder. |

---

## Controller states — `ControllerBadgeKind`

| Kind | Shape | Short text | Colour token | Accessible label | Meaning |
|------|-------|:----------:|--------------|------------------|---------|
| `Human` | `square` | `HUM` | `badge-human` | `"Human control"` (or `"Human control (agent {id} suspended)"`) | Player has direct control. |
| `Agent` | `diamond` | `AGT` | `badge-agent` | `"Agent control ({agentId})"` | An agent controller is active. |
| `AgentSuspended` | `diamond-dashed` | `AGT-SUSP` | `badge-agent-suspended` | `"Agent suspended ({agentId})"` | An agent is bound but no controller is active. |
| `None` | `none` | `—` | `badge-none` | `"No controller"` | No controller bound. Hidden on the map / OOB (`Visible` false). |

`Shape`, `ShortText`, and `ColorToken` are **distinct per kind** (asserted by the tests) so colour is never
the only signal.

---

## The projection — `ControllerBadgeProjection`

`Project(roster)` maps each `AgentRosterEntry` to one `ControllerBadge` **in roster order** (null/empty →
empty). The per-row work is `FromRosterEntry`:

- **Kind comes from `ModeLabel`, not `StatusLabel`.** `ParseKind` matches `"Human"` / `"Agent"` /
  `"AgentSuspended"` case-insensitively (trimmed); anything else is `None`. An agent that is *overloaded*
  gets `StatusLabel: "Attention"` but keeps `ModeLabel: "Agent"`, so its badge still reads `AGT` — the
  badge is about *who controls*, not *how loaded they are*.
- **`AgentId` resolution** — an empty, whitespace, or sentinel `—`
  ([`AgentRosterProjection.MissingAgentId`](../../src/ProjectAegis.Delegation/Projection/AgentRosterProjection.cs))
  agent id becomes `null`.
- **Held agent** — a `Human` row that still carries a real `AgentId` is a *take-control-over-a-suspended-
  agent* state: `HasHeldAgent` is `true` and the label names the suspended agent
  (`"Human control (agent {id} suspended)"`). Only `Human` can hold an agent.

`FormatKind` is the inverse of `ParseKind` (the mode-label a kind round-trips to), used by tests and hosts
that want the roster string for a kind.

Because the fold is a pure read of the roster, the badge *follows* an
`AssignAgent → TryTakeDirectControl → TryReleaseDirectControl → SuspendAgent` sequence
(`None → Agent → Human (held) → Agent → AgentSuspended`) **without the projection ever mutating the slot** —
the orchestrator owns the slot; the badge only reports it.

---

## The binder — `ControllerBadgeBinder`

`BindRoster(roster)` projects the roster and returns an ordinal `unitId → ControllerBadgeBinding` lookup
(later rows win on a repeated id; a `None` row stays in the map with `Visible: false`; empty-id rows are
dropped). `TryGet` is the null-safe lookup. `FromBadge` derives the binding: `ShapeClass` is
`"badge-shape-" + Shape`, `ColorClass` is the badge colour token, `Visible` is `Kind != None`, and
`OobSuffix` is `" [SHORT]"` when visible (else empty).

### OOB rows — `DecorateOobLine`

`DecorateOobLine(displayLine, binding)` appends the suffix (` [HUM]` / ` [AGT]` / ` [AGT-SUSP]`) when the
binding is visible, and leaves hidden or missing bindings (and a `null` line) unchanged. The
[`OobTreePanelHost`](../../unity/ProjectAegis/Assets/Scripts/Runtime/OobTreePanelHost.cs) uses it to decorate
the row label and adds the `oob-controller-badge` class.

### Map symbols — the friendly gate

`TryBindMapSymbol(symbolId, styleClass, bindings, out binding)` binds a badge onto a map symbol **only**
when the symbol is friendly (`map-symbol--friendly`) or friendly-destroyed (`map-symbol--friendly-dead`)
**and** the unit's badge is visible. `IsFriendlyMapSymbol` scans the style class as whitespace-delimited
tokens, so `map-symbol--friendlyextra` does **not** match — only the exact token. Hostile/neutral symbols,
unknown unit ids, and `None` badges return `false`. The
[`MapPlaceholderPanelHost`](../../unity/ProjectAegis/Assets/Scripts/Runtime/MapPlaceholderPanelHost.cs)
attaches a child `Label` named `ElementName(unitId)` (`"controller-badge-" + unitId`) with the
`map-controller-badge`, colour, and shape classes.

### Live vs historical — `RosterForBadges`

`RosterForBadges(inspecting, liveRoster, asOfRoster)` chooses the roster to paint. Live command uses the
live roster; a historical review uses the `asOfRoster` captured **at that event's tick**. A `null` as-of
roster is *unknown* and must **not** fall back to the live roster — the map host passes `asOfRoster: null`
while inspecting, so an inspected frame **hides** badges rather than painting current (future-relative)
control onto a past moment.

### Rebind cache — `RosterStamp` / `SameRoster` / `NeedsRebind`

The hosts cache the applied bindings and only rebuild when the roster's badge-relevant content
(`UnitId` / `ModeLabel` / `AgentId`, in order) changes. `RosterStamp` is a 64-bit content stamp;
`SameRoster` is the structural equality; `NeedsRebind(hasApplied, appliedStamp, applied, next)` returns
`true` when nothing is applied yet, when the stamp differs, **or** when the stamp matches but `SameRoster`
is `false`. The structural confirm is load-bearing: a 64-bit polynomial stamp collision can never keep a
stale tooltip (the tests force a 32-bit `Mix` collision and assert the rebind still fires).

---

## Consumers

| Surface | Host | Path |
|---------|------|------|
| Agent-roster panel | [`AgentRosterPanelHost`](../../unity/ProjectAegis/Assets/Scripts/Runtime/AgentRosterPanelHost.cs) | Binds `AgentRosterDisplayRow.Badge` (the `ControllerBadge` DTO) directly — `ShortText` / `ColorToken` / `Shape` / `AccessibleLabel` — via [`AgentRosterPresentationCache`](../../src/ProjectAegis.Delegation/Projection/AgentRosterPresentationCache.cs). No binder. |
| OOB tree | [`OobTreePanelHost`](../../unity/ProjectAegis/Assets/Scripts/Runtime/OobTreePanelHost.cs) | `BindRoster` + `DecorateOobLine` + `NeedsRebind`. |
| Friendly map symbols | [`MapPlaceholderPanelHost`](../../unity/ProjectAegis/Assets/Scripts/Runtime/MapPlaceholderPanelHost.cs) | `BindRoster` + `TryBindMapSymbol` + `RosterForBadges` (hides badges while inspecting with no as-of roster). |

The roster panel reuses an unchanged presentation with **zero per-frame allocation** after warm-up
(`AgentRosterPresentationCache`), and rebuilds when any badge-relevant field (unit id, mode, agent id,
autonomy, status, attention, order) changes.

---

## Invariants

- **Read-only presentation** — never mutates a `ControllerSlot` or sim state; the hosts carry no
  `DelegationBridge.Tick`, `CatalogWriteGate`, `SimulationSession`, or `IOrderSink` reference (asserted by
  the host-source tests).
- **Colour is never the only channel** — shape, short text, and colour are independent per kind; the USS
  carries both shape and colour classes and the badge text meets the roster legibility baseline
  (`≥ 12px` at 100% scale).
- **Kind from mode, not status** — attention/overload changes the `StatusLabel` but not the badge; the
  badge reports control, not load.
- **No future facts in review** — a historical inspection with no as-of roster hides badges rather than
  painting the live controller onto a past event.
- **Pure / deterministic** — a `static` fold over the roster list; no RNG, no wall clock, no I/O. Same
  roster ⇒ same badges. No `UnityEngine` in the projection or binder; the Unity dependency lives only in
  the hosts.
- **Off the sim fingerprint** — presentation chrome; it reads nothing the sim fingerprints and the Baltic
  v2 replay hash is untouched.

---

## See also

| Topic | Doc |
|-------|-----|
| The roster read-model + general projection/binder/host rules | [c2-projection-layer.md](c2-projection-layer.md) |
| The adapter seam between Unity hosts and projections | [c2-presentation-bridges.md](c2-presentation-bridges.md) |
| The decision-time autonomy/ROE write path the control states come from | [autonomy-roe-gating.md](autonomy-roe-gating.md) |
| The player take-control / return-to-agent command seam | [direct-control-override-runtime.md](direct-control-override-runtime.md) |

---

## Tests

[`ControllerBadgeProjectionTests`](../../src/ProjectAegis.Delegation.Tests/Projection/ControllerBadgeProjectionTests.cs)
and [`AgentRosterBadgeApplyTests`](../../src/ProjectAegis.Delegation.Tests/Projection/AgentRosterBadgeApplyTests.cs)
pin the four-kind ladder, the distinguishable-without-colour contract, the held-agent label, the
mode-not-status rule, the `None` fallback, and the assign/take-control/return/suspend sequence without
slot mutation; [`AgentRosterPresentationCacheTests`](../../src/ProjectAegis.Delegation.Tests/Projection/AgentRosterPresentationCacheTests.cs)
pins the zero-allocation reuse and the badge-relevant-field cache invalidation;
[`ControllerBadgeBinderTests`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/ControllerBadgeBinderTests.cs)
pins the binding map, the OOB suffix, the friendly-only map gate (friendly / friendly-dead only, exact
token), the stamp-collision rebind, the live-vs-as-of roster selection, and the host-source fences
(hosts bind badges but open no sim authority). All green (29 tests).
