# Air Ops ready-aggregate chrome — the "how many aircraft are ready to launch?" panel header cue (DRG-267)

> **ADR:** this is the **presentation / read-model seam**
> ([`adr-010-headless-first-command-driven-ui.md`](../architecture/adr-010-headless-first-command-driven-ui.md) §2–3
> — chrome is a **client**, not sim authority), with
> [`adr-007-c2-map-presentation.md`](../architecture/adr-007-c2-map-presentation.md) (C2 presentation) and
> [`adr-001-sim-assembly-boundary.md`](../architecture/adr-001-sim-assembly-boundary.md) (the adapter boundary).

The **Air Ops ready-aggregate chrome** turns an already-projected air-ops readiness roll-up into a non-colour-only
header cue on the existing Air Ops panel — *how many aircraft are ready to launch, and is that none / some / all?*
It only **reads the aggregate that upstream already produced**:
[`AirOpsApplyState.Apply`](../../src/ProjectAegis.Delegation/Projection/AirOpsApplyState.cs) folds the air-ops FSM /
readiness rows into an `AirOpsPresentation` whose `Aggregate` already carries the `ReadyCount` / `TotalCount` and the
`READY <ready>/<total>` summary line. This binder never re-walks the readiness map, re-derives the FSM phase, or
counts launch-ready aircraft itself — it only **classifies and presents** the band (`all` / `partial` / `none` /
`unknown`) the [logistics-ops runtime](logistics-ops-runtime.md)'s `AirOpsProjection` already resolved.

| File | Role |
|------|------|
| [`AirOpsReadyAggregateBinder.cs`](../../src/ProjectAegis.Delegation.UnityAdapter/Presentation/AirOpsReadyAggregateBinder.cs) | `AirOpsReadyAggregateBinder` (`Bind` / `BindRows`), `AirOpsReadyAggregatePresenter.ComputeFingerprint`, the `AirOpsReadyAggregateLabels` / `AirOpsReadyAggregateRow` records, `AirOpsReadyCueClasses`, `AirOpsReadyDeclutterTokens` |
| [`AirOpsApplyState.cs`](../../src/ProjectAegis.Delegation/Projection/AirOpsApplyState.cs) | produces the `AirOpsPresentation` (`Aggregate`, `HeaderLine`, `HasReadinessData`, `NoReadinessData` singleton) this binder folds |
| [`AirOpsProjection.cs`](../../src/ProjectAegis.Delegation/Projection/AirOpsProjection.cs) | the engine-agnostic projection whose `Aggregate(entries)` emits the `AirOpsAggregate(ReadyCount, TotalCount, "READY <r>/<t>")` the binder reads |
| [`AirOpsPanelHost.cs`](../../unity/ProjectAegis/Assets/Scripts/Runtime/AirOpsPanelHost.cs) | the UI-Toolkit host that binds the `air-ops-ready-line` label, swaps its cue class, and short-circuits on the fingerprint |
| [`AirOpsPanel.uxml`](../../unity/ProjectAegis/Assets/UI/AirOps/AirOpsPanel.uxml) | declares the `air-ops-header` + `air-ops-ready-line` labels the host paints |

It is **read-only**: it folds an already-projected aggregate into a label + cue class (plus an optional declutter
token and a replay-stable fingerprint). It never calls `IOrderSink` / `ApplyOrder` / `TryIssue`, touches
`DelegationBridge` / `CatalogWriteGate` / `SimulationSession`, re-runs the readiness model, or appends log rows, and
it is **not** part of the Baltic v2 `SimWorldHash`.

---

## The readiness band — `ResolveCueClass`

The binder classifies the aggregate into exactly four bands from `(HasReadinessData, ReadyCount, TotalCount)`:

| Band | Condition | Cue class | Declutter token |
|------|-----------|-----------|-----------------|
| Unknown | `!HasReadinessData` **or** `TotalCount == 0` | `air-ops-ready-cue--unknown` | `[AIR:NO-DATA]` *(only when no readiness data; `TotalCount == 0` with data → no token)* |
| All ready | `ReadyCount == TotalCount` (`> 0`) | `air-ops-ready-cue--all` | `[AIR:ALL-READY]` |
| None ready | `ReadyCount == 0` (`TotalCount > 0`) | `air-ops-ready-cue--none` | `[AIR:NONE-READY]` |
| Partial | `0 < ReadyCount < TotalCount` | `air-ops-ready-cue--partial` | `[AIR:PARTIAL]` |

