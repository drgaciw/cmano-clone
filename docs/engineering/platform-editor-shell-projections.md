# Platform & Scenario Editor shell projections

The **editor shells** are the pure, engine-agnostic read-models behind the Platform Editor
and Scenario Editor chrome — the tab strip, top bar, status/health strip, catalog browse
rows, dependency-graph list, and the Live Findings dock. They live in
[`ProjectAegis.Delegation/Projection/`](../../src/ProjectAegis.Delegation/Projection/) next to
the tactical C2 projections, but they describe the **authoring/editor** surface rather than the
run-time tactical picture.

> **Scope / boundary (ADR-010 §2–3, ADR-011).** Every type here is a **presentation
> read-model**: a pure function of its inputs with **no write-gate, no edit bus, no sim, and no
> `UnityEngine` reference**. The Unity host stays a *thin binder* — it calls `Bind(...)` / the
> `With*` transforms, then writes the returned label text and USS class names onto its visual
> elements. Nothing here mutates the catalog, the `DecisionLog`, or replay state, so none of it
> touches the Baltic replay goldens. The **write** side (propose/approve) is
> `CatalogWriteGate` / `PlatformWorkbookWriteService` — see
> [platform-workbook-roundtrip.md](platform-workbook-roundtrip.md) and the host bridges in
> [`ProjectAegis.Delegation.UnityAdapter/Bridge/`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/).

This page documents the *shell* read-models. For the tactical C2 panels (message log, contact
picture, map symbology, …) and the shared `Projection → Binder → State` layering, see
[c2-projection-layer.md](c2-projection-layer.md); it lists the catalog browsers in one row but
does not explain the editor chrome.

---

## Where it lives

| Type | File | Responsibility |
|------|------|----------------|
| `PlatformEditorShellProjection` | [`PlatformEditorShellProjection.cs`](../../src/ProjectAegis.Delegation/Projection/PlatformEditorShellProjection.cs) | `Catalog \| Import` tab navigation for the unified Platform Editor shell (PE-UX-W4 / P-PE-04). |
| `ScenarioEditorShellProjection` | [`ScenarioEditorShellProjection.cs`](../../src/ProjectAegis.Delegation/Projection/ScenarioEditorShellProjection.cs) | `Map \| Mission Board` navigation + top bar + Play/Sample gate + findings dock (SE-UX P2.1/P2.2). |
| `ScenarioEditorFindingsProjection` | (same file) | Pure helpers for Live Findings dock rows, filters, and severity tags (P-SE-02). |
| `PlatformCatalogListProjection` | [`PlatformCatalogListProjection.cs`](../../src/ProjectAegis.Delegation/Projection/PlatformCatalogListProjection.cs) | One-line browse-row label (ADR-011 Phase F). |
| `PlatformCatalogDetailProjection` | [`PlatformCatalogDetailProjection.cs`](../../src/ProjectAegis.Delegation/Projection/PlatformCatalogDetailProjection.cs) | Labelled detail panel for the selected row (ADR-011 Phase C / PE-UX-W2). |
| `PlatformCatalogFilterProjection` | [`PlatformCatalogFilterProjection.cs`](../../src/ProjectAegis.Delegation/Projection/PlatformCatalogFilterProjection.cs) | Case-insensitive browse-row filter (ADR-011 Phase C). |
| `PlatformCatalogGraphProjection` | [`PlatformCatalogGraphProjection.cs`](../../src/ProjectAegis.Delegation/Projection/PlatformCatalogGraphProjection.cs) | Dependency-graph line formatting with focus / search / cap (PE-UX-W5). |
| `PlatformCatalogHealthProjection` | [`PlatformCatalogHealthProjection.cs`](../../src/ProjectAegis.Delegation/Projection/PlatformCatalogHealthProjection.cs) | Read-only catalog health strip (PE-UX-W5). |

**Input feeds** (also pure): browse rows come from
[`CatalogPlatformBrowseProjection`](../../src/ProjectAegis.Delegation/Projection/CatalogPlatformBrowseProjection.cs)
(`CatalogPlatformBrowseRow`); dependency edges come from
[`CatalogDependencyEdge`](../../src/ProjectAegis.Data/Catalog/CatalogDependencyEdge.cs) — the same
edge model the CLI dependency-graph report uses, see
[catalog-integrity-reports.md](catalog-integrity-reports.md).

---

## Platform Editor shell

