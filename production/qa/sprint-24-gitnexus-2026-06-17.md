# Sprint 24 GitNexus Re-index — 2026-06-17

**Repo:** `/home/username01/cmano-clone/cmano-clone`  
**Branch:** `stack/sprint24/closeout-replay-gitnexus`  
**Command:** `npx gitnexus analyze . --force`  
**Duration:** 18.3s  
**Indexed commit:** `d0fc4db` (stack tip; trunk `main` @ `00f76df`)

## Graph stats

| Metric | Value |
|--------|-------|
| Nodes | 9,723 |
| Edges | 19,973 |
| Clusters | 254 |
| Flows | 300 |

**Prior baseline (Sprint 23 @ `d340436`):** 9,280 nodes / 19,007 edges — **+443 nodes / +966 edges** after Sprint 24 stack.

## Status

```
Repository: cmano-clone
Indexed: 2026-06-17
Status: up-to-date @ d0fc4db
```

## detect_changes — stack vs `main` (scope=compare)

| Metric | Value |
|--------|-------|
| Changed symbols | 274 |
| Changed files | 48 |
| Affected processes | 53 |
| Risk level | **CRITICAL** |

### Sprint touch-set blast radius (impact upstream)

| Symbol | Risk | Direct deps | Processes | Notes |
|--------|------|-------------|-----------|-------|
| `CatalogWriteGate` | **CRITICAL** | 42 | 13 | Phase B `Propose*/ApproveBatch` extend-only (S24-03); `PlatformImportXlsxCommand` staging path |
| `PlatformWorkbookImporter` | **LOW** | 1 | 1 | Mobility/Signatures/Emcon import wiring (S24-04) |
| `PlatformWorkbookValidator` | **MEDIUM** | 14 | 0 | Phase A validator baseline; S24-05 rule pack on parallel branch |
| `ICatalogReader` | **LOW** | 4 | 0 | Phase B `TryGetSignature/Mobility/Emcon` (S24-02) |
| `DelegationBridge` | **N/A** | — | — | **ZERO** file touches vs `main` (ADR-010 compliance) |

### High-blast-radius changed symbols (stack diff sample)

- `CatalogWriteGate` — `ApproveBatch`, `Propose*Batch`, Phase B staging tables (`008`)
- `PlatformWorkbookImporter` — Mobility/Signatures/Emcon sheet parsers + round-trip golden (S24-04)
- `PlatformWorkbookExporter` — Phase B sheet builders + real snapshot resolver (S24-02)
- `ICatalogReader` / `SqliteCatalogReader` / `InMemoryCatalogReader` — Phase B read APIs
- CLI: `PlatformImportXlsxCommand`, `PlatformExportXlsxCommand`, `PlatformDiffXlsxCommand`
- Unity: `App6Sidc` glyph map (S24-07), Cesium polish checklist (S24-08)

### Affected process families (stack vs main)

- `OnApproveSelected → ApproveBatch` — Phase B multi-entity commit path (S24-03)
- `Run → PlatformWorkbook` — import/export/diff CLI verbs
- `Run → StageFromFile → Propose*Batch` — platform workbook import staging (S24-04)
- `RunCatalogImportMarkdown → SortSensors` — catalog import ordering unchanged

## Verification gates (stack tip @ `d0fc4db`)

| Gate | Result |
|------|--------|
| `ReplayGoldenSuiteTests` | **6/6 PASS** (397 ms) |
| Full solution `dotnet test ProjectAegis.sln` | **570/570 PASS** |
| Sprint smoke (day-1 baseline) | `production/qa/smoke-sprint-24-2026-06-17.md` (540/540 @ `e77696d`) |

### Per-project test counts (closeout re-run)

| Project | Passed |
|---------|--------|
| ProjectAegis.Sim.Tests | 87 |
| ProjectAegis.Delegation.Tests | 171 |
| ProjectAegis.Delegation.UnityAdapter.Tests | 95 |
| ProjectAegis.MissionEditor.Cli.Tests | 21 |
| ProjectAegis.Data.Tests | 196 |
| **Total** | **570** |

## DelegationBridge ZERO-touch check

```bash
git diff main --name-only | grep -E 'DelegationBridge\.cs$' || echo "ZERO"
# DelegationBridge.cs: ZERO touches vs main
```

Note: `DelegationBridgeHost.cs` has stack presentation changes (S24-07/08); core bridge contract file untouched.

## Notes

- FTS extension unavailable (load-only policy); semantic search features limited.
- 6 large reference markdown files skipped (>512KB).
- S24-05 (validator) and S24-06 (sim consumer) branches not merged into closeout tip — parallel stack layers; re-index after bottom-up Graphite merge.
- Re-run `npx gitnexus analyze` after stack merge to `main` to refresh indexed commit to trunk HEAD.

## Next

Before further `CatalogWriteGate` edits: `gitnexus impact CatalogWriteGate --repo cmano-clone direction=upstream summaryOnly=true`.  
Merge stack bottom-up per `docs/superpowers/plans/sprint-24-graphite-stack.md`, then re-index @ post-merge trunk.