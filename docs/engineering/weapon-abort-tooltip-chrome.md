# Weapon-abort tooltip chrome (DRG-258 / DRG-352 / DRG-407)

> Detailed companion: [weapon-abort-tooltip-projection.md](weapon-abort-tooltip-projection.md).

> **ADR:** this is the **presentation / read-model seam**
> ([`adr-010-headless-first-command-driven-ui.md`](../architecture/adr-010-headless-first-command-driven-ui.md) §2–3
> — chrome is a **client**, not sim authority), with
> [`adr-007-c2-map-presentation.md`](../architecture/adr-007-c2-map-presentation.md) (C2 presentation) and
> [`adr-001-sim-assembly-boundary.md`](../architecture/adr-001-sim-assembly-boundary.md) (the adapter boundary).

The **weapon-abort tooltip chrome** answers one hover/focus question on the unit-detail panel — *why can't I fire
this weapon?* — by surfacing up to **three** already-computed abort reasons on a denied weapon row (REQ-13 ROE-07).
It never evaluates the ROE/engage gate itself: it only **reads** the already-projected
[engage-explain](engage-explain-contract-projection.md) DTO and the combat-detail card and copies the matching
reason strings onto the row's tooltip. It issues no order, builds no engage card, and touches no sim state.

The whole feature lives in the adapter Presentation layer plus the unit-panel host:

| File | Role |
|------|------|
| [`WeaponAbortTooltipBinder.cs`](../../src/ProjectAegis.Delegation.UnityAdapter/Presentation/WeaponAbortTooltipBinder.cs) | the pure binder (`Bind`, `BindForSelection`, `ScopeCombatDetail`, `BindRows`, `AppliesTo`) + the abort-token table |
| [`RightUnitCombatDetailCache.cs`](../../src/ProjectAegis.Delegation.UnityAdapter/Presentation/RightUnitCombatDetailCache.cs) | DRG-407 memo of the per-selection explain + scoped detail + tooltip |
| [`RightUnitPanelHost.cs`](../../unity/ProjectAegis/Assets/Scripts/Runtime/RightUnitPanelHost.cs) | the UI-Toolkit host that paints the tooltip onto the two weapon rows |

It is **read-only**: it reads DTOs that other projections already produced and emits display strings. It never
calls `IOrderSink` / `ApplyOrder` / `TryIssue`, touches `DelegationBridge` / `CatalogWriteGate` /
`SimulationSession`, or re-runs the engage gate, and it is **not** part of the Baltic v2 `SimWorldHash`.

---

## Inputs — the explain DTO and the combat-detail card

`WeaponAbortTooltipBinder.Bind(EngageExplain? explain, CombatDetailPresentation? detail = null)` is the single
entry point. Both inputs are **already-projected read models**, never sim internals:

- [`EngageExplain`](../../src/ProjectAegis.Delegation/Projection/EngageExplainProjection.cs) — `StatusLine`,
  `ReasonCode`, `ReasonPlain`, `IsBlocked`, produced by `EngageExplainProjection.Project`.
- [`CombatDetailPresentation`](../../src/ProjectAegis.Delegation.UnityAdapter/Presentation/CombatDetailPresentation.cs)
  — the ten evidence lines (`StatusLine`, `WeaponLine`, `HardConstraintsLine`, `PolicyLine`, `ConfidenceLine`,
  `FiringSolutionLine`, `NextActionLine`, `BdaLine`, `PostureLine`, `CorrelationLine`).

### Fail-closed on a clear launch

A **clear** explain suppresses everything, including any historical detail reasons
(`Clear_engage_explain_suppresses_historical_detail_reasons`):

```csharp
private static bool IsClear(EngageExplain? explain) =>
    explain is { IsBlocked: false }
    && string.Equals(explain.StatusLine, EngageExplainProjection.CanFireLabel, StringComparison.Ordinal);
```

Otherwise the binder reads the explain only when `explain.IsBlocked == true`, and reads the detail only when it is
non-null and not `CombatDetailPresentation.Empty`. If neither source is usable, the result is
`WeaponAbortTooltipState.Empty` (`Null_explain_and_detail_yield_empty_tooltip`).

---

## The abort-token table — what counts as a reason

`BuildTokens()` builds the fixed set of recognised abort strings once:

1. Every [`FireAbortReason`](../../src/ProjectAegis.Sim/Policy/FireAbortReason.cs) except `None`, registered under
   **both** its enum name (`RoeHoldFire`) and its SCREAMING_SNAKE log code (`ROE_HOLD_FIRE`) — both resolve to the
   **enum-name** display, so a log code is normalised onto the `FireAbortReason` name
   (`Catalog_log_code_maps_onto_FireAbortReason_name`).
