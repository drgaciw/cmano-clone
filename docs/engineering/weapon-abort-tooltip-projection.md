# Weapon-abort tooltip — the denied-weapon-row hover surface (DRG-258 / DRG-352)

> **ADR:** this is the **presentation / read-model seam**
> ([`adr-010-headless-first-command-driven-ui.md`](../architecture/adr-010-headless-first-command-driven-ui.md) §2–3
> — a binder is a **client**, not sim authority), with
> [`adr-007-c2-map-presentation.md`](../architecture/adr-007-c2-map-presentation.md) (C2 presentation) and
> [`adr-001-sim-assembly-boundary.md`](../architecture/adr-001-sim-assembly-boundary.md) (the adapter boundary).
> Do **not** cite Git ADR-018 (that is sensor side-picture / datalink). Combat UX; the binder is DRG-258 /
> REQ-13 ROE-07, selection-scoped by DRG-352.

The **weapon-abort tooltip** answers one question for the unit-detail panel: *"this fire button is denied —
why?"* It folds the already-projected refusal facts for the selected unit into a short, ranked, de-duplicated
list of blocking rules and hands the host one tooltip string to drop onto each denied weapon row. It lives in a
single file under
[`ProjectAegis.Delegation.UnityAdapter/Presentation/`](../../src/ProjectAegis.Delegation.UnityAdapter/Presentation/WeaponAbortTooltipBinder.cs):

1. `WeaponAbortTooltipBinder` — the pure static fold (`Bind`, `BindForSelection`, `ScopeCombatDetail`, `BindRows`).
2. `WeaponAbortTooltipState` — the immutable result (`TooltipText`, `Reasons`, `IsDenied`) + its `Empty` singleton.
3. `WeaponAbortTooltipRow` — one per-row binding (`ElementName`, `OptionId`, `TooltipText`, `IsDenied`).

It is strictly a **read-model**: it reads only the [engage-explain](engage-explain-contract-projection.md) DTO
and a [combat-detail](slice-a-contact-detail-surface.md) card that other projections already produced. It issues
no orders, recomputes no engagement, queries no sim, and is **not** part of the Baltic v2 `SimWorldHash`. It
never invents a reason — if nothing is blocking, the result is `WeaponAbortTooltipState.Empty`.

---

## Inputs — an explain DTO + an optional combat-detail card

```csharp
WeaponAbortTooltipState Bind(EngageExplain? explain, CombatDetailPresentation? detail = null)
```

| Input | Role |
|-------|------|
| `EngageExplain?` | The live engage preview for the current selection ([`EngageExplainProjection.Project`](../../src/ProjectAegis.Delegation/Projection/EngageExplainProjection.cs)): `StatusLine`, `ReasonCode`, `ReasonPlain`, `IsBlocked`. |
| `CombatDetailPresentation?` | The selected unit's most recent engagement card — its ten text lines (`StatusLine`, `WeaponLine`, `HardConstraintsLine`, `PolicyLine`, `ConfidenceLine`, `FiringSolutionLine`, `NextActionLine`, `BdaLine`, `PostureLine`, `CorrelationLine`). Optional historical context. |

### Source gating (what is read, and what suppresses it)

- **A clear (permitted) explain wins and suppresses everything.** If `explain.IsBlocked == false` **and**
  `explain.StatusLine == EngageExplainProjection.CanFireLabel` (`"ENGAGE: CLEAR"`), the result is `Empty` even
  when the combat-detail card still carries a historical refusal (`Clear_engage_explain_suppresses_historical_detail_reasons`).
  This is the *fire is permitted now, don't show a stale deny* rule.
- The **explain** is read only when `IsBlocked == true`.
- The **detail** is read only when it is non-null and not `CombatDetailPresentation.Empty`.
- If neither source is live, the result is `Empty`.

---

## The fold — ranking the top three

### 1. Token table

At class load, `BuildTokens` constructs one match table (built once, reused):

- **Every `FireAbortReason`** (except `None`): the PascalCase enum name *and* its `SCREAMING_SNAKE` log code
  (`ToLogCode`) both map to the enum-name display at **priority `(int)reason`** (`1…16`). So `RoeHoldFire` and
  `ROE_HOLD_FIRE` are the same reason at the same rank.
- A fixed alias: the doctrine catalog code `ROE_WEAPONS_TIGHT` → display `WeaponsTight`.
- The **equivalent abort-catalog codes** ([`AbortReasonCatalog`](../../src/ProjectAegis.Sim/Glossary/AbortReasonCatalog.Generated.cs)
  `Engage` / `Logistics` / `Sensor` / `Cyber` groups: `MOUNT_OFFLINE`, `DLZ_OUT`, `NO_AMMO`, `SENSOR_OFFLINE`,
  `CYBER_LINK_DEGRADED`, …): each kept verbatim at **priority `1000 + arrayIndex`**.