- **`ResolveCueClass(hasReadinessData, readyCount, totalCount)`** maps to the USS cue token above (both the
  missing-data and empty-roster cases collapse to `--unknown`).
- **`ResolveDeclutterToken(hasReadinessData, readyCount, totalCount)`** returns the `[AIR:…]` text token. Note the
  one asymmetry with the cue: a populated roster of `0/0` is `--unknown` **but** returns a `null` token, whereas a
  genuine missing-readiness-map state returns `[AIR:NO-DATA]`. The token is the accessibility channel — state is
  never signalled by colour alone.

---

## The binder — `AirOpsReadyAggregateBinder`

```csharp
AirOpsReadyAggregateLabels Bind(AirOpsPresentation presentation);
IReadOnlyList<AirOpsReadyAggregateRow> BindRows(AirOpsReadyAggregateLabels labels);

public sealed record AirOpsReadyAggregateLabels(
    string HeaderLine,
    string ReadySummaryLine,
    int ReadyCount,
    int NotReadyCount,
    int TotalCount,
    string ReadyLineText,
    string CueClass,
    string? DeclutterToken,
    string Fingerprint,
    bool HasReadinessData);

public sealed record AirOpsReadyAggregateRow(string ElementName, string Text, string CueClass);
```

`Bind` folds the presentation's already-computed aggregate into:

- **`HeaderLine`** = `presentation.HeaderLine` verbatim (e.g. `AIR OPS  ·  READY 2/3`).
- **`ReadySummaryLine`** = the aggregate's `SummaryLine` (`READY <ready>/<total>`).
- **`ReadyCount` / `TotalCount`** straight from the aggregate; **`NotReadyCount`** = `TotalCount − ReadyCount`.
- **`ReadyLineText`** = the folded one-line status (see below).
- **`CueClass`** / **`DeclutterToken`** = the band's cue + text token.
- **`Fingerprint`** = `AirOpsReadyAggregatePresenter.ComputeFingerprint(presentation)` (see below).
- **`HasReadinessData`** passed through for the host.

A `null` presentation throws `ArgumentNullException` (fail-loud on a missing bundle, not a silent blank). `BindRows`
likewise throws on a `null` labels argument and returns a single row for the `air-ops-ready-line` element so a host
can swap text + cue in one pass.

### `ReadyLineText` — `FormatReadyLine`

- **No readiness data** → the honest `AirOpsApplyState.NoReadinessDataLine` = `"NO READINESS DATA"` (never a
  fabricated `READY 0/0`).
- **`TotalCount == 0`** (but data present) → the bare `SummaryLine` (`READY 0/0`).
- **otherwise** → `"<SummaryLine> · NOT READY <notReady>"`, e.g. `READY 2/3 · NOT READY 1` (invariant-culture
  formatting).

---

## Replay-stable fingerprint — `AirOpsReadyAggregatePresenter.ComputeFingerprint`

The fingerprint lets the host skip a rebind when nothing material changed between frames:

- **`!HasReadinessData`** → `"ao:no-data"`.
- **`Rows.Count == 0 && Aggregate.TotalCount == 0`** → `"ao:empty"`.
- **otherwise** → `"ao:r=<ready>/<total>|rows=<count>"` followed, per row in order, by
  `"|<UnitId>,<ReadyForLaunch?1:0>,<CanLaunch?1:0>,<CanAbort?1:0>"`.

Because it folds each row's `ReadyForLaunch` / `CanLaunch` / `CanAbort` flags, flipping any aircraft's readiness
changes the fingerprint and forces a repaint. `ComputeFingerprint` throws `ArgumentNullException` on a null
presentation.

---

## The live host path — `AirOpsPanelHost`

The panel runs off the `DelegationBridge.Tick` hot path and issues nothing. Each `LateUpdate` it asks the bridge
host to refresh its air-ops list, then calls `Refresh(force: false)`:

1. `AirOpsApplyState.Apply(bridgeHost.LastAirOps, bridgeHost.HasAirOpsReadinessData)` rebuilds the presentation
   (the two-arg overload returns the `NoReadinessData` singleton when the session has no readiness map).
