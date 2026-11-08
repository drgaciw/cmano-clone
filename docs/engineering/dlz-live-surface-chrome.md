# DLZ live-surface chrome — the "is the target inside my launch envelope?" range row (DRG-266)

> **ADR:** this is the **presentation / read-model seam**
> ([`adr-010-headless-first-command-driven-ui.md`](../architecture/adr-010-headless-first-command-driven-ui.md) §2–3
> — chrome is a **client**, not sim authority), with
> [`adr-007-c2-map-presentation.md`](../architecture/adr-007-c2-map-presentation.md) (C2 presentation) and
> [`adr-001-sim-assembly-boundary.md`](../architecture/adr-001-sim-assembly-boundary.md) (the adapter boundary).

The **DLZ live-surface chrome** answers one question on the contact-hover and weapon panels — *is this target
inside, approaching, or outside the dynamic launch zone (DLZ) for my weapon right now?* — as a single `dlz-line`
row with a `DlzState`-keyed cue. It folds **already-projected** range facts (the structured
[`ThreatRangeAssessment`](../../src/ProjectAegis.Delegation/ThreatAssessment/ThreatRangeAssessment.cs), or, when no
exact range exists, the `DlzLabel` token already on the selected unit's
[`EngagePreview`](../../src/ProjectAegis.Delegation/Projection/EngagePreviewProjection.cs)). It never re-runs the
DLZ gate, re-computes range, evaluates the ROE/engage gate, or issues a fire order — the `DlzState` and the
`DLZ: <state> (<personality>)` label are produced upstream by
[`EngagePreviewProjection`](../../src/ProjectAegis.Delegation/Projection/EngagePreviewProjection.cs) /
`DlzEngageGate`; this binder only **presents** them.

It is **live-hosted** across three panels: the selected unit's weapon panel ([`RightUnitPanelHost`](../../unity/ProjectAegis/Assets/Scripts/Runtime/RightUnitPanelHost.cs)),
the engage-explain status row ([`EngageExplainPanelHost`](../../unity/ProjectAegis/Assets/Scripts/Runtime/EngageExplainPanelHost.cs)),
and the contact-hover card ([`ContactDetailPanelHost`](../../unity/ProjectAegis/Assets/Scripts/Runtime/ContactDetailPanelHost.cs)).

| File | Role |
|------|------|
| [`DlzLiveSurfaceBinder.cs`](../../src/ProjectAegis.Delegation.UnityAdapter/Presentation/DlzLiveSurfaceBinder.cs) | the three binders (`Bind`, `BindFromEngagePreview`, `BindContact`), `DlzLiveSurfaceState`, the cue/declutter catalogs, `DlzLiveSurfacePanelBinder.BindRows`, `ParseDlzState` |
| [`DelegationBridgeHost.cs`](../../unity/ProjectAegis/Assets/Scripts/Runtime/DelegationBridgeHost.cs) | `ProjectSelectedEngagePreview()` (CMD-11) and `ProjectSelectedContactThreatRange()` — resolve the range evidence the hosts feed in |
| [`RightUnitPanelHost.cs`](../../unity/ProjectAegis/Assets/Scripts/Runtime/RightUnitPanelHost.cs) / [`EngageExplainPanelHost.cs`](../../unity/ProjectAegis/Assets/Scripts/Runtime/EngageExplainPanelHost.cs) / [`ContactDetailPanelHost.cs`](../../unity/ProjectAegis/Assets/Scripts/Runtime/ContactDetailPanelHost.cs) | the UI-Toolkit hosts that store `LastDlzSurface` and paint `dlz-line` |

It is **read-only**: it folds DTOs other projections already produced and emits one display row. It never calls
`IOrderSink` / `ApplyOrder` / `TryIssue`, touches `DelegationBridge` / `CatalogWriteGate` / `SimulationSession`, or
re-runs the DLZ/engage gate, and it is **not** part of the Baltic v2 `SimWorldHash`.

---

## Inputs — three entry points, one state

```csharp
DlzLiveSurfaceState Bind(ThreatRangeAssessment? range);
DlzLiveSurfaceState BindFromEngagePreview(EngagePreview? preview);
DlzLiveSurfaceState BindContact(string? contactId, ThreatRangeAssessment? threatRange, EngagePreview? unitEngagePreview);
```

