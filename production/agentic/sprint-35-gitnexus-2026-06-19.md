# S35-01 — GitNexus Index @ Trunk

**Date:** 2026-06-19  
**Story:** S35-01  
**Commit:** 8de98b150da515b205358106852eb75376ddba5f

## Command

```bash
npx gitnexus analyze
```

## Result

| Metric | Value |
|--------|-------|
| Nodes | 16,508 |
| Edges | 33,453 |
| Clusters | 364 |
| Flows | 300 |
| Duration | 31.4 s |
| Mode | Incremental (+65 importers, 5 changed / 46 added / 82 deleted) |

## Notes

- Day-1 Sprint 35 baseline index; closeout will re-run @ tip (S35-14)
- Hard gates unchanged: ZERO touch `DelegationBridge.cs`; extend-only `CatalogWriteGate`