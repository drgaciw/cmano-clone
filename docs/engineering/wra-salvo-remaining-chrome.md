# WRA salvo-remaining chrome — the "how many rounds will doctrine let me fire?" weapon-panel row (DRG-259)

> **ADR:** this is the **presentation / read-model seam**
> ([`adr-010-headless-first-command-driven-ui.md`](../architecture/adr-010-headless-first-command-driven-ui.md) §2–3
> — chrome is a **client**, not sim authority), with
> [`adr-007-c2-map-presentation.md`](../architecture/adr-007-c2-map-presentation.md) (C2 presentation) and
> [`adr-001-sim-assembly-boundary.md`](../architecture/adr-001-sim-assembly-boundary.md) (the adapter boundary).

The **WRA salvo-remaining chrome** answers one question on the weapon panel — *how many rounds will the
weapons-release-authority (WRA) doctrine cap and my magazine actually let me put on this target right now?* — as a
single `WRA SALVO: <remaining>/<max>` line, with an abort code appended when the answer is zero. It folds
**already-known** magazine and policy facts (the employment ledger plus the resolved `EffectivePolicy.MaxSalvo`) and
never re-derives sim truth, evaluates the ROE/engage gate, or issues a fire order. Its `WRA_SALVO` output is the
same code the fire-commit [constraint strip](commit-constraint-strip-chrome.md) folds in as its `wra-salvo`
constraint source.

Unlike the tests-only strip, this binder **has a live Unity host**: it is projected per selection by
`DelegationBridgeHost` and painted onto the unit-detail panel's `wra-salvo-line` by `RightUnitPanelHost`.

| File | Role |
|------|------|
| [`WraSalvoRemainingBinder.cs`](../../src/ProjectAegis.Delegation.UnityAdapter/Presentation/WraSalvoRemainingBinder.cs) | the binder (`WraSalvoRemainingBinder.Bind`), `WraSalvoRemainingState`, the cue/declutter catalogs, `WraSalvoRemainingPanelBinder.BindRows` |
| [`DelegationBridgeHost.cs`](../../unity/ProjectAegis/Assets/Scripts/Runtime/DelegationBridgeHost.cs) | `ProjectSelectedWraSalvoRemaining()` — resolves the selected unit's policy + live engage context and calls `Bind` |
| [`RightUnitPanelHost.cs`](../../unity/ProjectAegis/Assets/Scripts/Runtime/RightUnitPanelHost.cs) | the UI-Toolkit host that stores `LastWraSalvoSurface` and paints `wra-salvo-line` |

It is **read-only**: it folds DTOs other projections already produced and emits one display row. It never calls
`IOrderSink` / `ApplyOrder` / `TryIssue`, touches `DelegationBridge` / `CatalogWriteGate` / `SimulationSession`, or
re-runs the engage gate, and it is **not** part of the Baltic v2 `SimWorldHash`.

---

## Inputs

```csharp
WraSalvoRemainingState Bind(
    string? shooterUnitId,
    in EngageContext engageContext,
    EffectivePolicy policy,
    EngagePreview? engagePreview = null);
```