- **`Bind(range)`** — the **exact** weapon-panel path. Reads `DlzState`, `DlzLabel`, `RangeMeters`,
  `EnvelopeMinMeters`, `EnvelopeMaxMeters`, and `InEnvelope` off the
  [`ThreatRangeAssessment`](../../src/ProjectAegis.Delegation/ThreatAssessment/ThreatRangeAssessment.cs) and emits the
  richer line `DLZ: <label> | <range> m | envelope <min>–<max> m`. A null range **fails closed** to
  [`DlzLiveSurfaceState.Empty`](#the-empty-placeholder).
- **`BindFromEngagePreview(preview)`** — the **label-only** path (positional props only). It has no range numbers,
  so it `ParseDlzState`s the `DlzLabel` token and emits the bare label as the line. A null preview → `Empty`.
- **`BindContact(contactId, threatRange, unitEngagePreview)`** — the **contact-hover** path. Null/blank `contactId`
  fails closed to `Empty`; otherwise it **prefers the exact threat range**, then the selected unit's engage preview,
  then a help line (see the ladder below).

`DlzState` ([`DlzState.cs`](../../src/ProjectAegis.Sim/Engage/DlzState.cs)) is `Unknown(0)`, `OutOfZone(1)`,
`Approaching(2)`, `InZone(3)`. The upstream `DlzLabel` is `"DLZ: <state> (<personality>)"` (e.g.
`DLZ: InZone (Normal)`); `ParseDlzState` reads the token between `DLZ:` and the `(` qualifier and `Enum.TryParse`s
it (unknown/blank → `DlzState.Unknown`).

---

## The resolve ladders

### `BindContact` — exact range first, then preview, then help

| Condition (first match wins) | Result |
|------------------------------|--------|
| `contactId` null/blank | `DlzLiveSurfaceState.Empty` (`Missing_contact_selection_fails_closed`) |
| `threatRange` present | `Bind(threatRange)` — exact range + envelope (`Contact_hover_prefers_exact_threat_range_over_unit_preview`) |
| else `unitEngagePreview` present | `BindFromEngagePreview(preview)` — the selected unit's DLZ label (`Contact_hover_falls_back_to_unit_preview_when_no_threat_range`) |
| else | `DLZ: UNKNOWN — select a friendly unit for range assessment`, `[DLZ:UNKNOWN]` cue (`Contact_hover_without_range_or_preview_reports_unknown`) |

So an in-zone contact at 50 km with a 1–100 km envelope → `DLZ: InZone (Normal) | 50000 m | envelope 1000–100000 m`,
short `IN` (`Threat_range_assessment_binds_in_zone_with_range_and_cue`); an approaching preview → short `MARGINAL`
(`Engage_preview_binds_approaching_as_marginal_without_recomputing_sim`); an out-of-zone preview → short `OUT`
(`Engage_preview_binds_out_of_zone_with_out_cue`).

### `Build` — the line format

The shared builder appends an `| outside envelope` suffix **only when** `InEnvelope == false` and the state is not
already `OutOfZone` — so a marginal/approaching target that still sits outside the hard envelope is called out, but
an already-`OUT` row is not double-labelled.

---

## `DlzLiveSurfaceState`

```csharp
public sealed record DlzLiveSurfaceState(
    string StateLine, string ShortLabel, string DeclutterToken,
    string CueClass, DlzState DlzState);
```

- **`StateLine`** is the painted `dlz-line` text (invariant culture for the numeric formats).
- **`ShortLabel`** is the compact badge: `IN` / `MARGINAL` / `OUT` / `UNKNOWN`.
- **`DlzState`** echoes the resolved enum so a host can key further chrome off it without re-parsing text.

### Cue classes & declutter tokens (never color-only)

| `DlzState` | `ShortLabel` | `CueClass` | `DeclutterToken` |
|------------|--------------|-----------|------------------|
| `InZone` | `IN` | `dlz-cue--in-zone` | `[DLZ:IN]` |
| `Approaching` | `MARGINAL` | `dlz-cue--approaching` | `[DLZ:MARGINAL]` |
| `OutOfZone` | `OUT` | `dlz-cue--out-of-zone` | `[DLZ:OUT]` |
| `Unknown` | `UNKNOWN` | `dlz-cue--unknown` | `[DLZ:UNKNOWN]` |

Every cue class is paired with a text declutter token so a host never signals DLZ state by color alone.
`DlzCueClasses.All` is the list a host clears before applying the active cue.

### The `Empty` placeholder

`DlzLiveSurfaceState.Empty` is the cleared row for no/blank selection or missing range evidence: `"DLZ: —"`, short
`"—"`, an **empty** declutter token, the `dlz-cue--unknown` cue, and `DlzState.Unknown`.

### `BindRows` — the host paint contract

`DlzLiveSurfacePanelBinder.BindRows(state)` maps the state onto exactly one
`DlzLiveSurfaceRow(ElementName, Text, CueClass)`:

| `ElementName` | Text | CueClass |
|---------------|------|----------|
| `dlz-line` | `StateLine` | `state.CueClass` |

(`Panel_binder_maps_dlz_line_and_cue_class`.)

---

## The live host path

The binder is wired end-to-end on three panels, all off the `DelegationBridge.Tick` hot path:

1. **`ContactDetailPanelHost`** (contact hover) resolves `BindContact(contactId,
   bridgeHost.ProjectSelectedContactThreatRange(), bridgeHost.ProjectSelectedEngagePreview())`, stores it as
   `LastDlzSurface`, and paints `dlz-line` via `BindRows` (clearing `DlzCueClasses.All` first).
   `ProjectSelectedContactThreatRange()` returns `evidence.ThreatAssessment?.Range` only when the snapshot is an
   `IAdviceEvidenceSource` that has advice for the selected contact — otherwise `null`, so the hover falls back to
   the unit preview or the help line.
2. **`RightUnitPanelHost`** (weapon panel) and **`EngageExplainPanelHost`** (status row) both resolve
   `BindFromEngagePreview(bridgeHost.ProjectSelectedEngagePreview())` and paint the same `dlz-line`.
   `ProjectSelectedEngagePreview()` fails closed to `null` with no snapshot or selection; otherwise it mirrors
   `DelegationBridge.GetEngagePreviewForUnit(SelectedUnitId, snapshot)` (CMD-11 / DRG-266).
3. The **`dlz-line`** element is declared in both
   [`ContactDetailPanel.uxml`](../../unity/ProjectAegis/Assets/UI/ContactDetail/ContactDetailPanel.uxml) and
   [`UnitDetailPanel.uxml`](../../unity/ProjectAegis/Assets/UI/UnitDetail/UnitDetailPanel.uxml).

The hosts project — they never set a fact, touch a controller slot, or call `DelegationBridge.Tick`.

---

## Determinism & invariants

- **Advisory, off the fingerprint.** The binder folds already-projected range/label facts; it never writes the sim
  log, re-evaluates a gate, or participates in the `SimWorldHash` / order-log fingerprint. The Baltic v2 hash
  `17144800277401907079` is untouched.
- **Fails closed.** No/blank contact or missing range evidence → `Empty`; a hover with neither exact range nor unit
  preview shows an explicit "select a friendly unit" help line, never a blank or guessed zone.
- **Exact range wins.** `BindContact` prefers the structured `ThreatRangeAssessment` over the unit's engage-preview
  label, so a contact's own measured range is never displaced by the selected shooter's preview.
- **Never color-only.** Every cue class is paired with a text declutter token, and `ShortLabel` carries the state in
  text.
- **Deterministic.** Identical inputs produce identical rows (`Repeated_bind_is_replay_stable`); no RNG, no wall
  clock.

---

## Tests (pins)

NUnit, part of the ≥1638-test baseline:

- [`DlzLiveSurfaceBinderTests`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/DlzLiveSurfaceBinderTests.cs)
  — **10** methods / 15 cases (in-zone range + envelope line, approaching → `MARGINAL`, out-of-zone → `OUT`,
  null/empty/blank contact → `Empty`, exact-range-over-preview preference, preview fallback, no-evidence help line,
  `BindRows` → `dlz-line`, replay-stable repeat, and the `ParseDlzState` label-token table).
- [`DlzLiveSurfaceHostContractTests`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/DlzLiveSurfaceHostContractTests.cs)
  — **5** methods asserting the live wiring (`ContactDetailPanelHost` binds via `BindContact` / `BindRows` /
  `LastDlzSurface`, `RightUnitPanelHost` and `EngageExplainPanelHost` bind via `BindFromEngagePreview`, and both
  `ContactDetailPanel.uxml` / `UnitDetailPanel.uxml` declare `dlz-line`).

---

## See also

- [wra-salvo-remaining-chrome.md](wra-salvo-remaining-chrome.md) — the sibling weapon-panel chrome (DRG-259) that
  consumes the same `EngagePreview` (its `DlzLabel`) and `RightUnitPanelHost`.
- [weapon-abort-tooltip-chrome.md](weapon-abort-tooltip-chrome.md) — the weapon-panel chrome (DRG-258) that explains
  *why* a weapon is denied, over the same engage-preview inputs.
- [engage-explain-contract-projection.md](engage-explain-contract-projection.md) — the engage-explain projection the
  `EngageExplainPanelHost` status row shares with this DLZ cue.
- [engagement-pipeline.md](engagement-pipeline.md) — the engage-time DLZ/launch gate (`DlzEngageGate`) that produces
  the `DlzState` and label this row presents.
- [abort-reason-catalog.md](abort-reason-catalog.md) — the `DLZ_OUT` code the preview carries when the launch is
  outside the zone.
- [c2-projection-layer.md](c2-projection-layer.md) — general read-model / presentation-boundary rules;
  [c2-presentation-bridges.md](c2-presentation-bridges.md) — the host-feed pattern these binders use.
