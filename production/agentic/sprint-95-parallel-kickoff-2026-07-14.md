# Sprint 95 Parallel Kickoff — 2026-07-14

**Program:** Release Continuity **S95 only** (after S94 complete)  
**Authority:** [`sprint-95-gauntlet-productization.md`](../sprints/sprint-95-gauntlet-productization.md), [`qa-plan-sprint-95-gauntlet-productization-2026-07-14.md`](../qa/qa-plan-sprint-95-gauntlet-productization-2026-07-14.md)  
**Canonical:** `/home/username01/cmano-clone`  
**Stage:** **Release** — do not advance Launch  
**Dispatch:** superpowers `dispatching-parallel-agents`

## Predecessor (do not re-open)

S94 complete: ASSET-006/021/026 Done; Approved criteria; sprint-status `sprint: 94`.

## Parallel tracks

| Track | Story | Env | Write roots | Deliverable |
|-------|-------|-----|-------------|-------------|
| **S95-01** | Expect/CI discipline | Cloud | `production/qa/gauntlet-expect-*`, `tools/qa-gauntlet/` (docs/scripts only preferred) | Tier-tick expect regen + CI fail-closed contract |
| **S95-02** | Defect registry | Cloud | `production/qa/gauntlet-defect-registry.json` (+ optional residual notes) | Hygiene update; residual risks; closed IDs kept |
| **S95-03** | Closeout | **Local** | smoke, stage note, sprint-status, execute-plan S95 `[x]` | After 01∥02 |

## Rules

1. Non-overlapping primary write paths between S95-01 and S95-02.
2. No Launch, Addressables, store submit, S94 asset rework, S96–S97 implementation.
3. Prefer **no** `BalticReplayHarness` edits; if required → GitNexus impact first.
4. Suite: run only if C# touched; else cite post-land **1638/0f**.

## Exit for kickoff

- [x] Sprint plan published  
- [x] QA plan published  
- [x] S95-01 ∥ S95-02 dispatched  
- [x] S95-03 closeout  

---
*Parallel kickoff S95 — 2026-07-14*
