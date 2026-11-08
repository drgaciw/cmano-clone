---
id: S35-02
status: Complete
completed: 2026-06-19
type: Config
priority: must-have
graphite_branch: stack/sprint35/qa-plan
estimate_days: 1
dependencies:
  - S35-01 green baseline
owner: team-qa
sprint: 35
req_trace: Production→Polish gate; PI-006 lean QA
governing_adrs: ADR-010 (C2 proxy gate)
---

# Story 035-02 — Sprint 35 QA Plan

> **Epic:** sprint-35-polish-foundation

## Summary

Author `production/qa/qa-plan-sprint-35-2026-06-19.md` with per-story test matrix, C2 **18/18** headless filters, playtest protocol, and smoke closeout checklist. **Blocks all S35-03+ feature work** until merged.

## Acceptance Criteria

- [x] QA plan file exists at `production/qa/qa-plan-sprint-35-2026-06-19.md`
- [x] Covers all must-have stories S35-01 through S35-07, S35-14
- [x] Documents C2 filters: checks 1–13 (`61/61`) + 14–18 (`58/58`)
- [x] Documents ReplayGolden 6/6 + Baltic hash gate on sim merges
- [x] Playtest protocol references `production/playtests/` corpus
- [x] `sprint-status.yaml` `qa_plan` field updated to plan path

## QA Test Cases

*Test cases not yet defined in plan — this story produces the plan that defines them for downstream stories.*

## Test Evidence Path

- `production/qa/qa-plan-sprint-35-2026-06-19.md`

## Out of Scope

- Implementing features under test; QA sign-off (sprint closeout)