# Scenario library browse — flat scenario listing & pre-load feasibility

> **Scope.** The pure, headless **scenario library** subsystem (CMD-27): how a host turns a
> directory of scenario documents into a browsable list with per-row **pre-load feasibility**, and
> a bindable preview pane — `ScenarioLibraryLister` + `ScenarioLibraryProjection` (in
> `ProjectAegis.Data/Scenario/`) and `ScenarioLibraryApplyState` (in
> `ProjectAegis.Delegation/Projection/`). It answers "*which scenarios can I load right now, and
> why not?*" **before** a scenario is opened. It is the **flat-scenario** counterpart of the
> campaign-progression browse (`ProjectAegis.Data/Scenario/Campaign/`); this page covers the flat
> list only.
>
> The scenario **document** model it reads (`*.scenario.json`, `ScenarioDocumentDto`) is
> [`scenario-document-authoring.md`](scenario-document-authoring.md); the interactive editing host
> is [`scenario-authoring-host.md`](scenario-authoring-host.md); the validation engine used for the
> optional feasibility check is `IScenarioValidationEngine`
> ([ADR-008](../architecture/adr-008-mission-editor-validation-engine.md)).
>
> Everything here is **presentation / read-only** and engine-agnostic — no `UnityEngine`, no sim
> mutation — so it runs and is pinned under plain `dotnet test`. The Unity library screen is only a
> consumer that binds the presentation records.

---

## Where it lives

| Type | Assembly / folder | Kind | Role |
|------|-------------------|------|------|
| `ScenarioLibraryEntry` | `ProjectAegis.Data/Scenario/` | `sealed record` | One browse row: id, title, policy, TL branch, seed, location/year, provenance, `Available` + `UnavailableReason`, difficulty/complexity, source path. |
| `ScenarioLibraryReasons` | `ProjectAegis.Data/Scenario/` | `static` | Stable pre-load reason codes: `FILE_UNREADABLE`, `DB_MISMATCH`, `BROKEN_REF`, `VALIDATION_BLOCKED`, `SCHEMA_ERROR`. |
| `ScenarioLibraryLister` | `ProjectAegis.Data/Scenario/` | `static` | Enumerates scenario documents under a directory → sorted `ScenarioLibraryEntry` list. |
| `ScenarioLibraryProjection` | `ProjectAegis.Data/Scenario/` | `static` | Projects one document/path → `ScenarioLibraryEntry`, incl. `EvaluateFeasibility`. |
| `ScenarioLibraryApplyState` | `ProjectAegis.Delegation/Projection/` | `static` | Folds entries → bindable list (`ScenarioLibraryPresentation`) + preview pane (`ScenarioLibraryPreviewPresentation`). |

---

## The browse pipeline

```text
ScenarioDataPaths.TryResolveScenariosDirectory()   (host resolves the dir)
        │
        ▼
ScenarioLibraryLister.ListFromDirectory(dir, catalog?, validation?, config?)
   enumerate *.scenario.json + *.aegis-scenario (recursive)
   + plain *.json under examples/ | validation/ | golden_*.json
   − exclude *.policy.json, *.schema.json, scenario-policy-ids.md
        │  per path
        ▼
ScenarioLibraryProjection.ProjectFromPath(path, …) → ScenarioLibraryEntry
        │
        ▼
   OrderBy(ScenarioId, Ordinal).ThenBy(SourcePath, Ordinal)   ← deterministic
```

- **Directory resolution is a host concern.** `ListFromDirectory` takes an explicit directory;
  hosts typically resolve it via `ScenarioDataPaths.TryResolveScenariosDirectory()`. A missing /
  non-existent directory returns an **empty list**, never throws.
- **Only authoring documents.** Sim policy files (`*.policy.json`) and schemas (`*.schema.json`)
  are intentionally excluded — they are not scenarios. Validation fixtures under `examples/` /
  `validation/` and `golden_*.json` are included so the library can browse the test corpus too.
- **One bad file never breaks the list.** Unreadable subtrees are skipped; a single malformed file
  becomes an *unavailable row* (below), not an exception.
- **Deterministic order.** Rows are sorted by `ScenarioId` then `SourcePath`, both
  `StringComparer.Ordinal` — never filesystem enumeration order.

---

## Pre-load feasibility — `EvaluateFeasibility`

The load-bearing part: each row is marked `Available` (`UnavailableReason == null`) or carries a
**stable reason code** so the UI can say *why* a scenario can't be loaded yet. The check is
**fail-closed** — anything it can't positively clear is surfaced as unavailable rather than
silently loadable.

| Condition (in order) | Result |
|----------------------|--------|
| Null / unparseable document | `SCHEMA_ERROR` |
| IO / access failure loading the file | `FILE_UNREADABLE` |
| A catalog is supplied **and** the doc's `dbRef` / `dbSnapshotId` doesn't resolve (`catalog.TryResolveDbRef`) | `DB_MISMATCH` |
| A catalog **and** an `IScenarioValidationEngine` are supplied and validation reports `Error` findings | `DB_MISMATCH` / `BROKEN_REF` when those specific codes are present, else `VALIDATION_BLOCKED`; a thrown validator is caught → `VALIDATION_BLOCKED` |
| No catalog supplied, but a mission `TargetIds` `ref:<unit>` points at a unit absent from any mission's `AssignedUnitIds` | `BROKEN_REF` |
| Otherwise | `null` → **Available** |

`ProjectFromPath` wraps all IO/parse in try/catch (`JsonException` / `InvalidDataException` →
`SCHEMA_ERROR`; `IOException` / `UnauthorizedAccessException` → `FILE_UNREADABLE`; any other →
`FILE_UNREADABLE`) so the enumerator can never crash on one row.

