# GitNexus Index — Sprint 35 Closeout (S35-14)

**Date:** 2026-06-19  
**Story:** S35-14 — Closeout Hygiene  
**Indexed commit:** `8de98b150da515b205358106852eb75376ddba5f` (baseline) + working-tree sprint deltas @ closeout verify  
**Command:** `npx gitnexus analyze`

## Index snapshot

| Metric | Count |
|--------|-------|
| Nodes | 16,794 |
| Edges | 33,811 |
| Clusters | 369 |
| Flows | 300 |
| Duration | 50.8 s |
| Mode | Incremental (changed=34, added=20, deleted=0) |

## Delta from S35-01 day-1 baseline

| Ref | Nodes | Edges | Clusters |
|-----|-------|-------|----------|
| S35-01 baseline (2026-06-19) | 16,508 | 33,453 | 364 |
| **S35 closeout** | **16,794** | **33,811** | **369** |

Drift (+286 nodes, +358 edges, +5 clusters) — expected from sim perf P0/P1, C2 USS/UXML polish, validation rule pack, and playtest/doc additions across S35-02..12.

## Hard gates (unchanged @ closeout)

| Gate | Status |
|------|--------|
| ZERO touch `DelegationBridge.cs` | **PASS** |
| Production Baltic hash `17144800277401907079` | **UNCHANGED** |
| ReplayGolden 6/6 | **PASS** |
| Full sln ≥1193 | **PASS** — 1204/1204 |

## Notes

- 6 large files skipped (>512KB, likely generated/vendored) — informational only
- Incremental index preserved 2,106 unchanged file rows; +68 importers added (BFS depth ≤ 4)
- Day-1 baseline evidence retained: `production/agentic/sprint-35-gitnexus-2026-06-19.md`