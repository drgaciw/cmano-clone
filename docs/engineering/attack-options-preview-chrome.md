# Attack-options preview chrome — the "which fire options can I pick right now?" weapon-panel menu (DRG-262)

> **ADR:** this is the **presentation / read-model seam**
> ([`adr-010-headless-first-command-driven-ui.md`](../architecture/adr-010-headless-first-command-driven-ui.md) §2–3
> — chrome is a **client**, not sim authority), with
> [`adr-007-c2-map-presentation.md`](../architecture/adr-007-c2-map-presentation.md) (C2 presentation) and
> [`adr-001-sim-assembly-boundary.md`](../architecture/adr-001-sim-assembly-boundary.md) (the adapter boundary).

The **attack-options preview chrome** turns the already-projected engage-options menu into the weapon panel's
`attack-options-line` summary plus one display row per fire option — *which attack options exist, which are
enabled, and (when disabled) the abort code the command façade would return.* It folds the
[`EngageAttackOptions`](../../src/ProjectAegis.Delegation/Projection/EngageAttackOptions.cs) rows a projection
already built (`fire-single` / `fire-salvo` / `hold-fire` for the MVP menu, but the binder is menu-agnostic). It
never re-evaluates the engage/ROE gate, re-derives `CanFire`, consults a second fact source, or issues an order —
the `Enabled` flag and `DisabledReason` are produced upstream by
[`EngagePreviewProjection`](../../src/ProjectAegis.Delegation/Projection/EngagePreviewProjection.cs) via
`EngageAttackOptions.Build`; this binder only **presents** them.

| File | Role |
|------|------|
| [`AttackOptionsPreviewBinder.cs`](../../src/ProjectAegis.Delegation.UnityAdapter/Presentation/AttackOptionsPreviewBinder.cs) | `Bind`, `FindRow`, `AttackOptionsPreviewState`, `AttackOptionMenuRow`, `AttackOptionsCueClasses`, the per-thread menu cache |
| [`EngageAttackOptions.cs`](../../src/ProjectAegis.Delegation/Projection/EngageAttackOptions.cs) | the engine-agnostic `AttackOption(Id, Label, Enabled, DisabledReason?)` menu this binder folds |
| [`AttackMenuPanelBinder.cs`](../../src/ProjectAegis.Delegation/Projection/AttackMenuPanelBinder.cs) | `ResolveButtonName(optionId)` — the stable `optionId → UXML button name` map the host already wires |
| [`RightUnitPanelHost.cs`](../../unity/ProjectAegis/Assets/Scripts/Runtime/RightUnitPanelHost.cs) | the UI-Toolkit host that stores `LastAttackOptionsPreview`, paints `attack-options-line`, and gates the attack buttons |

It is **read-only**: it folds a DTO another projection already produced and emits a preview line plus button rows.
It never calls `IOrderSink` / `ApplyOrder` / `TryIssue`, touches `DelegationBridge` / `CatalogWriteGate` /
`SimulationSession`, or re-runs the engage gate, and it is **not** part of the Baltic v2 `SimWorldHash`.

---

## Input — one menu, fail closed

```csharp
AttackOptionsPreviewState Bind(IReadOnlyList<EngageAttackOptions.AttackOption>? menu);
```