**Provenance** is resolved from `Metadata.Author` when it is one of `user` / `ai` / `import`
(lower-cased), else defaults to `authored`. Missing metadata fields render as the `—` placeholder;
`Title` falls back to the scenario id, which itself is the first-extension-stripped file name
(matching `ScenarioPackageLoader`).

---

## Presentation — `ScenarioLibraryApplyState`

Pure fold from entries to bindable records; hosts bind the strings without reformatting.

| Method | Returns |
|--------|---------|
| `Apply(entries)` | `ScenarioLibraryPresentation(Rows, Lines, Count)`; empty/null → `Empty`. Each row's `DisplayLine` is `FormatRowLine`. |
| `FormatRowLine(entry)` | `"<Title>  [available]"` or `"<Title>  [unavailable: <REASON>]"` — the reason is on the row itself (CMD-27.7). |
| `ApplyPreview(selected)` | `ScenarioLibraryPreviewPresentation`; `null` selection → `ZeroState` with the CMD-27.6 instruction *"Select a scenario to preview title, determinism metadata, and pre-load feasibility."* Otherwise per-field lines (id, availability, policy, TL, seed, location/year, provenance, difficulty, complexity, source path). |

The determinism metadata surfaced in the preview (`PolicyId`, `TlBranch`, `Seed`) is exactly what a
loader needs to reproduce a run — the library is the pre-load face of the
[determinism contract](determinism-and-replay.md).

---

## Consumer & determinism

- **Host-side only, no CLI verb.** Browsing is a presentation concern; there is no
  `scenario_library` Mission-Editor CLI verb (unlike the authoring/validation verbs in
  [`mission-editor-cli.md`](mission-editor-cli.md)). A Unity library screen binds
  `ScenarioLibraryPresentation.Lines` / the preview fields.
- **Read-only / replay-safe.** Nothing here reads or writes the sim, the order log, or the catalog
  write gate — it reads scenario files (and optionally a read-only catalog for the db-ref check).
  It cannot perturb the replay hash (`17144800277401907079`). *(Presentation boundary: ADR-010 /
  ADR-008 — not ADR-018.)*
- **IO-tolerant & deterministic.** Never throws on a bad file/subtree; ordinal sort gives a stable
  list for identical inputs.
- **Fail-closed feasibility.** Ambiguity (unparseable, unresolved db-ref, validator error/throw)
  becomes an explicit unavailable reason, never a silently-loadable row.

---

## Extend it

| You want to… | Do this |
|--------------|---------|
| Add a new unavailable reason | Add a constant to `ScenarioLibraryReasons` and return it from `EvaluateFeasibility`; surface it through `FormatRowLine` (it already renders any reason). |
| Surface a new preview field | Add it to `ScenarioLibraryEntry` (populate in `ScenarioLibraryProjection.Project`) and to `ScenarioLibraryPreviewPresentation` + `ApplyPreview`. |
| Recognise a new scenario file extension | Extend `ScenarioLibraryLister.ScenarioDocumentPatterns` (keep policy/schema exclusions intact). |
| Tighten catalog/validation gating | Pass a catalog + `IScenarioValidationEngine` into `ListFromDirectory`; without them the lister still does the no-catalog `ref:` broken-ref scan. |

---

## See also

| Doc | For |
|-----|-----|
| [scenario-document-authoring.md](scenario-document-authoring.md) | The `*.scenario.json` document model these rows read. |
| [scenario-authoring-host.md](scenario-authoring-host.md) | The interactive editing host (the write side of the same documents). |
| [mission-editor-cli.md](mission-editor-cli.md) | The headless authoring/validation CLI verbs (browse has none). |
| [catalog-seeding.md](catalog-seeding.md) | The `ICatalogReader` used for the `dbRef` feasibility check. |
| [c2-projection-layer.md](c2-projection-layer.md) | The wider read-only projection layer this apply-state belongs to. |
| [determinism-and-replay.md](determinism-and-replay.md) | Why policy/TL/seed metadata is the pre-load determinism face. |

## Tests

| Test | Pins |
|------|------|
| `ScenarioLibraryProjectionTests.ListFromDirectory_temp_dir_with_two_fixtures_is_sorted_by_scenario_id` (`ProjectAegis.Data.Tests`, xUnit) | Deterministic ordinal sort. |
| `ScenarioLibraryProjectionTests.ProjectFromPath_schema_error_marks_unavailable` / `...missing_file_is_file_unreadable` | `SCHEMA_ERROR` / `FILE_UNREADABLE` mapping. |
| `ScenarioLibraryProjectionTests.ListFromDirectory_one_bad_file_does_not_throw_and_marks_row` | One bad file → unavailable row, no throw. |
| `ScenarioLibraryProjectionTests.Project_db_mismatch_when_catalog_cannot_resolve_dbRef` / `Project_available_when_baltic_dbRef_resolves` | `DB_MISMATCH` vs available. |
| `ScenarioLibraryProjectionTests.Project_validation_blocked_when_engine_reports_errors` | `VALIDATION_BLOCKED` from the validation engine. |
| `ScenarioLibraryProjectionTests.ListFromDirectory_repo_examples_via_ScenarioDataPaths_when_present` | Resolver-driven enumeration of the repo corpus. |
| `ScenarioLibraryApplyStateTests` (`ProjectAegis.Delegation.Tests/Projection`, NUnit) | Row-line formatting, list `Apply`, preview zero-state + fields. |
