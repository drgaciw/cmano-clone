---
id: S27-10
status: Ready
type: Visual
priority: should-have
graphite_branch: stack/sprint27/presentation-evidence
estimate_days: 0.5
dependencies:
  - S27-07
  - S27-08
owner: team-unity
sprint: 27
req_trace: Lean QA advisory gate
---

# Story 027-10 — Editor Presentation Evidence

> **Epic:** sprint-27-phase-c-presentation  
> **Merge authority:** Headless tests; Editor evidence advisory (lean mode)

## Summary

Capture protocol evidence for platform viewer + APP-6 atlas sprites. Placeholders acceptable when Editor unavailable if headless gates PASS.

## Acceptance Criteria

- [ ] `production/qa/evidence/platform-viewer-s27-*.png` (list + filter)
- [ ] `production/qa/evidence/app6-atlas-s27-*.png` (sprite frames vs unicode)
- [ ] Headless regression filters unchanged and documented
- [ ] Protocol placeholders acceptable per S26-07 pattern

## QA Test Cases

**Manual (advisory):**
- [ ] Platform viewer list + filter visible
- [ ] APP-6 sprites distinct from unicode fallback
- [ ] Headless PASS documented alongside evidence

## References

- S26-07 pattern: `production/qa/sprint-26-presentation-closeout-2026-06-18.md`
- QA plan: `production/qa/qa-plan-sprint-27-2026-06-18.md`