- A **null or empty** menu fails closed to [`AttackOptionsPreviewState.Empty`](#the-empty-placeholder).
- Otherwise each `AttackOption` is folded into one `AttackOptionMenuRow`, in menu order (no sort — the projection's
  order is the display order).

The MVP menu is `EngageAttackOptions.Build(in ctx, preview)`:

| Option | `Id` | Enabled when | `DisabledReason` when blocked |
|--------|------|--------------|-------------------------------|
| Fire 1 round | `fire-single` | `preview.CanFire && ctx.RoundsRemaining > 0` | `preview.AbortPreviewCode` |
| Fire salvo (`n`) | `fire-salvo` | `preview.CanFire && ctx.RoundsRemaining >= salvo` | `NO_AMMO` when short, else `AbortPreviewCode` |
| Hold fire | `hold-fire` | always `true` | — |

The binder does **not** depend on these three ids — it folds whatever rows the menu carries (an unknown id just gets
a `null` `ButtonName`; see below).

---

## `Build` — the row and line format

For each `AttackOption`:

- **`AbortReason`** = `null` when `Enabled`; otherwise `DisabledReason ?? "BLOCKED"` (the
  [`MissingAbortToken`](../../src/ProjectAegis.Delegation.UnityAdapter/Presentation/AttackOptionsPreviewBinder.cs),
  which matches the `EngageAttackOrderResolver` code so the menu shows the same code the command façade returns).
- **`ButtonText`** = `Label` when enabled, else `"<Label> (<abort>)"`.
- **`ButtonName`** = `AttackMenuPanelBinder.ResolveButtonName(Id)` — `attack-fire-single` / `attack-fire-salvo` /
  `attack-hold-fire`, or **`null`** for any other id (the row still shows on the preview line, it just has no button
  to wire).
- **`CueClass`** = `attack-option-cue--ready` when enabled, else `attack-option-cue--blocked`.

The rolled-up state:

- **`PreviewLine`** = `"ATTACK: "` + each row's `ButtonText` joined by `" | "`, e.g.
  `ATTACK: Fire 1 round | Fire salvo (2) (NO_AMMO) | Hold fire`.
- **`CueClass`** = `attack-option-cue--blocked` when **any** row is blocked, else `attack-option-cue--ready`.
- **`Fingerprint`** = `"atk:"` + each row joined by `";"` as `"<id>:<1|0>:<abort|->"`, e.g.
  `atk:fire-single:0:DLZ_OUT;hold-fire:1:-`.

---

## `AttackOptionsPreviewState`

```csharp
public sealed record AttackOptionsPreviewState(
    string PreviewLine,
    IReadOnlyList<AttackOptionMenuRow> Rows,
    string CueClass,
    string Fingerprint);

public sealed record AttackOptionMenuRow(
    string OptionId, string? ButtonName, string Label,
    bool Enabled, string? AbortReason, string ButtonText, string CueClass);
```

- **`Rows`** is an `Array.AsReadOnly` wrapper — a host that casts it to `IList<AttackOptionMenuRow>` and tries to
  write a slot gets a `NotSupportedException`, so the cached preview can't be mutated through a cast.
- **`FindRow(state, optionId)`** returns the ordinal-matched row or `null` (how the host looks up the row for a
  given button id). It throws `ArgumentNullException` on a null `state`.

### Cue classes (never color-only)

| Row | `CueClass` |
|-----|------------|
| enabled | `attack-option-cue--ready` |
| disabled | `attack-option-cue--blocked` |
| empty menu | `attack-option-cue--empty` |

`AttackOptionsCueClasses.All` is the list a host clears before applying the active cue. The state is **also** carried
in text — the `(<abort>)` suffix on a blocked button and the abort code on the preview line — so DLZ/ROE state is
never signalled by colour alone.

### The `Empty` placeholder

`AttackOptionsPreviewState.Empty` is the cleared menu for a null/empty projection: `PreviewLine` `"ATTACK: —"`, no
rows, the `attack-option-cue--empty` cue, and the `"atk:empty"` fingerprint. `Bind(null)` and
`Bind(Array.Empty<…>())` both return this **same singleton instance**.

---

## The per-frame cache

`Bind` keeps a `[ThreadStatic]` `_lastBound` snapshot of the last menu + its state. When the next menu matches
(same count, each element `ReferenceEquals` **or** value-`Equals` the cached one) it returns the **previously bound
instance** and allocates nothing — so a per-frame host `Refresh()` over an unchanged menu does zero work. A changed
menu (including a caller that mutates its list after binding) rebuilds, because `Bind` snapshots the menu into its
own array rather than holding the caller's list. The cache is per-thread; Unity hosts bind on the main thread.

---

## The live host path

The binder is wired on the selected unit's weapon panel ([`RightUnitPanelHost`](../../unity/ProjectAegis/Assets/Scripts/Runtime/RightUnitPanelHost.cs)),
all off the `DelegationBridge.Tick` hot path:

1. Both `ApplyPanelState(state)` and the bridge-driven `Refresh()` resolve the menu off the already-bound
   `UnitDetailPanelState.AttackMenu` and call `AttackOptionsPreviewBinder.Bind(state.AttackMenu)`, storing the result
   as `LastAttackOptionsPreview`.
2. `RefreshAttackMenuButtons()` walks the wired attack buttons, looks each one up with `FindRow`, **hides** a button
   with no matching row, and otherwise sets its text / tooltip / enabled state from the row and applies the row's
   cue class. It overlays two already-projected facts on top of the row: the DRG-259
   [WRA salvo-exhausted](wra-salvo-remaining-chrome.md) block on `fire-salvo`, and the DRG-258
   [weapon-abort tooltip](weapon-abort-tooltip-chrome.md) on a denied fire row.
3. Clicking a button routes to `bridgeHost.TrySelectAttackOption(optionId, …)` — **that** is the command path. The
   preview itself issues nothing; it only decides what the buttons show and whether they are enabled.
4. The `attack-options-line` label and the `attack-fire-single` / `attack-fire-salvo` / `attack-hold-fire` buttons
   are declared in [`UnitDetailPanel.uxml`](../../unity/ProjectAegis/Assets/UI/UnitDetail/UnitDetailPanel.uxml); the
   `attack-option-cue--*` classes live in `UnitDetailPanel.uss`.

---

## Determinism & invariants

- **Advisory, off the fingerprint.** The binder folds already-projected menu rows; it never writes the sim log,
  re-evaluates a gate, or participates in the `SimWorldHash` / order-log fingerprint. The Baltic v2 hash
  `17144800277401907079` is untouched.
- **Fails closed.** A null/empty menu → `Empty` (`"ATTACK: —"`), never a guessed or blank menu. A disabled row with
  no `DisabledReason` shows the explicit `BLOCKED` token rather than an empty reason.
- **Mirrors the façade.** The disabled-row token matches the `EngageAttackOrderResolver` code, so the menu shows the
  same code the command path returns — the chrome never invents a friendlier reason.
- **Never color-only.** Every cue class is paired with the abort code in the button text / preview line.
- **Deterministic & allocation-aware.** Identical menus produce identical rows and fingerprints
  (`Bind_is_replay_stable_for_the_same_menu`); an unchanged rebind returns the cached instance and allocates nothing
  (`Unchanged_menu_rebind_allocates_nothing`). No RNG, no wall clock.

---

## Tests (pins)

NUnit, part of the ≥1638-test baseline:

- [`AttackOptionsPreviewBinderTests`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/AttackOptionsPreviewBinderTests.cs)
  — **12** methods: menu parity with the engage projection, disabled-reason on preview + button, the `BLOCKED`
  fallback, replay-stable fingerprint, null/empty fail-closed, an unknown id with no button name, the cached-instance
  / zero-allocation rebind, a rebuild on changed contents, caller-mutation safety, the read-only `Rows` cast guard,
  and the all-enabled ready cue.
- [`AttackOptionsPreviewHostContractTests`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/AttackOptionsPreviewHostContractTests.cs)
  — **2** methods asserting the live wiring (`RightUnitPanelHost` binds via `AttackOptionsPreviewBinder.Bind` /
  `AttackOptionsCueClasses` / `TrySelectAttackOption` / `RefreshAttackMenuButtons` with **no** `IOrderSink` /
  `ApplyOrder`, and `UnitDetailPanel.uxml` / `.uss` declare the buttons and `attack-option-cue--*` classes).

---

## See also

- [commit-constraint-strip-chrome.md](commit-constraint-strip-chrome.md) — the S124 fire-commit strip that mirrors
  this menu's row `Enabled` / `AbortReason` into a commit-readiness + cost line.
- [wra-salvo-remaining-chrome.md](wra-salvo-remaining-chrome.md) — the WRA salvo cap the host overlays on the
  `fire-salvo` button.
- [weapon-abort-tooltip-chrome.md](weapon-abort-tooltip-chrome.md) — the denied-weapon tooltip the host overlays on a
  blocked fire row.
- [dlz-live-surface-chrome.md](dlz-live-surface-chrome.md) — the sibling weapon-panel chrome over the same
  `EngagePreview`.
- [engagement-pipeline.md](engagement-pipeline.md) — the engage-time gate that produces `CanFire` /
  `AbortPreviewCode`; [abort-reason-catalog.md](abort-reason-catalog.md) — the abort codes a blocked row carries.
- [c2-projection-layer.md](c2-projection-layer.md) — general read-model / presentation-boundary rules;
  [c2-presentation-bridges.md](c2-presentation-bridges.md) — the host-feed pattern this binder uses.
