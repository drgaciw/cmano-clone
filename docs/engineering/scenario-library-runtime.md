# Scenario library runtime — browse list, pre-load feasibility & preview

> **Scope.** The pure, headless **scenario-library browse** subsystem (CMD-27): the
> data-layer enumerator/projector in [`src/ProjectAegis.Data/Scenario/`](../../src/ProjectAegis.Data/Scenario/)
> (`ScenarioLibraryLister`, `ScenarioLibraryProjection`, `ScenarioLibraryEntry`,
> `ScenarioLibraryReasons`) that turns a directory of `*.scenario.json` documents into a sorted,
> **pre-load feasibility**-annotated row list, and the presentation apply-state in
> [`src/ProjectAegis.Delegation/Projection/`](../../src/ProjectAegis.Delegation/Projection/)
> (`ScenarioLibraryApplyState` → `ScenarioLibraryPresentation` / `ScenarioLibraryPreviewPresentation`)
> that a UI host binds without re-formatting. This is the **read side that decides whether a
> scenario can even be opened**; authoring the documents themselves is
> [`scenario-document-authoring.md`](scenario-document-authoring.md) and the interactive edit host is
> [`scenario-authoring-host.md`](scenario-authoring-host.md).
>
> Boundary rationale: [ADR-010 (headless-first, command-driven UI)](../architecture/adr-010-headless-first-command-driven-ui.md)
> and [ADR-008 (mission-editor validation engine)](../architecture/adr-008-mission-editor-validation-engine.md).
> Everything here is **presentation-only** and pure — no sim state, no `DecisionLog`, no RNG, no
> wall-clock — so it runs under plain `dotnet test` and cannot perturb the replay hash
> (`17144800277401907079`). The flat scenario browse has a sibling for multi-scenario
> **campaigns** ([`CampaignLibraryLister`](../../src/ProjectAegis.Data/Scenario/Campaign/CampaignLibraryLister.cs),
> CMD-27.12); both are surfaced by the same Unity host.

---

## Where it lives

| Type | Assembly / folder | Kind | Role |
|------|-------------------|------|------|
| `ScenarioLibraryEntry` | `Data/Scenario/` | `sealed record` | One browse row: metadata + `Available` + `UnavailableReason`. |
| `ScenarioLibraryReasons` | `Data/Scenario/` | `static` consts | The stable pre-load feasibility reason codes. |
| `ScenarioLibraryLister` | `Data/Scenario/` | `static` | Enumerate a directory → sorted `ScenarioLibraryEntry` list. |
| `ScenarioLibraryProjection` | `Data/Scenario/` | `static` | Project one document/path → `ScenarioLibraryEntry` (+ feasibility). |
| `ScenarioLibraryApplyState` | `Delegation/Projection/` | `static` | Fold entries → bindable list + preview presentation. |
| `ScenarioLibraryDisplayRow` / `ScenarioLibraryPresentation` | `Delegation/Projection/` | `sealed record` | List-side bind bundle. |
| `ScenarioLibraryPreviewPresentation` | `Delegation/Projection/` | `sealed record` | Preview-pane fields (or zero-state). |
| `ScenarioLibraryPanelHost` | `unity/…/Runtime/` | `MonoBehaviour` | UI-Toolkit consumer (binds scenario **and** campaign lists). |

The data layer (`ProjectAegis.Data`) does the IO + feasibility; the presentation layer
(`ProjectAegis.Delegation`) does the string formatting. Neither references `UnityEngine`.

---

## The row model — `ScenarioLibraryEntry`

```csharp
public sealed record ScenarioLibraryEntry(
    string ScenarioId, string Title, string PolicyId, string TlBranch, ulong Seed,
    string Location, string Year, string ProvenanceLabel,
    bool Available, string? UnavailableReason,
    string Difficulty, string Complexity, string SourcePath);
```

- **`ScenarioId`** — derived from the file name with a **first-extension strip only**
  (`ScenarioIdFromPath`, matching `ScenarioPackageLoader`), so `alpha.scenario.json → "alpha.scenario"`.
- **Metadata** (`Title`, `PolicyId`, `TlBranch`, `Seed`) is read from the document's
  `metadata` block ([`ScenarioMetadataDto`](../../src/ProjectAegis.Data/Scenario/Authoring/)); `TlBranch`
  is normalized via `CatalogTlTier.Normalize`. Absent fields render as the `MissingLabel` em-dash `"—"`.
- **`ProvenanceLabel`** — `"user"` / `"ai"` / `"import"` when the metadata `author` matches one of
  those, else the default `"authored"` (`DefaultProvenance`). Provenance is not yet a first-class
  metadata field, so this is a hint, not authority.
- **`Location`, `Year`, `Difficulty`, `Complexity`** are reserved columns (CMD-27) and currently
  emit `"—"` — the schema is in place for later population.
- **`Available` / `UnavailableReason`** — the pre-load feasibility verdict (below).

### Feasibility reason codes — `ScenarioLibraryReasons`

