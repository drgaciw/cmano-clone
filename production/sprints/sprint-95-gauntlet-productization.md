# Sprint 95 — Gauntlet Productization

**Dates:** 2026-07-14 → 2026-07-21 (est. 4–6 days)  
**Lead:** qa-lead / producer (local closeout)  
**Program:** S94–S97 Release Continuity — **S95 only** (Gauntlet productization)  
**Authority:** [`docs/reports/roadmap-execution-plan-071426.md`](../../docs/reports/roadmap-execution-plan-071426.md), [`docs/reports/future-sprint-roadmap-07142026.md`](../../docs/reports/future-sprint-roadmap-07142026.md), [`qa-gauntlet-effectiveness-plan-2026-07-13.md`](../qa/qa-gauntlet-effectiveness-plan-2026-07-13.md), [`critical-hub-merge-playbook-2026-07-14.md`](../agentic/critical-hub-merge-playbook-2026-07-14.md)

**Predecessor:** **S94 COMPLETE** — asset wave 2 (ASSET-006/021/026 Done); Approved criteria published; stage **Release**.  
**QA plan:** [`production/qa/qa-plan-sprint-95-gauntlet-productization-2026-07-14.md`](../qa/qa-plan-sprint-95-gauntlet-productization-2026-07-14.md)  
**Parallel kickoff:** [`production/agentic/sprint-95-parallel-kickoff-2026-07-14.md`](../agentic/sprint-95-parallel-kickoff-2026-07-14.md)

## Sprint Goal

Productize gauntlet hard-gate norms: **expect/CI discipline** (tier-tick expect regen guidance + CI contract) and **defect-registry hygiene** (residual risks, retest contract), then close with max-variance/oracle family green or last-gate evidence and suite floor **≥1638/0f** cited. **No Launch.** **Does not implement S96–S97.**

## Capacity

| Dimension | Value |
|-----------|-------|
| Total days | 5 |
| Buffer (20%) | 1 day |
| Available | 4 days |
| Parallel tracks | 2 cloud + 1 local closeout |

## Tracks (parallel after baseline)

| Track | Stack prefix | Worktree | Env | Story | Owner |
|-------|--------------|----------|-----|-------|-------|
| Expect / CI discipline | `stack/sprint95/gauntlet-expects` | `.worktrees/stack/sprint95/gauntlet-expects` | **Cloud** | S95-01 | qa-engineer |
| Defect registry hygiene | `stack/sprint95/gauntlet-defects` | `.worktrees/stack/sprint95/gauntlet-defects` | **Cloud** | S95-02 | qa-engineer |
| Closeout + smoke | `stack/sprint95/closeout` | main / local | **Local** | S95-03 | producer |

**Wave order:** Phase 0 → (S95-01 ∥ S95-02) → S95-03

## Tasks

### Must Have

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|---------------------|
| S95-01 | Expect regen / CI discipline deliverable | qa-engineer | 2 | Phase 0 | Doc and/or tool under `production/qa/` or `tools/qa-gauntlet/` defining tier-tick expect regen + CI fail-closed contract; optional workflow notes |
| S95-02 | Defect-registry hygiene | qa-engineer | 1.5 | Phase 0 | `gauntlet-defect-registry.json` updated with residual-risk hygiene (open/watched residuals; closed set intact); retest path still documented |
| S95-03 | Closeout — smoke, floors, stage, sprint-status | producer | 1 | S95-01..02 | Smoke closeout; ≥1638 cited (run or last-gate); stage **Release**; sprint-status S95 |

### Should Have

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|---------------------|
| S95-04 | Optional oracle fingerprint ADR | architect | 1 | Product ask | **Deferred** unless product requests this run |

## Hard Gates

| Gate | Pass criterion |
|------|----------------|
| Scope | Gauntlet QA docs/fixtures/registry only unless expect regen requires C# |
| Suite | **≥1638/0f** if C# touched; else cite `gates-gauntlet-land-post-2026-07-14.log` |
| CRITICAL | Impact before `BalticReplayHarness` / oracle evaluator edits |
| Stage | **Release** — no Launch |
| Bridge | **ZERO** DelegationBridge hotpath |

## Definition of Done

- [ ] S95-01 and S95-02 Must Have complete with on-disk artifacts
- [ ] Smoke closeout published
- [ ] sprint-status.yaml S95 section present
- [ ] Execute-plan **S95** checkboxes flipped for completed work
- [ ] Stage remains **Release**
- [ ] S96–S97 **not** in this sprint scope

## Explicit non-goals

- S94 asset rework (predecessor only)
- S96 architecture promote; S97 continuity gate
- Launch / store submit / Addressables bulk
- Full regen of every expect file unless chosen as S95-01 scope
- Optional oracle ADR unless product asks
- DelegationBridge / hash / Baltic reopen

---
*Created 2026-07-14 from execute plan 071426 §6 S95. Superpowers parallel tracks.*
<!-- harness workspace copy -->