`PlatformEditorShellProjection.Bind(mode, selectedPlatformId, statusSummary)` returns an
immutable `PlatformEditorShellState`. The mode is the two-pane switch:

| `PlatformEditorShellMode` | `ModeTitle` | `RootUssClass` | Active/visible flags set |
|---------------------------|-------------|----------------|--------------------------|
| `Catalog` (default) | `BROWSE CATALOG` | `platform-editor-shell--browse` | `CatalogTabActive` / `CatalogContentVisible` |
| `Import` | `IMPORT STAGING` | `platform-editor-shell--import` | `ImportTabActive` / `ImportContentVisible` |

Navigation is expressed as pure transforms that re-`Bind` from the current state:
`WithMode`, `WithSelection`, `WithStatus`, and `CycleMode` (toggles Catalog ↔ Import).
`TabUssClass(isActive)` returns the active/inactive tab class so the host never hard-codes
style strings.

### Catalog browse read-models

Inside the Catalog pane, four projections format the `CatalogPlatformBrowseRow` feed. All render
a missing value as `—` and are culture-invariant:

- **List** — `FormatRow(row)` produces one scannable line:
  `id hp=… res=… withdraw=… flags=… speed=… mounts=… sensors=…`.
  `FormatRowWithMagazine(row, magazineCountsByPlatform)` appends `mags=…` from an external count
  lookup (magazine capacity is surfaced *for display only*, not resolved here).
- **Detail** — `Format(row?)` returns a `PlatformCatalogDetailEntry` of labelled fields
  (`HP`, `RESILIENCE`, `WITHDRAW %`, `FLAGS`, `SPEED kt`, `RADIUS nm`, `MOUNTS`, `SENSORS`, `ID`).
  A `null` row yields the all-`—` `Empty()` entry. Scenario `LAT`/`LON` are tagged `(doc 11)`
  and deliberately demoted — the class editor is not doc-11 placement (Req 21).
- **Filter** — `Apply(rows, filterText)` is case-insensitive and matches **either** the
  `PlatformId` **or** the formatted list line, so a search like `speed=` or a value substring
  works. A null/blank filter returns the rows unchanged.
- **Graph** — `FormatLines(edges, focusPlatformId?, search?, displayCap = 20)` renders
  `CatalogDependencyEdge`s. A `focusPlatformId` filters to that platform (ordinal) and shows
  **all** its edges (the display cap applies only to the unfocused view); `search` is an
  `OrdinalIgnoreCase` substring over the formatted line. An empty result under a focus returns a
  single `(no graph edges for <id>)` line. `FormatEdgeLine` renders per
  `CatalogDependencyEdgeKind` (platform→link/fitting, →sensor, →mount→weapon, →mount).

### Health strip

`PlatformCatalogHealthProjection.Format(blockedFindingCount, pendingDiffCount, dependencyEdgeCount)`
returns `Health: OK|ATTENTION · edges N · pending N · blocked N`. The level is `ATTENTION`
whenever there is at least one blocked finding **or** one pending diff, otherwise `OK`. It is a
read-only summary — it has no write path and never gates a save.

---

## Scenario Editor shell

`ScenarioEditorShellProjection.Bind(...)` returns a richer `ScenarioEditorShellState` covering
three concerns: **mode navigation**, the **product top bar**, and the **Live Findings dock**.

| `ScenarioEditorShellMode` | `ModeTitle` | `RootUssClass` |
|---------------------------|-------------|----------------|
| `Map` (default) | `MAP AUTHORING` | `scenario-editor-shell--map` |
| `MissionBoard` | `MISSION BOARD` | `scenario-editor-shell--mission-board` |

`EventsTabEnabled` is always `false` — the Events tab is reserved for P2.3.

**Top bar** (`WithTopBar`) surfaces `ScenarioTitle` (blank ⇒ `Untitled Scenario`), `IsDirty` +
`DirtyLabel` (`• unsaved` / `saved`), `EditVersion`, and the action-enabled flags. The gating is
the load-bearing part:

| Action | Enabled when |
|--------|--------------|
| `Save` | `sessionOpen` |
| `Load` | always |
| `Undo` / `Redo` | `sessionOpen` **and** the caller's undo/redo availability |
| `Play` / `Sample` | `sessionOpen` **and not** `PlayBlocked` |