2. `AirOpsReadyAggregateBinder.Bind(presentation)` produces the labels.
3. If not forced **and** `labels.Fingerprint == _lastBindFingerprint`, the host only refreshes the launch/abort
   button enablement and returns — no label churn.
4. Otherwise it stores the new fingerprint and calls `ApplyPresentationToUi`, which sets the `air-ops-header` text
   to `HeaderLine`, sets the `air-ops-ready-line` text to `ReadyLineText`, swaps the cue via
   `ApplyReadyAggregateCueClass` (which clears every `AirOpsReadyCueClasses.All` token before adding the active
   cue), and sets the ready-line tooltip to `DeclutterToken ?? ReadySummaryLine`.

The `air-ops-ready-line` label seeds in `AirOpsPanel.uxml` with the `air-ops-ready-cue--unknown` class and
`READY 0/0` text until the first bind. `ApplyPresentation(presentation)` is the direct test / headless bind entry
point (used instead of pulling from the bridge host).

---

## Determinism & invariants

- **Advisory, off the fingerprint.** The binder classifies an already-projected aggregate; it never writes the sim
  log, re-evaluates readiness, or participates in the `SimWorldHash` / order-log fingerprint. The Baltic v2 hash
  `17144800277401907079` is untouched. (The `ao:` fingerprint is a **presentation** short-circuit key, not a sim
  fingerprint.)
- **Reads one fact source.** The ready/total counts come only from `AirOpsPresentation.Aggregate`; the chrome never
  re-walks the readiness map or re-derives the air-ops FSM phase (that is the
  [logistics-ops runtime](logistics-ops-runtime.md)).
- **Fails closed / honest.** No readiness map → the `--unknown` cue, the `[AIR:NO-DATA]` token, and the literal
  `NO READINESS DATA` line (never a fabricated `READY 0/0`); a `null` presentation or labels throws rather than
  painting a stale cue.
- **Never colour-only.** Every band pairs its cue class with an `[AIR:…]` declutter token (surfaced as the ready-line
  tooltip) and the readable `READY <r>/<t> · NOT READY <n>` text.
- **Deterministic.** Identical inputs produce identical labels, cue classes, and fingerprints. No RNG, no wall clock.

---

## Tests (pins)

NUnit, part of the ≥1638-test baseline:

- [`AirOpsReadyAggregateBinderTests`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/AirOpsReadyAggregateBinderTests.cs)
  — **7** methods: ready / not-ready / total counts + `ReadySummaryLine` + `ReadyLineText` + `HeaderLine` from a
  three-aircraft aggregate, the all-ready cue + `[AIR:ALL-READY]` token, the none-ready cue + `[AIR:NONE-READY]`
  token + `READY 0/1 · NOT READY 1` line, the partial cue + `[AIR:PARTIAL]` token, the no-readiness-data path
  (`--unknown` cue, `[AIR:NO-DATA]` token, `NO READINESS DATA` line, `ao:no-data` fingerprint), the
  `BindRows` → `air-ops-ready-line` element contract, and the fingerprint changing when a unit's ready flag flips.
- [`AirOpsReadyHostContractTests`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/AirOpsReadyHostContractTests.cs)
  — **2** methods asserting the live wiring: `AirOpsPanelHost` binds via `AirOpsReadyAggregateBinder.Bind` /
  `AirOpsReadyCueClasses` / `ApplyReadyAggregateCueClass`, short-circuits on `_lastBindFingerprint` +
  `_readyLabels.Fingerprint`, and targets `air-ops-ready-line`; and `AirOpsPanel.uxml` declares exactly one
  `air-ops-ready-line` element.

---

## See also

- [logistics-ops-runtime.md](logistics-ops-runtime.md) — the air-ops FSM (`AirOpsFsm` / `AirOpsProjection`) and the
  per-tick readiness model that resolves the ready/not-ready state this chrome aggregates and presents.
- [fuel-band-chrome.md](fuel-band-chrome.md) — sibling panel chrome that classifies an already-projected band into
  non-colour-only cue classes + declutter tokens over a different readout.
- [c2-projection-layer.md](c2-projection-layer.md) — general read-model / presentation-boundary rules;
  [c2-presentation-bridges.md](c2-presentation-bridges.md) — the host-feed pattern this binder uses.
