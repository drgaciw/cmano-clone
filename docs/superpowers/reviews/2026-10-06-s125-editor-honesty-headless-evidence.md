# S125 Editor honesty — headless evidence (2026-10-06)

**Sprint:** [`production/sprints/sprint-125-editor-honesty.md`](../../../production/sprints/sprint-125-editor-honesty.md)
**QA plan:** [`production/qa/qa-plan-sprint-122-127-rebaseline-2026-09-30.md`](../../../production/qa/qa-plan-sprint-122-127-rebaseline-2026-09-30.md) (S125 row)
**Linear:** DRG-274 (AUTH-01) · DRG-236 (G3) · DRG-234
**Branch:** `cursor/s125-editor-honesty-7fee`
**Doctrine:** ADR-011 Excel-primary · no ADR-013 `.scen` parser · `CatalogWriteGate` extend-only (no gate file touched) · `DelegationBridge.cs` untouched · no editor rewrite · headless only.

## DoD freeze (W3-AUTH-01)

Frozen: 2026-10-06 — the S125 Definition of Done is the sprint file's DoD list plus this table. It is
**evidence-first**: an item is only "headless evidence landed" when the cited tests exist and pass; it is
not "Done" until the orchestrator's serial gates and owner acceptance are recorded. Scope may not be
widened or narrowed after this freeze without a dated amendment below. Enforced by
`S125EditorHonestyEvidencePinTests` (doc section present, S125-01/02 stay pending, every cited test class exists).

| DoD line (sprint file) | Status |
|------------------------|--------|
| AUTH-01/02 evidence attached to 236/274 | **Pending** — needs real Unity Editor screenshots (S125-01/02) |
| Save≠export demo | Headless evidence landed (S125-04) |
| Excel write-gate only | Headless evidence landed (S125-05 verb table + S125-06 contract); no write path added |
| W3-AUTH-01 freeze honored | This section + pin test (S125-03) |
| Shared QA plan executed, QA decision recorded | **Pending** — orchestrator serial gates + QA decision |

## Item → evidence map

| ID | Item | Status | Implementation | Tests |
|----|------|--------|----------------|-------|
| S125-01 | AUTH-01′ / DRG-274 G3 ME evidence package | PENDING — real Editor evidence (success + recovery screenshots in Unity 6000.3.22f1; cannot be produced headless) | — | — |
| S125-02 | AUTH-02 PE evidence (DRG-236 child) | PENDING — real Editor evidence (same bar for Platform Editor; file after S122.0) | — | — |
| S125-03 | W3-AUTH-01 evidence-first DoD pin | Headless evidence landed | This doc (DoD freeze section) | `S125EditorHonestyEvidencePinTests` |
| S125-04 | AUTH-04 save vs export chrome | Headless evidence landed | `ScenarioSaveExportGate` (Data), `EditorSaveExportProjection` (Delegation) | `ScenarioSaveExportGateTests`, `EditorSaveExportProjectionTests` |
| S125-05 | AUTH-03 Doc 11 / verb honesty | Headless evidence landed | `EditorVerbCatalog`, [`docs/engineering/editor-verb-honesty.md`](../../engineering/editor-verb-honesty.md) | `EditorVerbCatalogTests` |
| S125-06 | AUTH-08 workbook contract CI + silent-drop counts | Headless evidence landed | `PlatformWorkbookContract`, `PlatformImportDropCounts`, importer `ContractDrift`/`DropCounts`, CLI `dropCounts`, [`docs/engineering/platform-workbook-contract.md`](../../engineering/platform-workbook-contract.md) | `PlatformWorkbookContractTests` |
| S125-07 | AUTH-12 dbRef / snapshot binding visible | Headless evidence landed | `EditorCatalogBindingProjection` | `EditorCatalogBindingProjectionTests` |
| S125-08 | AUTH-13 + AUTH-09 quarantine UX + LatLon messaging | Headless evidence landed | `PlatformImportQuarantineProjection`, `ScenarioLatLonDiagnostics`, `PlatformWorkbookLatLonDiagnostics` | `PlatformImportQuarantineProjectionTests`, `ScenarioLatLonDiagnosticsTests`, `PlatformWorkbookContractTests` |

## What each item proves

- **S125-04 Save ≠ Export.** `ScenarioSaveExportGate.SaveDraft` persists an invalid draft
  (`STRIKE_NO_TARGETS`), reports the blocking count, and writes no `<draft>.export.json`.
  `ScenarioSaveExportGate.Export` runs `ScenarioExportCommand.Prepare` (TeleportUnit transform + export
  gate), returns the blocking findings and writes nothing while blocked; when allowed it writes a
  separate artifact and leaves the draft byte-identical; it refuses to overwrite the draft path.
  `EditorSaveExportProjection` gives the chrome distinct labels (`Save draft` / `Export validated`),
  tooltips, USS classes and status text; Save stays enabled while Export is blocked.
