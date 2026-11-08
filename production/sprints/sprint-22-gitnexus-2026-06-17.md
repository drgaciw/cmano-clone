# Sprint 22 GitNexus Re-index — 2026-06-17

**Repo:** `/home/username01/cmano-clone/cmano-clone`  
**Command:** `npx gitnexus analyze --force`  
**Duration:** 17.6s  
**Indexed commit:** `34d4e6f` (matches HEAD; uncommitted Sprint 22 work not yet in index commit)

## Graph stats

| Metric | Value |
|--------|-------|
| Nodes | 8,671 |
| Edges | 17,891 |
| Clusters | 217 |
| Flows | 300 |

**Prior baseline (Sprint 11):** 6,531 nodes — **+2,140 nodes** after Sprint 12–22 growth.

## Status

```
Repository: cmano-clone
Indexed: 2026-06-17
Status: up-to-date @ 34d4e6f
```

## detect_changes (uncommitted, scope=all)

| Metric | Value |
|--------|-------|
| Changed symbols | 75 |
| Changed files | 24 |
| Affected processes | 29 |
| Risk level | **CRITICAL** |

### High-blast-radius symbols (extend-only verified in tests)

- `CatalogWriteGate` — `ProposePlatformBatch`, `ProposeWeaponBatch`, `ApproveBatch`, `DeleteStagingRows`
- `CmoMarkdownImporter` / `CmoMarkdownImportProposer` — platform+weapon+mount parsing
- `OsintCatalogMapper` — TL routing (`ResolveTrlLevel`, `ResolveBranchTag`)
- `DoctrineOverrideCommand` / `DelegationBridgeHost.TrySetDoctrineOverride`

### Affected process families

- `RunCatalogImportMarkdown → *` (sensor path regression risk)
- `ProposePlatformWeaponMounts → *` (new S22-04 path)
- `OnApproveSelected → ApproveBatch` (staging commit path)

## Notes

- FTS extension unavailable (load-only policy); semantic search features limited.
- 6 large reference markdown files skipped (>512KB).
- Re-run `npx gitnexus analyze` after Sprint 22 commits land to refresh indexed commit hash.

## Next

Before merge: `gitnexus impact CatalogWriteGate direction=upstream` on any further gate edits.
After commit: `npx gitnexus analyze` to sync index to new HEAD.