Because named `FireAbortReason` reasons sit at `1…16` and catalog equivalents at `1000+`, a named reason always
outranks a catalog code, and the array order breaks ties among catalog codes.

### 2. Appearance test (word-boundary)

A token `Appears` when it is found — as a whole token — in any read source: for the explain, its `ReasonCode` /
`StatusLine` / `ReasonPlain`; for the detail, any of its ten lines. Matching is **word-boundary** (`IndexOfToken`):
a hit must not be flanked by `[0-9A-Za-z_]`, so a `DLZ:` zone label does **not** match the `DLZ_OUT` abort code
(`Dlz_zone_label_is_not_an_abort_reason`), and `DLZ_OUT` will not match inside `DLZ_OUTER`.

### 3. Top-three selection (ranked, de-duplicated)

`Consider` keeps the lowest-priority (highest-rank) `MaxReasons` (**3**) distinct displays, insertion-sorted
ascending by priority. Duplicate displays — e.g. the enum name and its log code both present — collapse to one
entry (`Duplicate_enum_name_and_log_code_count_once`). The resulting `Reasons` therefore follow
`FireAbortReason` order, **not** the order the fields were scanned
(`Top_three_follow_FireAbortReason_order_not_field_order`), and catalog equivalents fill only the slots the named
reasons leave open (`Equivalent_abort_strings_fill_remaining_tooltip_slots`).

### 4. Result

If no token appears, `Empty`. Otherwise `TooltipText = string.Join("\n", Reasons)` and `IsDenied = true`.

---

## Selection scoping — `BindForSelection` / `ScopeCombatDetail` (DRG-352)

The *global* combat-history inspection key can point at **another unit's** engagement. Feeding that card into the
tooltip would attribute the wrong unit's denial to the selected unit. `ScopeCombatDetail` fixes this by rebuilding
the combat-detail card from the captured `CombatPresentationFrame` scoped to the selected unit's **own** latest
engagement leg:

```csharp
CombatDetailPresentation ScopeCombatDetail(CombatPresentationFrame? frame, string? selectedUnitId, string? selectedContactId)
WeaponAbortTooltipState   BindForSelection(EngageExplain? explain, CombatPresentationFrame? frame, string? selectedUnitId, string? selectedContactId)
```

- No frame or no selected unit → `CombatDetailPresentation.Empty` (so the tooltip falls back to explain only).
- A selected contact narrows the leg to that contact's `TargetId`; otherwise the selected unit's most recent leg
  (walked from the end of the event list) is used.
- It reads only the already-captured frame — it never queries the sim, and it ignores the global combat-history
  inspection key, so another unit's history cannot leak into or displace the selected unit's denial
  (`Inspected_history_key_for_another_unit_does_not_leak_into_selected_tooltip`,
  `Other_unit_history_cannot_displace_selected_denial_from_top_three`).

`BindForSelection` is the convenience that composes `ScopeCombatDetail` + `Bind`; the live unit panel inlines the
two calls (see the host below).

---

## Mapping onto rows — `BindRows` / `AppliesTo`

```csharp
IReadOnlyList<WeaponAbortTooltipRow> BindRows(WeaponAbortTooltipState state)
bool AppliesTo(string? optionId)
```

`BindRows` returns two bindings — one for the single-round row, one for the salvo row — each carrying the shared
`TooltipText` and `IsDenied` (`Bind_rows_target_denied_weapon_rows_only`; throws `ArgumentNullException` on a null
state). `AppliesTo` is `true` only for the two **fire** option ids; **hold-fire is not a weapon-abort row**.

### Stable element contract

| Constant | Value |
|----------|-------|
| `MaxReasons` | `3` |
| `FireSingleRowName` / `FireSalvoRowName` | `weapon-row-fire-single` / `weapon-row-fire-salvo` |
| `FireSingleOptionId` / `FireSalvoOptionId` | `fire-single` / `fire-salvo` |
| `DeniedRowClass` | `unit-detail-weapon-row--denied` |

---

## Consumer

The live wiring is the Unity
[`RightUnitPanelHost`](../../unity/ProjectAegis/Assets/Scripts/Runtime/RightUnitPanelHost.cs). Per the host
contract (`WeaponAbortTooltipHostContractTests`), in the post-Tick C2 refresh — **never** on the
`DelegationBridge.Tick` hot path — the host:

1. projects the live preview with `EngageExplainProjection.Project(engagePreview)`,
2. scopes the card with `WeaponAbortTooltipBinder.ScopeCombatDetail(bridgeHost.LastCombatFrame, selectedUnitId, bridgeHost.SelectedContactId)`,
3. folds both with `WeaponAbortTooltipBinder.Bind(explain, detail)`,
4. maps the result through `AttackOptionsPreviewBinder` + `RefreshAttackMenuButtons`, adding/removing
   `DeniedRowClass` on each weapon row (`AppliesTo`-gated), and copies `TooltipText` onto the row.

