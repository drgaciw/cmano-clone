# Sprint 99 Parallel Kickoff — 2026-07-16

**Skill:** superpowers:dispatching-parallel-agents  
**Review mode:** lean (PR-SPRINT skipped)  
**Stage:** **Release** — not Launch  

## Independent tracks (dispatch when implementing)

| Agent | Story | Scope | Must not |
|-------|-------|-------|----------|
| A | S99-01 | Approved review queue package for ASSET-006/021 | Invent `asset approved:`; auto-flip Approved; Launch docs |
| B | S99-02 | Residual re-watch + `retest-defect.sh` closed IDs | Fake-close residuals; hand-edit expects |
| C | S99-03 | Closeout smoke after A∥B land | Claim Launch; reopen S97/S98 |

## Merge order

1. Land A and B (independent paths under `production/qa/s99-*`)  
2. C closeout + sprint-status story flips  
3. Dual-tree sync if both checkouts present  

## Predecessor truth

S94–S97 **COMPLETE** (S97 **i acknowledge**). S98 **COMPLETE** (pilot + residual triage). Do not rewrite.

---
*Sprint-99 parallel kickoff — open only; implementation is a later goal.*