2. The doctrine string `AbortReasonCatalog.Doctrine.ROE_WEAPONS_TIGHT` → `WeaponsTight`.
3. A list of **equivalent** [abort-reason-catalog](abort-reason-catalog.md) strings from the `Engage`,
   `Logistics`, `Sensor`, and `Cyber` families (`MOUNT_OFFLINE`, `DLZ_OUT`, `NO_AMMO`, `BINGO_FUEL`,
   `SENSOR_OFFLINE`, `CYBER_LINK_DOWN`, …) that have no `FireAbortReason` member; these keep their catalog string
   as their own display.

Each token carries a **priority**: `FireAbortReason` tokens use the enum's integer value (so priority follows the
`FireAbortReason` declaration order, *not* the order fields appear in the detail card), and equivalents start at
`EquivalentPriorityBase` (`1000`), after every enum member.

### Word-boundary matching

`IndexOfToken` is an **ordinal, word-boundary** match (`IsCodeChar` = ASCII alphanumeric or `_`). A token only
matches when it is not glued to another code character on either side, so a substring never produces a false
positive — e.g. the DLZ *zone* label `DLZ: InZone` does **not** match the `DLZ_OUT` abort string
(`Dlz_zone_label_is_not_an_abort_reason`).

---

## Scoring — top-3 by `FireAbortReason` order, de-duplicated

For each token, `Appears` checks the explain fields (`ReasonCode`, `StatusLine`, `ReasonPlain`) first, then the ten
combat-detail lines. Matches feed `Consider`, which keeps the lowest-priority (= highest-precedence) `MaxReasons`
(`3`) via an insertion sort and **de-duplicates by display** so an enum name and its log code count once
(`Duplicate_enum_name_and_log_code_count_once`):

- The three kept reasons follow `FireAbortReason` order regardless of where they appear across the two sources
  (`Top_three_follow_FireAbortReason_order_not_field_order`).
- Once the three enum slots are full, equivalent (`≥ 1000`) strings only fill the **remaining** slots behind any
  `FireAbortReason` match (`Equivalent_abort_strings_fill_remaining_tooltip_slots`).

The result:

```csharp
public sealed record WeaponAbortTooltipState(
    string TooltipText,          // reasons joined by "\n"
    IReadOnlyList<string> Reasons,
    bool IsDenied);
```

`TooltipText` is the kept reasons joined by newlines; `IsDenied` is true only when at least one reason survived.

---

## Selection scope — `BindForSelection` / `ScopeCombatDetail` (DRG-352)

A single global combat-detail inspection key can point at **another** unit's engagement history. To stop that
leaking into the selected unit's weapon row, `BindForSelection` binds against detail scoped to the selected unit:

```csharp
public static WeaponAbortTooltipState BindForSelection(
    EngageExplain? explain, CombatPresentationFrame? frame,
    string? selectedUnitId, string? selectedContactId) =>
    Bind(explain, ScopeCombatDetail(frame, selectedUnitId, selectedContactId));
```

`ScopeCombatDetail` reverse-scans the captured frame's events for the selected unit's **own** most recent
engagement leg (`ShooterId == selectedUnitId`), narrowed to the selected contact's `TargetId` when a contact is
selected, and builds the detail card from that leg only. It reads the already-captured
`CombatPresentationFrame`; it never queries the sim. Load-bearing behaviours pinned by
`WeaponAbortTooltipSelectionScopeTests`:

- another unit's inspected history does not leak into the selected tooltip
  (`Inspected_history_key_for_another_unit_does_not_leak_into_selected_tooltip`);
- another unit's history cannot displace the selected unit's denial from the top three
  (`Other_unit_history_cannot_displace_selected_denial_from_top_three`);
- the selected unit's own history still contributes (`Selected_unit_own_history_still_contributes`);
- a selected contact narrows the detail to that engagement target
  (`Selected_contact_narrows_detail_to_current_engagement_target`);
- no frame or no selected unit → `CombatDetailPresentation.Empty`, so the result is explain-only
  (`No_selected_unit_or_frame_yields_explain_only`);
- a clear explain still suppresses the selected history (`Clear_explain_still_suppresses_selected_history`).

---

## Row mapping & applicability

`BindRows(state)` maps the one tooltip onto the two weapon rows the unit panel wires, keyed by their UXML element
names and attack-menu option ids:

| Constant | Value |
|----------|-------|
| `FireSingleRowName` / `FireSingleOptionId` | `weapon-row-fire-single` / `fire-single` |
| `FireSalvoRowName` / `FireSalvoOptionId` | `weapon-row-fire-salvo` / `fire-salvo` |
| `DeniedRowClass` | `unit-detail-weapon-row--denied` |

`AppliesTo(optionId)` is true **only** for `fire-single` / `fire-salvo`; hold-fire is not a weapon-abort row
(`Bind_rows_target_denied_weapon_rows_only`).

---

## The host — paint the tooltip, nothing else

