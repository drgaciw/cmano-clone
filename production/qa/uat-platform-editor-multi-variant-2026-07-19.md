# Platform Editor UAT — multi-variant (2026-07-19)

**Orchestration:** 3 parallel agents (xlsx pipeline · catalog/write-gate · automated Platform tests) + TDD on P0 empty-export/browse.

**Surfaces:** CLI `platform_*` / `catalog_*` verbs + Unity `PlatformCatalogViewerHost` / `PlatformImportPanelHost` (UI needs PanelSettings already fixed).

## Variations

| Variation | Source | Outcome |
|-----------|--------|---------|
| Baltic SQLite catalog (79 platforms) | `assets/data/catalog/baltic_patrol.db` | surface 35 / air 24 / subsurface 20 |
| Export without `--snapshot` (pre-fix) | empty silent ok | **P0 fixed** → defaults `baltic_patrol`, 79 rows |
| Export with `--snapshot baltic_patrol` | full sheets | PASS |
| Dual IO closedxml / canonical | parity | PASS `diffCount: 0` |
| Identical re-export diff | empty delta | PASS |
| Cell edit MaxHp / BasePd | `diffCount: 1` | PASS |
| Import unedited / edited Baltic xlsx | blocked | **400 magazine capacity Errors** residual |
| Speculative platforms JSON (GTL 4–5) | `data/catalog/speculative_platforms.json` | metadata only — **not** in SQLite/xlsx path |
| Near-future archetypes | `data/catalog/near_future_archetypes.json` | not Excel-importable today |
| Catalog write propose/approve | isolated DB copy | PASS |
| Catalog intelligence / graphs / links | Baltic DB | PASS |
| OSINT staging review | empty pending | PASS |

## Automated tests (pre-fix baseline)

| Suite | Passed | Failed |
|-------|-------:|-------:|
| Data.Tests ~Platform | 185 | 0 |
| Cli.Tests Platform/CatalogPlatform/CatalogRelease | 11 | 0 |
| UnityAdapter.Tests ~Platform | 58 | 0 |

Post-fix browse/export tests: **4/4 PASS**.

## Bugs fixed (TDD)

| ID | Issue | Fix |
|----|-------|-----|
| BUG-PX-01/02 | `catalog_platform_browse` + default `platform_export_xlsx` silent empty (`snapshotResolved: false`) | `PlatformCatalogExportResolver` defaults empty snapshot → `baltic_patrol`; export command same default; `ok:false` + exit 1 if still unresolved |
| Tests | Real Baltic DB browse + export without snapshot | `CatalogPlatformBrowseCommandTests` |

## Residual (not fixed this pass)

| ID | Severity | Notes |
|----|----------|-------|
| BUG-PX-03/04 | P0 data/validation | Magazine qty > mount capacity → 400 Errors → import never proposes/stages. Needs capacity model fix or data quarantine — separate story. |
| Phase B sheets empty | P2 data | Mobility/Signatures/Emcon rows 0 in Baltic drop |
| Speculative/future platforms | P3 product | Not in workbook pipeline; Sim metadata + near_future archetypes only |
| Browse schema no domain | P2 | Type breakdown only via DB `platform.domain` |

## Evidence logs

- `production/qa/uat-platform-xlsx-variants-2026-07-19.log`
- `production/qa/uat-platform-catalog-variants-2026-07-19.log`
- `production/qa/uat-platform-tests-2026-07-19.log`
- `production/qa/tdd-platform-browse-export-2026-07-19.log`

## Operator smoke (after pull)

```bash
dotnet run --project src/ProjectAegis.MissionEditor.Cli -c Release -- \
  catalog_platform_browse --db assets/data/catalog/baltic_patrol.db --max-records 5
# expect rowCount=5

dotnet run --project src/ProjectAegis.MissionEditor.Cli -c Release -- \
  platform_export_xlsx --db assets/data/catalog/baltic_patrol.db --out /tmp/p.xlsx --io canonical
# expect platformCount=79, snapshotResolved=true
```