The host touches **no** `IOrderSink` / `ApplyOrder` — it only renders. The focusable weapon rows, the
`unit-detail-weapon-row--denied` / `:focus` styles, and the `attack-option-cue--blocked` cue live in
[`UnitDetailPanel.uxml`](../../unity/ProjectAegis/Assets/UI/UnitDetail/UnitDetailPanel.uxml) /
[`.uss`](../../unity/ProjectAegis/Assets/UI/UnitDetail/UnitDetailPanel.uss).

---

## Determinism & invariants

- **Read-model, off the fingerprint.** Built from already-projected DTOs; writes no log, is not part of the
  `SimWorldHash` or order-log fingerprint. The Baltic v2 hash `17144800277401907079` is untouched.
- **Never fabricates a reason.** Nothing blocking → `Empty`. A clear/permitted explain suppresses stale history.
- **Ranked, capped, de-duplicated.** At most three distinct reasons, ordered by `FireAbortReason` severity, with
  the enum name and its log code counted once.
- **Word-boundary matching.** A substring that is part of a larger code (a `DLZ:` label vs `DLZ_OUT`) does not
  match.
- **Selection fail-closed (DRG-352).** The tooltip reflects the selected unit's own engagement; another unit's
  inspected history can neither leak in nor displace the selected unit's denial.
- **Fire-only rows.** `hold-fire` is never a weapon-abort row.
- **Pure.** `Bind` / `BindForSelection` / `ScopeCombatDetail` / `BindRows` touch only their arguments and the
  one-time token table — no RNG, no wall clock, no `UnityEngine` in the binder.

---

## Tests (pins)

Across
[`WeaponAbortTooltipBinderTests`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/WeaponAbortTooltipBinderTests.cs),
[`WeaponAbortTooltipSelectionScopeTests`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/WeaponAbortTooltipSelectionScopeTests.cs),
and
[`WeaponAbortTooltipHostContractTests`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/WeaponAbortTooltipHostContractTests.cs)
(NUnit, **17** tests). Highlights:

| Test | Covers |
|------|--------|
| `Null_explain_and_detail_yield_empty_tooltip` | both sources missing → the `Empty` singleton. |
| `Clear_engage_explain_suppresses_historical_detail_reasons` | a `CanFire` explain suppresses a stale detail denial. |
| `Blocked_explain_reason_code_is_the_tooltip` | a single blocked reason-code becomes the tooltip verbatim. |
| `Catalog_log_code_maps_onto_FireAbortReason_name` | `ROE_WEAPONS_TIGHT` → `WeaponsTight`. |
| `Top_three_follow_FireAbortReason_order_not_field_order` | three reasons ranked by enum severity, not scan order. |
| `Duplicate_enum_name_and_log_code_count_once` | enum name + its log code collapse to one entry. |
| `Equivalent_abort_strings_fill_remaining_tooltip_slots` | catalog equivalents fill slots the named reasons leave open. |
| `Dlz_zone_label_is_not_an_abort_reason` | word-boundary: a `DLZ:` label is not the `DLZ_OUT` code. |
| `Bind_rows_target_denied_weapon_rows_only` | the two fire-row bindings; `AppliesTo` excludes `hold-fire`. |
| `Inspected_history_key_for_another_unit_does_not_leak_into_selected_tooltip` | DRG-352 scoping vs the global key. |
| `Other_unit_history_cannot_displace_selected_denial_from_top_three` | another unit's history can't crowd out the selected denial. |
| `Selected_unit_own_history_still_contributes` / `Selected_contact_narrows_detail_to_current_engagement_target` | the selected unit's own leg is read; a selected contact narrows the target. |
| `No_selected_unit_or_frame_yields_explain_only` / `Clear_explain_still_suppresses_selected_history` | scoping fallbacks and the clear-explain suppression on the selection path. |
| `Right_unit_panel_binds_weapon_abort_tooltip_from_explain_and_combat_detail` / `Unit_detail_layout_declares_focusable_weapon_rows` | the host wiring + the UXML/USS element/style contract. |

Part of the ≥1638-test baseline.

---

## See also

- [engage-explain-contract-projection.md](engage-explain-contract-projection.md) — the `EngageExplain` / engage-explain contract this binder reads.
- [engagement-explanation-projection.md](engagement-explanation-projection.md) — the deeper *why permitted / refused + remedy* card for a selected engage-assess leg.
- [threat-assessment-projection.md](threat-assessment-projection.md) — the feasibility / weapon-recommendation ladder that classifies the same `FireAbortReason` family.
- [abort-reason-catalog.md](abort-reason-catalog.md) — the stable `ENGAGE_ABORT` / `POLICY_DENIAL` codes this binder matches on.
- [slice-a-contact-detail-surface.md](slice-a-contact-detail-surface.md) — the combat-detail card the tooltip reads for historical context.
- [c2-projection-layer.md](c2-projection-layer.md) — general read-model / presentation-boundary rules; [c2-presentation-bridges.md](c2-presentation-bridges.md) — the host-feed pattern the unit panel uses.
