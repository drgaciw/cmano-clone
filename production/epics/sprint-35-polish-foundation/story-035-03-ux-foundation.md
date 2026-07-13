---
id: S35-03
status: Complete
completed: 2026-06-19
type: UI
priority: must-have
graphite_branch: stack/sprint35/ux-foundation
estimate_days: 2
dependencies:
  - S35-02 QA plan merged
owner: team-ui
sprint: 35
req_trace: Gate r2 residual; polish-scope UX foundation
governing_adrs: ADR-007 (C2 map presentation); design/art/art-bible.md
---

# Story 035-03 — UX Foundation Docs

> **Epic:** sprint-35-polish-foundation

## Summary

Create gate-required design docs: accessibility tier, interaction pattern library, and difficulty curve. **Doc-only** — no new gameplay systems per `production/polish-scope-boundary-2026-06-19.md`.

## Acceptance Criteria

- [x] `design/accessibility-requirements.md` — tier committed (contrast, scaling, motion, remapping stubs)
- [x] `design/ux/interaction-patterns.md` — C2 + Platform Editor patterns (selection, staging diff, error feedback)
- [x] `design/difficulty-curve.md` — Baltic scenario bands; telegraph/fail-recover loops
- [x] Cross-links to `design/ux/c2-command-post.md` and `design/art/art-bible.md`
- [x] Lean review recorded (CONCERNS accepted or APPROVED)
- [x] No code changes required for story completion

## QA Test Cases

```
Manual check: Accessibility doc completeness
  Setup: Read design/accessibility-requirements.md
  Verify: Tier named; C2-relevant requirements listed
  Pass condition: Gate r2 "accessibility tier verified" gap closed

Manual check: Interaction patterns cover implemented screens
  Setup: Read design/ux/interaction-patterns.md
  Verify: Patterns for C2 drawer, map placeholder, Platform import staging
  Pass condition: No "designed in-code only" screens without pattern reference
```

## Test Evidence Path

- `design/accessibility-requirements.md`
- `design/ux/interaction-patterns.md`
- `design/difficulty-curve.md`

## Out of Scope

- AD-ART-BIBLE formal sign-off (S35-03 may reference; full sign-off in S35-07 evidence chain)
- Delegation badges UX (Polish Phase 1 OUT)