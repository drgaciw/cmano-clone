# Fuel-band chrome — the "am I NOMINAL / JOKER / BINGO on fuel?" unit-detail + message-log cue (DRG-263)

> **ADR:** this is the **presentation / read-model seam**
> ([`adr-010-headless-first-command-driven-ui.md`](../architecture/adr-010-headless-first-command-driven-ui.md) §2–3
> — chrome is a **client**, not sim authority), with
> [`adr-007-c2-map-presentation.md`](../architecture/adr-007-c2-map-presentation.md) (C2 presentation) and
> [`adr-001-sim-assembly-boundary.md`](../architecture/adr-001-sim-assembly-boundary.md) (the adapter boundary).

The **fuel-band chrome** turns an already-projected fuel readout into a non-colour-only band cue in two places —
the unit-detail panel's `FUEL:` line and the message-log fuel rows — *which endurance band (NOMINAL / JOKER /
BINGO) is a unit in, and does that warrant a warning cue?* It only **reads band labels that upstream already
produced**: [`FuelStateProjection`](../../src/ProjectAegis.Delegation/Projection/FuelStateProjection.cs) formats the
unit-detail `FUEL:` line and [`FuelStateChangeRecord`](../../src/ProjectAegis.Delegation/Decision/FuelStateChangeRecord.cs)
carries the order-log transition. This binder never re-reads the burn ledger, re-derives the band from fuel
fractions, consults a second fact source, or writes the sim log — it only **classifies and presents** the band the
[logistics fuel runtime](logistics-fuel-runtime.md) already resolved.

| File | Role |
|------|------|
| [`FuelBandPresentationBinder.cs`](../../src/ProjectAegis.Delegation.UnityAdapter/Presentation/FuelBandPresentationBinder.cs) | `FuelBandLabelResolver`, `UnitDetailFuelBandBinder`, `MessageLogFuelBandBinder`, the `UnitDetailFuelBandPresentation` / `MessageLogFuelBandPresentation` records, `FuelBandCueClasses`, `FuelBandDeclutterTokens` |
| [`FuelStateProjection.cs`](../../src/ProjectAegis.Delegation/Projection/FuelStateProjection.cs) | produces the unit-detail `FUEL: <band> [<kg>kg] (<unit>)` line this binder parses |
| [`FuelStateChangeRecord.cs`](../../src/ProjectAegis.Delegation/Decision/FuelStateChangeRecord.cs) | the order-log band transition (`PreviousState → NewState`) the message-log binder folds |
| [`RightUnitPanelHost.cs`](../../unity/ProjectAegis/Assets/Scripts/Runtime/RightUnitPanelHost.cs) | the UI-Toolkit host that binds the unit-detail `fuel-line` label and swaps its cue class |
| [`MessageLogPanelHost.cs`](../../unity/ProjectAegis/Assets/Scripts/Runtime/MessageLogPanelHost.cs) | the message-log host that layers the band cue onto each bound `FUEL` row |

It is **read-only**: it folds already-projected band text and emits a label + cue class (plus an optional declutter
token). It never calls `IOrderSink` / `ApplyOrder` / `TryIssue`, touches `DelegationBridge` / `CatalogWriteGate` /
`SimulationSession`, re-runs the fuel model, or appends its own log/timeline rows, and it is **not** part of the
Baltic v2 `SimWorldHash`.

---

## The band ladder — `FuelBandLabelResolver`

The resolver recognises exactly three bands, case-insensitively; anything else normalises to **unknown**:

| Band | Label constant | Cue class | Declutter token |
|------|----------------|-----------|-----------------|
| Nominal | `NOMINAL` | `fuel-band-cue--nominal` | — (no warning) |
| Joker | `JOKER` | `fuel-band-cue--joker` | `[FUEL:JOKER]` |
| Bingo | `BINGO` | `fuel-band-cue--bingo` | `[FUEL:BINGO]` |
| *(unrecognised / blank)* | — | `fuel-band-cue--unknown` | — |

- **`CueClassForBand(band)`** maps the normalised band to its USS cue token (unknown band → the `--unknown` cue).
- **`DeclutterTokenForBand(band)`** returns the `[FUEL:…]` warning token for `JOKER` / `BINGO` only, else `null`.

There are three parse entry points, one per source:

