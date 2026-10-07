# Editor verb honesty (Doc 11 / Doc 21)

> **S125-05 / AUTH-03.** Every Platform Editor (PE), Mission Editor (ME) and catalog-tool verb names what
> it actually does to the catalog write gate (`IWriteGate` / `CatalogWriteGate`, ADR-006, extend-only).
> Code source of truth: `src/ProjectAegis.Delegation/Projection/EditorVerbCatalog.cs`. Enforced by
> `src/ProjectAegis.Delegation.Tests/Projection/EditorVerbCatalogTests.cs`, which checks this table, the
> `IWriteGate` surface, the shipped UXML button text, the CLI `case` switch and the gate call sites.

Rules the test enforces:

- **Propose** only stages pending batches (`Propose*Batch`); it never approves.
- **Approve** is the only verb that commits (`ApproveBatch`); **Reject** discards staging (`RejectBatch`).
- **Save** writes the scenario draft only — no write gate, no export, allowed with blocking findings (AME-6.5).
- **Export** is either a read-only workbook export (PE) or the validated scenario artifact (ME); neither
  touches the write gate. ME export writes `<draft>.export.json` only when the validation gate passes.
- Every mutating `IWriteGate` operation is reachable through at least one verb listed here.

<!-- verbs:start -->
| Verb id | UI label | CLI verb | Gate operation | Effect |
|---------|----------|----------|----------------|--------|
| `pe.export_workbook` | Export | `platform_export_xlsx` | None | ReadOnly |
| `pe.diff_workbook` | Diff | `platform_diff_xlsx` | None | ReadOnly |
| `pe.propose` | Propose | `platform_import_xlsx` | Propose | StagedBatch |
| `pe.approve` | Approve | `catalog_write_approve` | Approve | CommittedBatch |
| `pe.reject` | Reject | — | Reject | DiscardedBatch |
| `catalog.write_propose` | Propose | `catalog_write_propose` | Propose | StagedBatch |
| `catalog.import_markdown` | Propose | `catalog_import_markdown` | Propose | StagedBatch |
| `catalog.osint_pending` | List pending | `osint_staging_review` | ListPending | ReadOnly |
| `catalog.osint_approve` | Approve | `osint_staging_review` | Approve | CommittedBatch |
| `me.save` | Save draft | — | None | DraftFile |
| `me.export` | Export validated | `scenario_export` | None | ValidatedArtifact |
| `me.publish` | Publish | `scenario_publish` | None | ValidatedArtifact |
| `me.sample` | Sample | `scenario_simulate_sample` | None | ReadOnly |
<!-- verbs:end -->

Known naming gaps (recorded, not renamed — renaming shipped CLI verbs is out of S125 scope):

- `platform_import_xlsx` reads "import" but only **proposes**; its JSON output already says
  `nextStep = catalog_write_approve`. The PE button is labelled **Propose**.
- `pe.reject` has no CLI verb; rejection is reachable from the PE Import pane only.
- The ME shell UXML button text is `Save`; hosts bind `EditorSaveExportProjection.SaveLabel`
  ("Save draft") at runtime. No Export button exists in ME chrome yet — `scenario_export` is CLI-only.
