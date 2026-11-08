# Sprint 23 GitNexus Re-index — 2026-06-17

**Repo:** `/home/username01/cmano-clone/cmano-clone`  
**Branch:** `stack/sprint23/closeout-gitnexus`  
**Command:** `npx gitnexus analyze --force`  
**Duration:** 18.7s  
**Indexed commit:** `d340436` (stack tip pre-closeout commit; trunk `main` @ `f81f1f9`)

## Graph stats

| Metric | Value |
|--------|-------|
| Nodes | 9,280 |
| Edges | 19,007 |
| Clusters | 229 |
| Flows | 300 |

**Prior baseline (Sprint 22 @ `34d4e6f`):** 8,671 nodes / 17,891 edges — **+609 nodes / +1,116 edges** after Sprint 23 stack.

## Status

```
Repository: cmano-clone
Indexed: 2026-06-17
Status: up-to-date @ d340436
```

## detect_changes — stack vs `main` (scope=compare)

| Metric | Value |
|--------|-------|
| Changed symbols | 337 |
| Changed files | 64 |
| Affected processes | 72 |
| Risk level | **CRITICAL** |

### Sprint touch-set blast radius (impact upstream)

| Symbol | Risk | Direct deps | Processes | Notes |
|--------|------|-------------|-----------|-------|
| `CatalogWriteGate` | **CRITICAL** | 34 | 13 | ApproveBatch multi-entity + CatalogSortKey sort delegation (S23-04/06); extend-only verified by 68 scoped tests |
| `PlatformWorkbookExporter` | **HIGH** | 10 | 3 | Phase B stub sheets Signatures/Mobility/Emcon (S23-05); CLI export/import/diff paths |
| `IPlatformWorkbookIo` | **HIGH** | 2 | 3 | ClosedXML adapter + `PlatformWorkbookIoSelection` (S23-01); LOW regression in S23-01 story-done |
| `DoctrineInheritancePanelHost` | **LOW** | 0 | 0 | UXML/USS wiring only; presentation seam |
| `DelegationBridge` | **N/A** | — | — | **ZERO** file touches vs `main` (S23-03 ADR-010 compliance) |

### High-blast-radius changed symbols (stack diff sample)

- `CatalogWriteGate` — `ApproveBatch`, `Propose*Batch`, `CatalogSortKeyComparer` delegation, `StagingBatchContent`
- `CatalogSortKeyComparer` / `CatalogSortKeyGoldenHashes` — new determinism layer (S23-06)
- `PlatformWorkbookExporter` — Phase B sheet builders + meta schema `008`
- `PlatformWorkbookIoSelection` / `ClosedXmlPlatformWorkbookIo` — binary `.xlsx` I/O (S23-01)
- `DoctrineInheritancePanelHost` / `DelegationSmokeSceneBuilder` — doctrine panel assets (S23-03)
- CLI: `PlatformExportXlsxCommand`, `PlatformImportXlsxCommand`, `PlatformDiffXlsxCommand`

### Affected process families (stack vs main)

- `OnApproveSelected → ApproveBatch` — multi-entity commit path (S23-04)
- `Run → StageFromFile → Propose*Batch` — platform workbook import staging
- `RunCatalogImportMarkdown → SortSensors` — CMO import ordering now uses sort-key helpers (S23-06)
- `Run → Resolve → IPlatformWorkbookIo` — ClosedXML vs canonical adapter selection (S23-01)

## Verification gates (stack tip)

| Gate | Result |
|------|--------|
| Full solution `dotnet test ProjectAegis.sln` | **538/538 PASS** |
| Sprint smoke | `production/qa/smoke-sprint-23-2026-06-17.md` |
| Doctrine headless proxy | `production/qa/sprint-23-doctrine-editor-signoff-2026-06-17.md` |

## Notes

- FTS extension unavailable (load-only policy); semantic search features limited.
- 6 large reference markdown files skipped (>512KB).
- Re-run `npx gitnexus analyze` after stack merge to `main` to refresh indexed commit to trunk HEAD.

## Next

Before further `CatalogWriteGate` edits: `gitnexus impact CatalogWriteGate direction=upstream summaryOnly=true`.  
Merge stack bottom-up (#139 → #145), then re-index @ post-merge trunk.