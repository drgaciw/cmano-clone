# Sprint 94 Parallel Kickoff — 2026-07-14

**Program:** Release Continuity S94 only  
**Authority:** [`sprint-94-asset-wave-2.md`](../sprints/sprint-94-asset-wave-2.md), [`qa-plan-sprint-94-asset-wave-2-2026-07-14.md`](../qa/qa-plan-sprint-94-asset-wave-2-2026-07-14.md), [`roadmap-execution-plan-071426.md`](../../docs/reports/roadmap-execution-plan-071426.md)  
**Canonical checkout:** `/home/username01/cmano-clone` (`stack/post-editor/s93-asset-production`)  
**Stage:** **Release** — do not advance Launch  
**Dispatch:** superpowers `dispatching-parallel-agents`

## User approval (roadmap protocol)

User stated **2026-07-14**: open execute plan, follow S94 checkboxes, approve via roadmap protocol, `/sprint-plan new` for **S94 only**.

## Parallel tracks

| Track | Story | Env | Write roots (non-overlapping) | Target child(ren) |
|-------|-------|-----|-------------------------------|-------------------|
| **S94-01** | C2 children | Cloud | `production/assets/c2/` only + manifest rows for C2 | **ASSET-006** Message Log Panel |
| **S94-02** | Baltic + store | Cloud | `production/assets/baltic/` and/or `store/` + matching manifest rows | **ASSET-021** Combat domains hot-tick (primary); optional **ASSET-026** press-kit stub |
| **S94-03** | Closeout | **Local** | Approved criteria, smoke closeout, sprint-status, stage append | After 01∥02 land |

## Rules for agents

1. Placeholders OK — document quality bar (stub USS / minimal PNG / MD overlay sheet).
2. **Honest manifest:** Specced→Done only when file exists; do not mark Approved.
3. **No** Addressables bulk, store upload, Launch docs, C# hotpath, DelegationBridge, CatalogWriteGate rewrites.
4. Do not edit S95–S97 scope into this sprint.
5. Prefer Graphite stacks `stack/sprint94/asset-c2` ∥ `stack/sprint94/asset-baltic-store` when using worktrees; same-tree parallel OK if paths do not collide.

## Merge order

1. S94-01 and S94-02 complete (parallel).  
2. Local S94-03: Approved criteria + manifest sanity + smoke + stage note.  
3. Sync copies into workspace nested tree if harness tracks it.

## Exit for kickoff

- [x] Sprint plan published  
- [x] QA plan published  
- [x] S94-01 ∥ S94-02 dispatched  
- [x] S94-03 closeout  

---
*Parallel kickoff S94 — 2026-07-14*