**Play/Sample gate:** `PlayBlocked` is true whenever `ErrorFindingCount > 0` (errors only —
warnings never block), and `PlayBlockReason` reads e.g. `Blocked by 2 error findings`. This is
the shell-side reflection of the export/play gate enforced by the validation engine in
[`ProjectAegis.Data/Validation/`](../../src/ProjectAegis.Data/Validation/).

### Live Findings dock

Findings flow in as `ScenarioEditorFindingRow`s (built by
`ScenarioEditorFindingsProjection.CreateRow`), each carrying a **text `SeverityTag`**
(`ERROR` / `WARN` / `INFO`) alongside the `Severity` enum — a tag *and* colour, never
colour-only (accessibility). The dock supports:

- **Filter** — `WithFindingsFilter` / `Filter(rows, filter)` narrows to `All` / `Errors` /
  `Warnings`; `VisibleFindingRows` is the filtered view while `FindingRows` keeps the full set.
  `FindingsSummaryText` reads `Findings: N errors · M warnings`.
- **Jump-to** — `JumpToFinding(state, row)` selects the row's `EntityId` and, when the row (or
  the `PreferMissionBoardForCode` heuristic — `MISSION`/`STRIKE`/`PATROL`/`FERRY`/`SUPPORT`/
  `TARGET` codes) prefers it, switches to `MissionBoard` mode.
- **USS helpers** — `FilterChipUssClass`, `RowSeverityUssClass`, `TabUssClass(isActive,
  isEnabled)`, and `ActionEnabledUssClass(enabled)` so the host binds classes without string
  literals.

`WithFindingRows(rows, filter?)` recomputes the error/warning counts from the rows, keeping the
top-bar gate and the dock consistent in one call.

---

## Extending safely

- **Add a shell field or label** — extend the `*State` record and set it in `Bind`; add a `With*`
  transform if it changes independently. Hosts look labels up by name with a null-safe query, so
  a new label needs no scene/panel rebuild (a UXML that omits it simply skips it).
- **Keep it pure** — no `UnityEngine`, no I/O, no clock, no RNG, no write-gate. Derive everything
  from the method inputs so the projection stays unit-testable and replay-neutral.
- **Route writes through the gate** — surfacing a new catalog value for display is fine; changing
  one is a `CatalogWriteGate` / `PlatformWorkbookWriteService` propose/approve, never a shell
  edit ([platform-workbook-roundtrip.md](platform-workbook-roundtrip.md)).

---

## Tests

Pure projections are pinned in
[`ProjectAegis.Delegation.Tests/Projection/`](../../src/ProjectAegis.Delegation.Tests/Projection/)
(NUnit): `PlatformEditorShellProjectionTests`, `ScenarioEditorShellProjectionTests`,
`PlatformCatalogListProjectionTests`, `PlatformCatalogDetailProjectionTests`,
`PlatformCatalogFilterProjectionTests`, `PlatformCatalogGraphProjectionTests`,
`PlatformCatalogHealthProjectionTests`. Host binding is covered by
`PlatformEditorShellHostTests` / `PlatformCatalogViewerTests` in
[`ProjectAegis.Delegation.UnityAdapter.Tests/Platform/`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Platform/)
and the C2 proxy `PlayModeSmokeHarnessTests`.

```bash
dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj \
  -v minimal --filter "FullyQualifiedName~Projection.PlatformEditorShell|FullyQualifiedName~Projection.ScenarioEditorShell|FullyQualifiedName~Projection.PlatformCatalog"
```

---

## See also

| Topic | Where |
|-------|-------|
| Tactical C2 panels + `Projection → Binder → State` layering | [c2-projection-layer.md](c2-projection-layer.md) |
| Platform Editor host bridges (export/design/write) | [`ProjectAegis.Delegation.UnityAdapter/Bridge/`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/) |
| Platform workbook round-trip (write side) | [platform-workbook-roundtrip.md](platform-workbook-roundtrip.md) |
| Dependency-edge model (`CatalogDependencyEdge`) + CLI reports | [catalog-integrity-reports.md](catalog-integrity-reports.md) |
| Scenario validation / findings source + export gate | [`ProjectAegis.Data/Validation/`](../../src/ProjectAegis.Data/Validation/) · [scenario-authoring-host.md](scenario-authoring-host.md) |
| Presentation boundary rationale | [ADR-010](../architecture/adr-010-headless-first-command-driven-ui.md) · [ADR-011](../architecture/adr-011-platform-editor-excel-roundtrip.md) |