- **`ResolveFromUnitFuelLine(fuelLine)`** — strips the `FUEL:` prefix (case-insensitive) and reads the first
  whitespace/`(`-delimited token as the band (e.g. `FUEL: JOKER 2400kg (u1)` → `JOKER`). A line that does not start
  with `FUEL:` is still probed as a bare band token; a null/blank line → `null`.
- **`ResolveFromFuelStateChange(change)`** — reads `change.NewState`, returning the normalised band or the literal
  `"UNKNOWN"` for an unrecognised state (this path **always** yields a non-null label).
- **`ResolveFromMessageLogRow(row)` / `ResolveFromMessageLogDisplayLine(line)`** — only rows whose `Category` is
  `FUEL` (case-insensitive) resolve; any other category → `null` (no cue). The display line is parsed by reading the
  **post-`→` transition token** (the band being entered), e.g. `[FUEL] Fuel u1: JOKER → BINGO (800 kg)` → `BINGO`.

---

## Unit-detail chrome — `UnitDetailFuelBandBinder`

```csharp
UnitDetailFuelBandPresentation Bind(UnitDetailPresentation presentation);

public sealed record UnitDetailFuelBandPresentation(
    string FuelLineText,
    string? BandLabel,
    string CueClass,
    string? DeclutterToken);
```

`Bind` folds the panel's already-projected `FuelLine` into:

- **`FuelLineText`** = the projection's `FUEL:` line verbatim (never rewritten).
- **`BandLabel`** = the resolved band, or `null` when the line carries no recognised band.
- **`CueClass`** = the band's cue (`--unknown` when unresolved).
- **`DeclutterToken`** = `[FUEL:JOKER]` / `[FUEL:BINGO]` on a warning band, else `null`.

A `null` presentation throws `ArgumentNullException` (fail-loud on a missing panel, not a silent blank).

---

## Message-log chrome — `MessageLogFuelBandBinder`

```csharp
MessageLogFuelBandPresentation Bind(MessageLogDisplayRow row);
MessageLogFuelBandPresentation Bind(FuelStateChangeRecord change);
string FormatFuelStateChangeDisplayLine(FuelStateChangeRecord change);

public sealed record MessageLogFuelBandPresentation(string? BandLabel, string? CueClass);
```

- **`Bind(MessageLogDisplayRow)`** classifies a bound log row. A non-`FUEL` row (or an unresolved `FUEL` row) yields
  `(null, null)` — the chrome adds **no** cue, leaving the row's category styling untouched.
- **`Bind(FuelStateChangeRecord)`** classifies an order-log transition record straight from the decision log; its
  `BandLabel` is never null (`"UNKNOWN"` for an unrecognised `NewState`).
- **`FormatFuelStateChangeDisplayLine(change)`** is the canonical row text:
  `"[FUEL] Fuel <unitId>: <PreviousState> → <NewState> (<RemainingFuelKg:F0> kg)"`, e.g.
  `[FUEL] Fuel u1: NOMINAL → JOKER (2400 kg)`.

Each binder throws `ArgumentNullException` on a null argument.

---

## Cue classes (never color-only)

```csharp
public static class FuelBandCueClasses
{
    public const string Unknown = "fuel-band-cue--unknown";
    public const string Nominal = "fuel-band-cue--nominal";
    public const string Joker   = "fuel-band-cue--joker";
    public const string Bingo   = "fuel-band-cue--bingo";
    public static IReadOnlyList<string> All { get; } // the list a host clears before applying the active cue
}
```

The cue classes are **binder constants applied at runtime** — the host clears `FuelBandCueClasses.All` from the
element, then adds the active cue. They are not declared in a `.uss` stylesheet; a theme that wants to colour them
styles the class names above. Band state is **also** carried in text (the `[FUEL:JOKER]` / `[FUEL:BINGO]` declutter
suffix on the unit-detail line, and the `→ <band>` transition in the log row), so a JOKER/BINGO warning is never
signalled by colour alone. `FuelBandCueClassList.All` re-exports the same list for host code.

---

## The live host paths

Both consumers run off the `DelegationBridge.Tick` hot path and issue nothing.