- **S125-05 Verb honesty.** `EditorVerbCatalog` maps every PE/ME/catalog verb to its real write-gate
  operation. Tests check: declared methods exist on `IWriteGate` and match the operation class; every
  mutating `IWriteGate` method has a verb; labels start with the verb the gate performs (Propose /
  Approve / Reject; Save never touches the gate); shipped UXML button text uses the same verb word; CLI
  verbs exist in `Program.cs`; source call sites (`PlatformWorkbookImporter`, CLI commands, markdown
  proposer, session save) call exactly the declared gate methods; `me.export` / `scenario_export` is
  ReadOnly (JSON summary from `ScenarioExportCommand.Prepare`, no `ScenarioSaveExportGate.Export`, no
  artifact file); a recording gate proves Propose never approves at runtime; and the documented table
  matches the code, including effect.
- **S125-06 Workbook contract CI.** `PlatformWorkbookContract` is the code contract (schema `010`),
  now also driving the importer's stageable classification. CI fails if the exporter's sheets/columns,
  the importer's per-column stageability, or the documented table drift. `CheckDrift` reports
  missing/unknown sheets and columns and column reorders. `PlatformImportDropCounts` counts unknown-sheet
  rows, header-drift rows, removed rows, quarantined rows, unknown-column fields, overflow fields and
  edits to non-stageable columns; staging appends the summary to notes and the `platform_import_xlsx`
  JSON (`dropCounts`, `contractDriftCount`, `quarantineCount`).
- **S125-07 Binding visible.** `EditorCatalogBindingProjection` shows `dbRef · snapshot · TL` for a
  scenario (optionally resolved through `ICatalogReader.TryResolveDbRef`), the workbook `_Meta`
  `SourceSnapshotId`, and turns unbound / unresolved / mismatched bindings into explicit states.
- **S125-08 Quarantine + LatLon.** `PlatformImportQuarantineProjection` labels each quarantined row,
  adds a fix-it hint per reason code, groups counts by reason, carries the silent-drop summary and
  states that quarantined rows are never proposed. `ScenarioLatLonDiagnostics` gives one actionable
  message (value, range, sign convention, decimal example) for out-of-range / non-finite / non-decimal
  input; ORBAT upsert, **move and clone** (previously unchecked) now throw `ScenarioLatLonException`
  with a stable code that the command bus and CLI surface. `PlatformWorkbookLatLonDiagnostics` raises the
  non-blocking `PLE-PLT-LATLON` warning for bad `Platforms.LatDeg/LonDeg` cells. Numeric checks only — no parser.

## Targeted commands (RUN + READ)

```bash
export PATH="$HOME/.dotnet:$PATH"
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj -m:1 \
  --filter "FullyQualifiedName~PlatformWorkbookContractTests|FullyQualifiedName~ScenarioLatLonDiagnosticsTests|FullyQualifiedName~ScenarioSaveExportGateTests"
dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj -m:1 \
  --filter "FullyQualifiedName~EditorSaveExportProjectionTests|FullyQualifiedName~EditorVerbCatalogTests|FullyQualifiedName~EditorCatalogBindingProjectionTests|FullyQualifiedName~PlatformImportQuarantineProjectionTests|FullyQualifiedName~S125EditorHonestyEvidencePinTests"
```

Results are recorded in the PR body. Full solution gates (build 0/0, ≥1638 tests, ReplayGolden 6/6,
PlayModeSmoke ≥20/20, hash `17144800277401907079`) are run serially by the orchestrator.

## Out of scope / residual

- S125-01 / S125-02 need real Unity Editor screenshots; no headless substitute is claimed.
- No Export button exists in ME UXML chrome yet; hosts bind `EditorSaveExportProjection` labels. UXML/scene
  edits were not made headless.
- Shipped CLI verb names (`platform_import_xlsx`) are documented, not renamed.
- **Follow-up:** `ScenarioSaveExportGate.SaveDraft` with no bound catalog uses
  `ScenarioDocumentEditor.LiveValidate()`, which always validates against
  `InMemoryCatalogReader.BalticPatrolFixture()`. Non-Baltic drafts can show a false blocking count.
  Not changed in this pass (see `docs/engineering/editor-verb-honesty.md`).
