# Sprint 18 — P2-1 catalog_import_markdown (GitNexus)

**Date:** 2026-06-04  
**Branch:** `stack/data/phase2-cli`

## Impact (pre-edit)

| Symbol | Risk | Action |
|--------|------|--------|
| `CatalogWriteGate` | **HIGH** | **No edits** — `CmoMarkdownImportProposer` calls `ProposeSensorBatch` only |
| `CmoMarkdownImporter` | **LOW** | Read path reused |

## Delivered

| Artifact | Path |
|----------|------|
| Proposer | `src/ProjectAegis.Data/Import/CmoMarkdownImportProposer.cs` |
| CLI | `catalog_import_markdown` in `ProjectAegis.MissionEditor.Cli` |
| Tests | `CmoMarkdownImportProposerTests`, `CatalogImportMarkdownCommandTests` |

## Verify

```powershell
dotnet test ProjectAegis.sln -v minimal
```