# Sprint 110 — Gauntlet Correctness Wave

**Dates:** 2026-08-09 → 2026-08-12 (est. 3 days)  
**Lead:** qa-lead / producer (local closeout)  
**Program:** Release Product Progress (S110–S114) — **S110 only**  
**Stage:** **Release** · **Not Launch**  
**Review mode:** lean  
**Authority:**  
[`production/agentic/agentic-workflow-sprint-series-2026-08-09.md`](../agentic/agentic-workflow-sprint-series-2026-08-09.md) ·  
[`docs/superpowers/plans/2026-08-09-release-product-progress-s110-s114.md`](../../docs/superpowers/plans/2026-08-09-release-product-progress-s110-s114.md) ·  
AGENTS.md · verification-before-completion

**Decision:** Agile owner approved S110–S114 program direction; **package + dispatch S110 only** (2026-08-09).

**Linear:** [DRG-61](https://linear.app/drgamtd-workspace/issue/DRG-61) · [DRG-63](https://linear.app/drgamtd-workspace/issue/DRG-63)

**QA plan:** [`production/qa/qa-plan-sprint-110-gauntlet-correctness-2026-08-09.md`](../qa/qa-plan-sprint-110-gauntlet-correctness-2026-08-09.md)  
**Kickoff:** [`production/agentic/sprint-110-parallel-kickoff-2026-08-09.md`](../agentic/sprint-110-parallel-kickoff-2026-08-09.md)

## Sprint Goal

Make gauntlet **mechanically honest**: tier-3 policies that were silently skipped enter corpus regression; `verify_axis` becomes a production gate (not a manual runbook step). Stage stays **Release**.

## Capacity

| Dimension | Value |
|-----------|-------|
| Total days | 3 |
| Buffer (20%) | 0.5 |
| Available | ~2.5 |
| Parallel tracks | 2 implement + 1 closeout |

## Tracks

| Track | Env | Story | Surface | Owner |
|-------|-----|-------|---------|-------|
| A Policies tier tags | Cloud | S110-01 / DRG-61 | `data/scenarios/gauntlet-20260727-1455-t3-s{1,2,3}.policy.json` + budget docs | qa-lead |
| B verify_axis production path | Cloud | S110-02 / DRG-63 | `tools/qa-gauntlet/verify_stress_axes.py`, `run-gauntlet.sh` or stress entry, tests | qa-lead |
| C Closeout | Local | S110-03 | `production/qa/*`, Linear, smoke | producer |

**Rule:** A and B are surface-disjoint (scenarios JSON vs tools/qa-gauntlet scripts). Do not co-edit.

## Must Have

| ID | Task | Acceptance |
|----|------|------------|
| S110-01 | Add `gauntlet.tier: 3` to t3-s1/s2/s3 | DRG-61 checklist; policies load with tier; budget docs 39→42 tiered (117→126 runs @ 3 seeds) |
| S110-02 | Production caller for `verify_axis` | Invoked from gauntlet stress/post-oracle path; fail when declared non-config-only axis unproven; logistics config-only not hard-fail |
| S110-03 | Dual residual retest + closeout | SYN-T12 + MD-001 via retest-defect.sh if available; smoke-sprint-110; Linear Done |
| S110-04 | Suite / pytest floors | `tools/qa-gauntlet` pytest green; solution suite floor **≥1638/0f** if C# touched (prefer no C# this sprint) |

## Should Have

| ID | Task | Acceptance |
|----|------|------------|
| S110-05 | Expect envelope note | If expects already regenerated 2026-07-27 @ 16 ticks, document "no regen required"; else regen per README-expect-regen |
| S110-06 | README-stress-axes honesty | Corpus tiered count + cost ceiling updated in one PR with S110-01 |

## Nice to Have

| ID | Task |
|----|------|
| S110-07 | Tag other missing-tier ladder policies (t4/t5-s*) — **only if** zero conflict with S110-01 and expects known |

## Hard Gates

| Gate | Pass |
|------|------|
| Stage | Release |
| Launch | Not advanced |
| DelegationBridge | ZERO hotpath |
| CatalogWriteGate | untouched |
| Baltic hash | preserved |
| Phase N | not opened |

## Definition of Done

- [ ] S110-01…03 merged  
- [ ] DRG-61 + DRG-63 Done with PR links  
- [ ] smoke-sprint-110 published  
- [ ] Residual list explicit (t4/t5 missing tiers if not fixed)

## Non-goals

IR sensors (S111) · sim clock (S112) · asset Approved invent · Launch · Phase N · full expect regen of entire corpus