[`RightUnitPanelHost`](../../unity/ProjectAegis/Assets/Scripts/Runtime/RightUnitPanelHost.cs) projects the engage
explain (`EngageExplainProjection.Project`), scopes the detail with `WeaponAbortTooltipBinder.ScopeCombatDetail`
over `bridgeHost.LastCombatFrame` / `SelectedContactId`, and binds with `WeaponAbortTooltipBinder.Bind(explain,
detail)` — memoized through [`RightUnitCombatDetailCache`](../../src/ProjectAegis.Delegation.UnityAdapter/Presentation/RightUnitCombatDetailCache.cs)
(DRG-407) so an unchanged preview/frame/unit/contact does not re-bind. (It calls `Bind` directly, not
`BindForSelection`, because it already holds the scoped detail; the host contract test asserts
`BindForSelection` is *not* used.)

When a fire row is disabled, `AppliesTo` matches, and the state `IsDenied`, the host copies `TooltipText` onto the
row and keeps the row `focusable` so the tooltip is reachable by **hover and keyboard focus** (a disabled button is
not pickable), and adds `DeniedRowClass`. The host contract is pinned by `WeaponAbortTooltipHostContractTests`: the
source contains **no** `IOrderSink` / `ApplyOrder`, and the
[`UnitDetailPanel.uxml`](../../unity/ProjectAegis/Assets/UI/UnitDetail/UnitDetailPanel.uxml) /
[`UnitDetailPanel.uss`](../../unity/ProjectAegis/Assets/UI/UnitDetail/UnitDetailPanel.uss) declare the focusable
weapon rows, the `.unit-detail-weapon-row--denied` and `.unit-detail-weapon-row:focus` classes, and the
`attack-option-cue--blocked` cue.

---

## Determinism & invariants

- **Advisory only.** The binder produces display strings; it issues no order, weapon recommendation, or engage
  card, and never re-evaluates the ROE/engage gate.
- **Fail-closed.** A clear (`ENGAGE: CLEAR`, `IsBlocked == false`) explain suppresses all reasons, including stale
  detail; the panel never shows a denial reason on a permitted launch.
- **Pure.** `Bind` / `ScopeCombatDetail` read only their arguments — no RNG, no wall clock, no `UnityEngine`. Same
  inputs → same state. `RightUnitCombatDetailCache` is a pure content-keyed memo (keyed on rendered content, not on
  frame clocks).
- **Capped & ordered.** At most `MaxReasons` (`3`) reasons, ordered by `FireAbortReason` value (then equivalents),
  de-duplicated by display; a log code and its enum name count once.
- **Boundary-safe matching.** Abort tokens match on word boundaries only, so a zone/label substring never
  manufactures a reason.
- **Selection-scoped.** The live unit panel reads the selected unit's own engagement leg, so another unit's
  inspected combat history cannot displace or leak into its denial (DRG-352).
- **Off the fingerprint.** Built from read-model DTOs; writes no log, is not part of the `SimWorldHash` or
  order-log fingerprint. The Baltic v2 hash `17144800277401907079` is untouched.
- **Off the Tick hot path.** The host binds in its UI refresh path, never on `DelegationBridge.Tick`.

---

## Tests (pins)

| Fixture | Covers |
|---------|--------|
| [`WeaponAbortTooltipBinderTests`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/WeaponAbortTooltipBinderTests.cs) | empty inputs, clear-explain suppression, blocked reason code, log-code→enum-name normalisation, top-3 `FireAbortReason` ordering, duplicate-name/log-code de-dup, equivalent-string slot fill, the DLZ-label non-match, and `BindRows` / `AppliesTo`. |
| [`WeaponAbortTooltipSelectionScopeTests`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/WeaponAbortTooltipSelectionScopeTests.cs) | DRG-352 selection scope — no cross-unit history leak, no displacement of the selected denial, own-history contribution, selected-contact narrowing, empty-scope explain-only, and clear-explain suppression. |
| [`WeaponAbortTooltipHostContractTests`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/WeaponAbortTooltipHostContractTests.cs) | the host wires `ScopeCombatDetail` + `Bind` (not `BindForSelection`) with no `IOrderSink`/`ApplyOrder`, and the UXML/USS declare the focusable denied rows + cue classes. |
| [`RightUnitCombatDetailCacheTests`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/RightUnitCombatDetailCacheTests.cs) | DRG-407 — the content-keyed memo returns a hit on unchanged inputs and misses when the rendered content changes. |

Part of the ≥1638-test baseline.

---

## See also

- [engage-explain-contract-projection.md](engage-explain-contract-projection.md) — the explain DTO this chrome reads (the write path it never touches).
- [abort-reason-catalog.md](abort-reason-catalog.md) — the canonical abort codes the token table recognises.
- [domain-abort-explain-chrome.md](domain-abort-explain-chrome.md) — the sibling domain-abort explain chrome over the same explain + combat-detail inputs.
- [engagement-pipeline.md](engagement-pipeline.md) — the engage-time gate that produces the `FireAbortReason` / catalog codes.
- [c2-projection-layer.md](c2-projection-layer.md) — general read-model / presentation-boundary rules this chrome follows.
- [c2-presentation-bridges.md](c2-presentation-bridges.md) — the host-feed pattern the unit-panel host uses.