**Unit detail** ([`RightUnitPanelHost`](../../unity/ProjectAegis/Assets/Scripts/Runtime/RightUnitPanelHost.cs)):
on refresh it binds `UnitDetailFuelBandBinder.Bind(_presentation)`, sets the `fuel-line` label text to
`FuelLineText` (appending `" <DeclutterToken>"` when a warning band is active), and calls `ApplyFuelBandCueClass`,
which clears every `FuelBandCueClasses.All` token from the label before adding the active cue. The label seeds to
`"FUEL: —"` with the `--unknown` cue until the first bind. The `fuel-line` element is declared in
[`UnitDetailPanel.uxml`](../../unity/ProjectAegis/Assets/UI/UnitDetail/UnitDetailPanel.uxml) with the shared
`unit-detail-line` class.

**Message log** ([`MessageLogPanelHost`](../../unity/ProjectAegis/Assets/Scripts/Runtime/MessageLogPanelHost.cs)):
inside `bindItem`, after applying the base + category CSS classes, it binds `MessageLogFuelBandBinder.Bind(row)`,
removes every `FuelBandCueClasses.All` token from the row label, and adds the band cue when one resolves. It does
**not** append a fuel timeline or its own state-change rows — the row text already carries the `→` transition, and
the chrome only layers the cue.

---

## Determinism & invariants

- **Advisory, off the fingerprint.** The binder classifies already-projected band text; it never writes the sim
  log, re-evaluates the fuel model, or participates in the `SimWorldHash` / order-log fingerprint. The Baltic v2 hash
  `17144800277401907079` is untouched.
- **Reads one fact source.** The band comes only from the projected `FUEL:` line / `FuelStateChangeRecord`; the
  chrome never re-derives it from fuel fractions or sim time (that is the [logistics fuel runtime](logistics-fuel-runtime.md)).
- **Fails closed / loud.** An unrecognised or blank band → the `--unknown` cue (never a guessed NOMINAL); a `null`
  presentation or record throws rather than painting a stale cue. A non-`FUEL` log row gets no cue at all.
- **Never color-only.** Every warning band pairs its cue class with a `[FUEL:…]` text token (unit detail) or the
  `→ <band>` transition (log row).
- **Deterministic.** Identical inputs produce identical labels and cue classes. No RNG, no wall clock.

---

## Tests (pins)

NUnit, part of the ≥1638-test baseline:

- [`FuelBandPresentationBinderTests`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/FuelBandPresentationBinderTests.cs)
  — **6** methods: JOKER / BINGO resolve + cue + declutter token from a unit-detail projection line (both the raw
  `FuelStateProjection` line and a `UnitDetailApplyState`-applied panel), the `FuelStateChangeRecord` prop path with
  its `FormatFuelStateChangeDisplayLine` parity, the bound `MessageLogDisplayRow` transition path, the non-`FUEL`
  category returning no band/cue, and NOMINAL using the nominal cue with no declutter token.
- [`FuelBandHostContractTests`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/FuelBandHostContractTests.cs)
  — **2** methods asserting the live wiring: `RightUnitPanelHost` binds via `UnitDetailFuelBandBinder.Bind` /
  `FuelBandCueClasses` / `ApplyFuelBandCueClass` on `fuel-line`, and `MessageLogPanelHost` binds via
  `MessageLogFuelBandBinder.Bind` + clears `FuelBandCueClasses.All` with **no** host-side fuel timeline
  (`AppendFuelStateChange` / `FuelTimelineTracker`).

---

## See also

- [logistics-fuel-runtime.md](logistics-fuel-runtime.md) — the per-tick burn ledger / time-band model that resolves
  the NOMINAL / JOKER / BINGO state this chrome presents; the Joker/Bingo thresholds live in the scenario
  `logistics` block.
- [combat-event-log-projection.md](combat-event-log-projection.md) / [order-log-runtime.md](order-log-runtime.md) —
  the order-log + message-log rows the message-log binder layers its cue onto.
- [wra-salvo-remaining-chrome.md](wra-salvo-remaining-chrome.md) / [attack-options-preview-chrome.md](attack-options-preview-chrome.md)
  — sibling unit-panel chrome binders over other already-projected readouts.
- [c2-projection-layer.md](c2-projection-layer.md) — general read-model / presentation-boundary rules;
  [c2-presentation-bridges.md](c2-presentation-bridges.md) — the host-feed pattern this binder uses.
