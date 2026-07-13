---
id: S35-13
status: Complete
Last Updated: 2026-06-19
type: Config
priority: should-have
graphite_branch: stack/sprint35/stage-advance
estimate_days: 0.25
dependencies:
  - S35-14 closeout hygiene complete
  - explicit user confirmation of gate CONCERNS (r2)
owner: producer
sprint: 35
req_trace: Production→Polish gate; production/stage.txt
governing_adrs: N/A — producer stage ritual
---

# Story 035-13 — Stage Advance Production → Polish

> **Epic:** sprint-35-closeout-devops

## Summary

After sprint closeout and **explicit user acknowledgment** of gate r2 **CONCERNS (uplifted)**, update `production/stage.txt` from `Production` to `Polish`. Records gate path and residual Sprint 0 items — does not claim clean PASS.

## Acceptance Criteria

- [x] User has explicitly confirmed advance despite CONCERNS (documented in closeout or story evidence)
- [x] `production/stage.txt` updated to `Polish`
- [x] Gate reference recorded: `production/gate-checks/production-to-polish-2026-06-19-r2.md` (CONCERNS uplifted)
- [x] Residual items acknowledged in evidence (accessibility, interaction patterns, Unity C2 frame budget, AD sign-off, `tests/unit/` layout)
- [x] `sprint-status.yaml` sprint 35 `status` reflects stage advance if applicable
- [x] No code or test changes required — doc-only stage ritual

## QA Test Cases

```
Manual check: Stage file reflects Polish with gate ack
  Setup: S35-14 closeout PASS; user confirmation captured
  Verify: production/stage.txt reads "Polish"; evidence cites gate r2 CONCERNS + user ack
  Pass condition: Stage advanced only after both closeout green and user confirmation
```

## Test Evidence Path

- `production/stage.txt`
- `production/qa/smoke-sprint-35-closeout-YYYY-MM-DD.md` (user-ack section)
- `production/gate-checks/production-to-polish-2026-06-19-r2.md`

## Out of Scope

- Re-running gate-check or claiming PASS verdict
- Implementing residual Sprint 0 items (S35-03, S35-04, etc.)
- Advancing to Release or beyond Polish