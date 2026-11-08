# Sprint 98 — Release Residual Backlog (Approved path + gauntlet residuals)

**Dates:** 2026-07-16 → 2026-07-19 (est. 3 days)  
**Lead:** producer / qa-lead (local)  
**Program:** Post–S94–S97 **Release residual** open — **S98 only** (not a new multi-sprint Launch train)  
**Authority:**  
[`docs/reports/future-sprint-roadmap-07142026.md`](../../docs/reports/future-sprint-roadmap-07142026.md) §1 residuals ·  
[`docs/reports/roadmap-execution-plan-071426.md`](../../docs/reports/roadmap-execution-plan-071426.md) (S94–S97 **COMPLETE** + ack) ·  
[`design/assets/approved-criteria-2026-07-14.md`](../../design/assets/approved-criteria-2026-07-14.md) ·  
[`production/qa/gauntlet-defect-registry.json`](../qa/gauntlet-defect-registry.json) ·  
[`production/gate-checks/s97-release-continuity-gate-2026-07-15.md`](../gate-checks/s97-release-continuity-gate-2026-07-15.md) P1 recs

**Predecessors COMPLETE (do not reopen):**  
- **S94** asset wave 2 + Approved criteria · **S95** gauntlet productization · **S96** architecture hygiene · **S97** release continuity gate  
- S97 human ack **PROVIDED** (`i acknowledge` 2026-07-16) — **"release continuity program complete"**; stage **Release**

**QA plan:** [`production/qa/qa-plan-sprint-98-release-residual-2026-07-16.md`](../qa/qa-plan-sprint-98-release-residual-2026-07-16.md)  
**Parallel kickoff:** [`production/agentic/sprint-98-parallel-kickoff-2026-07-16.md`](../agentic/sprint-98-parallel-kickoff-2026-07-16.md)

## Sprint Goal

Advance **Release residual quality** after S94–S97: (1) produce a human-reviewable **Done→Approved pilot package** for 1–2 non-placeholder Done assets without auto-promoting Approved, and (2) triage **watched gauntlet residuals** (expect drift, T5, CI, dual-tree) with retest of closed IDs — while holding stage **Release**. **This is not Launch.**

## Capacity

- Total days: **3**  
- Buffer (~20%): **0.5 day** unplanned  
- Available: **~2.5 days** across 2–3 parallel doc/asset tracks (lean review mode)

## Tracks

| Track | Env | Story | Owner |
|-------|-----|-------|-------|
| Approved-path pilot package | **Local** / Cloud draft | S98-01 | producer / art-director |
| Gauntlet residual triage + retest | **Local** | S98-02 | qa-lead |
| Closeout smoke + floors cite | **Local** | S98-03 | producer |

## Must Have

| ID | Task | Acceptance |
|----|------|------------|
| S98-01 | Approved-path pilot | Select **1–2** Done assets with real binary/content (prefer non-README placeholders); write pilot review package under `production/qa/` or `design/assets/` listing A1–A7 checklist from `approved-criteria-2026-07-14.md`; **do not** flip manifest to Approved without human phrase `asset approved: ASSET-NNN` |
| S98-02 | Gauntlet residual triage | Re-read registry residuals (EXPECT/T5/GHA/BRH/WORKTREE); retest closed id `GAUNTLET-SYN-T12-001` (and optional `GAUNTLET-MD-001`) via `tools/qa-gauntlet/retest-defect.sh`; write residual disposition note (still watched / promote / defer) — **no fake-close** of residuals |
| S98-03 | Closeout | Smoke closeout + sprint-status S98 rows; cite suite floor **≥1638/0f** last-gate family; stage remains **Release** |

## Should Have

| ID | Task | Acceptance |
|----|------|------------|
| S98-04 | Expect discipline smoke | Point operators at `README-expect-regen.md` + discipline doc; optional one-tier expect regen **only** if retest/oracle proves envelope drift (fail-closed; no hand-weakening) |
| S98-05 | Umbrella status honesty | Note ASSET-001…003 still **In Production** in pilot package / closeout (no false Done/Approved) |

## Nice to Have

| ID | Task | Acceptance |
|----|------|------------|
| S98-06 | GitNexus freshness note | `node .gitnexus/run.cjs status` (or analyze) @ HEAD before any CRITICAL-hub code land — docs note only if no C# this sprint |

## Hard Gates

| Gate | Pass |
|------|------|
| Suite | Cite **≥1638/0f** last-gate (or RUN if C# touched) |
| Stage | **Release** throughout |
| Launch | **Not** advanced — `commercial-launch-execution-gate` **out of scope** |
| Predecessors | S94–S97 COMPLETE + S97 human ack cited; **not** reopened |
| Approved path | No auto-Approved; human review required per criteria |
| Residuals | Watched residuals not fake-closed |

## Carryover from Previous Sprint

| Item | Source | Disposition in S98 |
|------|--------|-------------------|
| Approved count **0** | S94 / S97 | Pilot package only (S98-01) |
| Gauntlet residuals watched | S95 | Triage + retest (S98-02) |
| Optional oracle ADR | S95 deferred | Remains deferred unless product asks |
| Launch / commercial gate | Explicit non-goal | Still not opened |

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| Auto-promoting Approved without art review | Med | Process debt | Hard gate: phrase + A1–A7 |
| Treating residual triage as Launch prep | Low | Scope creep | Explicit non-goals; stage Release |
| Fake-closing gauntlet residuals | Med | QA blindness | Registry hygiene rule retained |
| Dual-tree drift on new docs | Med | Agent confusion | Sync nested + standalone on land |

## Dependencies on External Factors

- Human art/producer reviewer for any actual Approved promotion (may leave Approved=0 this sprint)  
- No billing-gated GHA requirement for residual triage (local retest sufficient)

## Definition of Done for this Sprint

- [x] All Must Have tasks completed  
- [x] QA plan exists (`production/qa/qa-plan-sprint-98-*.md`)  
- [x] Smoke closeout written  
- [x] Sprint-status.yaml S98 stories updated honestly  
- [x] Stage remains **Release**  
- [x] No Launch advance / no commercial-launch-execution gate execution  
- [x] S97 remains closed with human ack (not rewritten incomplete)

## Non-goals

- Launch stage advance / store submission / `commercial-launch-execution-gate-TBD` execution  
- Auto-promoting assets to **Approved** without human art review  
- Full max-variance gauntlet ladder / suite re-run unless C# touched  
- Reopening S94–S97 as incomplete or revoking S97 continuity ack  
- Optional oracle ADR unless product explicitly requests  
- Addressables bulk import / ME Phase 2 GUI / DelegationBridge hotpath / Baltic hash reopen  

---
*Created 2026-07-16 via /sprint-plan new (lean). Residual-grounded after S94–S97 program complete. Stage **Release**. Not Launch.*

---
*S98 COMPLETE 2026-07-16 — pilot + residual triage + closeout. Stage **Release**. Not Launch.*