- **`shooterUnitId`** — the selected shooter. A null/blank id **fails closed** to
  [`WraSalvoRemainingState.Empty`](#the-empty-placeholder) (`Missing_shooter_selection_fails_closed`).
- **`engageContext`** — the already-built [`EngageContext`](../../src/ProjectAegis.Sim/Engage/EngageContext.cs);
  only its `SalvoSize` (requested salvo) and `RoundsRemaining` (rounds in the magazine before this attempt) are read.
- **`policy`** — the resolved [`EffectivePolicy`](../../src/ProjectAegis.Sim/Policy/EffectivePolicy.cs); only
  `MaxSalvo` (the WRA doctrine cap) is read.
- **`engagePreview`** — optional already-projected
  [`EngagePreview`](../../src/ProjectAegis.Delegation/Projection/EngagePreviewProjection.cs) (`DlzLabel`, `CanFire`,
  `AbortPreviewCode`); used only as a defensive final fallback for the abort code (see below).

The requested salvo is normalized to `requestedSalvo = Math.Max(1, engageContext.SalvoSize)` and the cap to
`maxSalvo = Math.Max(0, policy.MaxSalvo)`.

---

## The resolve ladder — magazine first, then WRA cap

The binder asks the employment ledger first, then the doctrine cap. It builds a one-row
[`EmploymentLedgerProjection`](../../src/ProjectAegis.Delegation/EmploymentLedger/EmploymentLedgerProjection.cs)
(DRG-224) for the shooter against `CatalogWeaponIds.MvpDefault` with the magazine's `RoundsRemaining` and the
requested salvo, and reads `WithholdReason` off the first row. The ledger must stay advisory — a ledger that
reports `IsFireOrder` throws `InvalidOperationException`.

| Condition (first match wins) | `RemainingSalvo` | `AbortReasonCode` |
|------------------------------|------------------|-------------------|
| magazine **winchester** (`RoundsRemaining <= 0`) | `0` | `WINCHESTER_ORDNANCE` (ledger `WithholdReason`) |
| magazine **short** (`0 < RoundsRemaining < requestedSalvo`) | `0` | `NO_AMMO` (ledger `WithholdReason`) |
| WRA cap **blocks** (`maxSalvo <= 0` **or** `requestedSalvo > maxSalvo`) | `0` | `WRA_SALVO` (`AbortReasonCatalog.Doctrine.WRA_SALVO`) |
| **nominal** (within magazine and cap) | `Math.Min(requestedSalvo, maxSalvo)` | `null` |

So a 2-round salvo against an 8-round `MaxSalvo` with 8 in the magazine → `WRA SALVO: 2/8`, `RemainingSalvo = 2`,
no abort (`Nominal_salvo_within_wra_cap_shows_remaining_over_max`); the same salvo against `MaxSalvo: 2` with a
requested 4 → `0/2` + `WRA_SALVO` (`Salvo_exceeding_wra_cap_is_exhausted_with_wra_salvo_abort`); 0 rounds →
`WINCHESTER_ORDNANCE` (`Winchester_magazine_withhold_is_exhausted_with_ordnance_abort`); 1 round under a 2-round
salvo → `NO_AMMO` (`Below_salvo_rounds_use_employment_no_ammo_abort`); `MaxSalvo: 0` → `WRA_SALVO`
(`Zero_max_salvo_is_exhausted_with_abort`).

> **Defensive preview fallback.** After the ladder, `if (remaining <= 0 && abort == null &&
> engagePreview?.AbortPreviewCode != null)` the preview code fills the abort. Under the current ladder every
> `remaining == 0` branch already sets a code, so this is a belt-and-suspenders guard: a future zero-remaining path
> that forgot to name a reason still inherits the engage preview's code rather than showing a blank abort.

---

## `WraSalvoRemainingState`

```csharp
public sealed record WraSalvoRemainingState(
    string StateLine, int RemainingSalvo, int MaxSalvo,
    string? AbortReasonCode, bool IsExhausted,
    string DeclutterToken, string CueClass, bool IsFireOrder);
```

- **`StateLine`** is `"WRA SALVO: <remaining>/<max>"` (invariant culture), with ` | <code>` appended **only when
  exhausted and a code is present** (e.g. `WRA SALVO: 0/2 | WRA_SALVO`).
- **`IsExhausted`** is `remaining <= 0` — the single "can't fire the requested salvo" flag hosts key off.
- **`IsFireOrder`** is hard-wired `false`.

### Cue classes & declutter tokens (never color-only)

| State | `CueClass` | `DeclutterToken` |
|-------|-----------|------------------|
| nominal (can fire) | `wra-salvo-cue--nominal` | `[WRA-SALVO:READY]` |
| exhausted (zero remaining) | `wra-salvo-cue--exhausted` | `[WRA-SALVO:EXHAUSTED]` |
| cleared / unknown | `wra-salvo-cue--unknown` | `[WRA-SALVO:UNKNOWN]` |

The declutter token is paired with the cue so a host never signals state by color alone. The token is
`EXHAUSTED` when exhausted, otherwise `READY` (the `UNKNOWN` token is reserved for the `maxSalvo <= 0` case, which
in practice is already exhausted, so a live bind lands on `READY`/`EXHAUSTED`; `Empty` carries the `--unknown` cue).
`WraSalvoCueClasses.All` is the list a host clears before applying the active cue.

### The `Empty` placeholder

`WraSalvoRemainingState.Empty` is the cleared row for no/blank selection: `"WRA SALVO: —"`, zero remaining/max,
`null` abort, `IsExhausted = true`, an **empty** declutter token, and the `wra-salvo-cue--unknown` cue.

### `BindRows` — the host paint contract

`WraSalvoRemainingPanelBinder.BindRows(state)` maps the state onto exactly one
`WraSalvoRemainingRow(ElementName, Text, CueClass)`:

| `ElementName` | Text | CueClass |
|---------------|------|----------|
| `wra-salvo-line` | `StateLine` | `state.CueClass` |

(`Panel_binder_maps_wra_salvo_line_element`.)

---

## The live host path

Unlike the tests-only fire-commit strip, this chrome is wired end-to-end:

1. **`DelegationBridgeHost.ProjectSelectedWraSalvoRemaining()`** fails closed to `Empty` when there is no snapshot or
   no selection. Otherwise it resolves the selected unit's policy via
   `ScenarioPolicy?.ResolveUnitPolicy(SelectedUnitId, isFriendly: true).Effective ?? EffectivePolicy.DefaultFree`,
   builds the live engage context with `BuildSelectedLiveEngageContext` (a read-only mirror of
   `DelegationBridge.GetEngagePreviewForUnit` inputs — `ScenarioEngageDefaults.ToEngageContext(DefaultMagazineRounds)`
   overlaid with `HasFireControlTrack`, `RadarEmconActive`, `AirOperationsReady`), and calls
   `WraSalvoRemainingBinder.Bind(SelectedUnitId, in ctx, policy, ProjectSelectedEngagePreview())`.
2. **`RightUnitPanelHost`** stores the result as `LastWraSalvoSurface` (`_wraSalvoSurface`) and reads
   `_wraSalvoSurface.IsExhausted` when refreshing the attack-menu buttons, then paints the labels.
3. The **`wra-salvo-line`** element is declared in
   [`UnitDetailPanel.uxml`](../../unity/ProjectAegis/Assets/UI/UnitDetail/UnitDetailPanel.uxml).

The host projects — it never sets a fact, touches a controller slot, or calls `DelegationBridge.Tick`.

---

## Determinism & invariants

- **Advisory, off the fingerprint.** The binder folds already-known magazine + policy facts; it never writes the
  sim log, re-evaluates a gate, or participates in the `SimWorldHash` / order-log fingerprint. The Baltic v2 hash
  `17144800277401907079` is untouched.
- **Fails loud if the ledger isn't advisory.** A ledger reporting `IsFireOrder` throws — the chrome refuses to
  present a fact that claims to be an order.
- **Fails closed.** No/blank selection → `Empty`; zero remaining always carries a reason code when one is known.
- **One reason, shared code.** `WRA_SALVO` here is the same `AbortReasonCatalog.Doctrine.WRA_SALVO` the constraint
  strip and abort catalog use, so the weapon panel and the fire-commit strip never disagree.
- **Never color-only.** Every cue class is paired with a text declutter token and the `StateLine` carries the
  numbers and code in text.
- **Deterministic.** Identical inputs produce identical lines; no RNG, no wall clock.

---

## Tests (pins)

NUnit, part of the ≥1638-test baseline:

- [`WraSalvoRemainingBinderTests`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/WraSalvoRemainingBinderTests.cs)
  — **7** methods / 9 cases (nominal remaining/max, WRA-cap exhaustion + `WRA_SALVO`, winchester →
  `WINCHESTER_ORDNANCE`, short magazine → `NO_AMMO`, null/empty/blank shooter → `Empty`, zero `MaxSalvo` →
  `WRA_SALVO`, `BindRows` → `wra-salvo-line`).
- [`WraSalvoRemainingHostContractTests`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/WraSalvoRemainingHostContractTests.cs)
  — **3** methods asserting the live wiring (`RightUnitPanelHost` binds via `ProjectSelectedWraSalvoRemaining` /
  `WraSalvoRemainingPanelBinder.BindRows` / `LastWraSalvoSurface`, `DelegationBridgeHost` exposes
  `ProjectSelectedWraSalvoRemaining` over `WraSalvoRemainingBinder.Bind` + `BuildSelectedLiveEngageContext`, and
  `UnitDetailPanel.uxml` declares `wra-salvo-line`).

---

## See also

- [commit-constraint-strip-chrome.md](commit-constraint-strip-chrome.md) — the S124 fire-commit strip that folds
  this binder's `WRA_SALVO` output as its lowest-but-one `wra-salvo` constraint source.
- [weapon-abort-tooltip-chrome.md](weapon-abort-tooltip-chrome.md) — the sibling weapon-panel chrome (DRG-258) that
  explains *why* a weapon is denied, over the same `RightUnitPanelHost`.
- [abort-reason-catalog.md](abort-reason-catalog.md) — the `WRA_SALVO` / `NO_AMMO` / `WINCHESTER_ORDNANCE` codes
  this row surfaces.
- [engagement-pipeline.md](engagement-pipeline.md) / [autonomy-roe-gating.md](autonomy-roe-gating.md) — the
  engage-time gate and the two-layer ROE/WRA `MaxSalvo` split that this row mirrors.
- [c2-projection-layer.md](c2-projection-layer.md) — general read-model / presentation-boundary rules;
  [c2-presentation-bridges.md](c2-presentation-bridges.md) — the host-feed pattern this binder uses.
