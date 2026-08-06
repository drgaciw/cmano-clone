# Sprint 103 — Approved C2 Wires + Residual Cadence

**Dates:** 2026-07-17 → 2026-07-21 (est.)  
**Lead:** producer / ui + qa-lead  
**Program:** Post–S102 **Release product** — **S103 only**  
**Stage:** **Release** · **Not Launch**

**Predecessors:** S101 complete (Approved 006/021/004 path); S102 residual + 006 wire PR #316; dual retest green.

## Sprint Goal

Land **Approved assets into live C2**: finish **#316** (006 Message Log), wire **ASSET-021** Combat Domains Hot-Tick into Unity UI Toolkit (tokenized USS + UXML + headless tests), keep residual dual retest + suite floor. Path A only on real human phrases (005 optional). Stage remains **Release**.

## Must Have

| ID | Task | Acceptance |
|----|------|------------|
| S103-01 | Land #316 / 006 wire | CI green + human merge (agent restacks/ready only unless told merge) |
| S103-02 | ASSET-021 C2 wire | Unity CombatDomains panel + tests green; suite ≥1638 |
| S103-03 | Residual dual retest | SYN-T12 dual + MD-001 PASS; residuals watched |
| S103-04 | Suite floor | Live green after code (target ~1702+) |

## Should Have

| ID | Task | Acceptance |
|----|------|------------|
| S103-05 | Path A ASSET-005 if phrase | Real `asset approved: ASSET-005` only |

## Non-goals

- Launch / commercial  
- Invented approvals  
- Full gauntlet TDD without red oracle  
- Broad sim refactor  

## Definition of Done

- [ ] Must Haves complete (S103-01 merge may stay human-gated)  
- [ ] QA plan + kickoff  
- [ ] Evidence under `production/qa/s103-*`  
- [ ] yaml S103 updated  
- [ ] Stage Release  

---
*Created 2026-07-17 orchestrator. Product sprint after residual hold.*