| Code | Raised when |
|------|-------------|
| `FILE_UNREADABLE` | Path missing / IO / access failure loading the document. |
| `SCHEMA_ERROR` | JSON parse / `InvalidDataException` / null document. |
| `DB_MISMATCH` | A catalog is installed and the document's `dbRef` / `dbSnapshotId` does not resolve (`ICatalogReader.TryResolveDbRef`), or validation surfaced a `DB_MISMATCH` finding. |
| `BROKEN_REF` | Validation (or the no-catalog heuristic) found an unresolved `ref:`-prefixed target unit id. |
| `VALIDATION_BLOCKED` | The validation engine reported `Error`-severity findings (any other than the two specific codes above), or validation itself threw. |

A row is **available** exactly when `UnavailableReason is null`.

---

## Enumeration — `ScenarioLibraryLister.ListFromDirectory`

```csharp
IReadOnlyList<ScenarioLibraryEntry> ListFromDirectory(
    string scenariosDir,
    ICatalogReader? catalog = null,
    IScenarioValidationEngine? validation = null,
    ValidationConfig? validationConfig = null);
```

- **Patterns.** Recursively (`AllDirectories`) collects `*.scenario.json` and `*.aegis-scenario`,
  plus plain `*.json` **only** under `examples/` or `validation/` folders or named `golden_*` (the
  repo's validation fixtures). `*.policy.json` (sim policies), `*.schema.json`, and
  `scenario-policy-ids.md` are **explicitly excluded** — policies are authored via
  [`scenario-policy-authoring.md`](scenario-policy-authoring.md), not browsed here.
- **IO-tolerant.** `IOException` / `UnauthorizedAccessException` on any subtree is swallowed; the
  lister continues with whatever it has. A `HashSet` (ordinal, case-insensitive) dedupes paths that
  match more than one pattern.
- **Deterministic order.** Entries are sorted by `ScenarioId` then `SourcePath`, both
  `StringComparer.Ordinal` — never filesystem/enumeration order. Same directory → same list.
- **Never throws on one bad file.** Each path goes through `ProjectFromPath`, which converts any
  per-file failure into an *unavailable* row (see below) rather than aborting the whole listing.
- An empty/whitespace/nonexistent directory returns `Array.Empty<…>()`.

The directory itself is resolved by
[`ScenarioDataPaths.TryResolveScenariosDirectory()`](../../src/ProjectAegis.Data/Scenario/ScenarioDataPaths.cs)
(a walk-up search for `data/scenarios`), so hosts and tests do not hard-code paths.

---

## Projection & feasibility — `ScenarioLibraryProjection`

Three entry points, all pure and **fail-soft**:

| Method | Use |
|--------|-----|
| `Project(scenarioId, document, sourcePath, catalog?, validation?, config?)` | Project an already-loaded `ScenarioDocumentDto` (tests / in-memory packages). |
| `ProjectFromPath(path, catalog?, validation?, config?)` | Load from disk then project; catches `Json` / `InvalidData` / `IO` / `Unauthorized` as the matching unavailable code. |
| `ProjectUnavailable(scenarioId, sourcePath, reason)` | Build an unavailable row directly (all metadata → `"—"`). |

`EvaluateFeasibility(document, catalog?, validation?, config?)` returns the reason (or `null`):

1. **Null document →** `SCHEMA_ERROR`.
2. **Catalog binding.** If a `catalog` is supplied and the metadata `dbRef` (falling back to
   `dbSnapshotId`) is non-empty but `TryResolveDbRef` fails → `DB_MISMATCH`.
3. **Light validation.** If both `catalog` **and** `validation` are supplied, run
   `validation.Validate(document, catalog, config)`. On failure, prefer the specific `DB_MISMATCH` /
   `BROKEN_REF` finding codes, else `VALIDATION_BLOCKED`. Validation throwing is itself caught and
   downgraded to `VALIDATION_BLOCKED` — feasibility never crashes a row.
4. **No catalog.** Without a catalog it still runs a cheap `HasBrokenRef` heuristic (a mission
   `ref:<unit>` target with no matching assigned unit) → `BROKEN_REF`.

> **Progressive fidelity.** Callers choose how deep to check: pass nothing for a fast metadata-only
> list, a `catalog` for DB-binding checks, or `catalog + validation` for full pre-load validation.
> The full [validation engine](../../src/ProjectAegis.Data/Validation/) (ADR-008) is the authority;
> this seam only surfaces its `Error`-severity verdict as a browse-row availability flag.

---

## Presentation — `ScenarioLibraryApplyState`

The headless apply path a UI host binds verbatim (no re-formatting in the view):

- **`Apply(entries)` → `ScenarioLibraryPresentation`** (`Rows`, `Lines`, `Count`; `Empty` for a
  null/empty list). Each row's `FormatRowLine` is:
  - available → `"{Title}  [available]"`
  - unavailable → `"{Title}  [unavailable: {reason}]"` (reason stated **on the row**, CMD-27.7).
- **`ApplyPreview(selected?)` → `ScenarioLibraryPreviewPresentation`** — the selected row's fields
  (`ID:`, availability, `Policy:`, `TL:`, `Seed:`, location/year, `Provenance:`, difficulty,
  complexity, source path). A `null` selection returns the **zero-state**:
  `"Select a scenario to preview title, determinism metadata, and pre-load feasibility."`
  (CMD-27.6).

```text
Alpha Strike  [available]
Zulu Patrol   [unavailable: DB_MISMATCH]
```

---

## Consumer — `ScenarioLibraryPanelHost` (Unity)

[`ScenarioLibraryPanelHost`](../../unity/ProjectAegis/Assets/Scripts/Runtime/ScenarioLibraryPanelHost.cs)
is a UI-Toolkit `MonoBehaviour` (`#if UNITY_5_3_OR_NEWER`) that binds this subsystem and the
sibling **campaign** library in one panel. On enable / reload it:

1. resolves `data/scenarios` via `ScenarioDataPaths.TryResolveScenariosDirectory()` (and
   `TryResolveCampaignsDirectory()` for campaigns),
2. calls `ScenarioLibraryLister.ListFromDirectory(dir, catalog)`,
3. folds via `ScenarioLibraryApplyState.Apply(...)` and binds the `ListView` to
   `Presentation.Lines`,
4. on selection calls `ApplyPreview(...)` and binds the preview labels; the preview pane shows the
   scenario fields, the campaign fields, or the appropriate zero-state instruction.

The host holds only presentation state (last entries / presentation / preview) and never touches the
sim — selection is a pure `ApplyPreview` recompute.

---

## Determinism & invariants

- **Presentation-only.** Inputs are a directory of documents (+ optional catalog/validation);
  outputs are records/strings. Nothing reads or writes the `DecisionLog` or sim state → it cannot
  move the replay hash `17144800277401907079`. *(Boundary cites ADR-010 / ADR-008 — not ADR-018.)*
- **Deterministic.** Ordinal sort by `(ScenarioId, SourcePath)`; no RNG, no wall-clock; identical
  inputs → identical list.
- **Fail-soft, never-throw.** One unreadable/invalid file becomes an unavailable row, not an
  exception; unreadable subtrees are skipped.
- **No engine dependency.** The data + presentation types have no `UnityEngine` reference, so they
  are CI-safe and headless-testable; only `ScenarioLibraryPanelHost` is Unity-gated.
- **Read-only feasibility.** `EvaluateFeasibility` binds the catalog and *reads* validation output;
  it never writes the catalog or mutates the document.

---

## Extend it

| You want to… | Do this |
|--------------|---------|
| Populate `Location` / `Year` / `Difficulty` / `Complexity` | Add the fields to `ScenarioMetadataDto` + read them in `ScenarioLibraryProjection.Project`; the record columns and preview lines already exist. |
| Add a browsable document extension | Add the glob to `ScenarioLibraryLister.ScenarioDocumentPatterns` (keep policy/schema files excluded). |
| Add a new feasibility reason | Add a const to `ScenarioLibraryReasons` and raise it from `EvaluateFeasibility`; the row line + preview render it automatically. |
| Change a row / preview string | Edit `ScenarioLibraryApplyState` only — formatting lives there, not in the host or the data layer. |
| Browse multi-scenario campaigns | Use the sibling [`CampaignLibraryLister`](../../src/ProjectAegis.Data/Scenario/Campaign/CampaignLibraryLister.cs) (CMD-27.12); the same host binds both. |

---

## See also

| Doc | For |
|-----|-----|
| [campaign-library-runtime.md](campaign-library-runtime.md) | The sibling **campaign** artifact class (`CampaignLibraryLister`, CMD-27.12) — ordered scenario progression browsed by the same Unity host. |
| [scenario-document-authoring.md](scenario-document-authoring.md) | The `*.scenario.json` document / `ScenarioDocumentDto` model these rows read. |
| [scenario-authoring-host.md](scenario-authoring-host.md) | The interactive edit host that opens a browsed scenario for authoring. |
| [scenario-policy-authoring.md](scenario-policy-authoring.md) | `*.policy.json` sim policies — deliberately excluded from this browse list. |
| [mission-editor-cli.md](mission-editor-cli.md) | The headless CLI that validates/simulates the same documents. |
| [determinism-and-replay.md](determinism-and-replay.md) | Why the read path must stay pure. |

## Tests

| Test | Assembly (framework) | Pins |
|------|----------------------|------|
| `ScenarioLibraryProjectionTests` | `ProjectAegis.Data.Tests/Scenario/` (xUnit) | Directory listing sorted by `ScenarioId`; `SCHEMA_ERROR` on bad JSON; one bad file does not throw; `DB_MISMATCH` on unresolved `dbRef`; available on resolvable Baltic `dbRef`; `VALIDATION_BLOCKED` on engine errors; `FILE_UNREADABLE` on missing file; repo-examples smoke via `ScenarioDataPaths`. |
| `ScenarioLibraryApplyStateTests` | `ProjectAegis.Delegation.Tests/Projection/` (NUnit) | `[available]` / `[unavailable: DB_MISMATCH]` row lines; null preview → zero-state instruction; selected preview fills metadata fields. |
