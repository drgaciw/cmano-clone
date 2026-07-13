---
id: S35-09
status: Complete
completed: 2026-06-19
type: Visual/Feel
priority: should-have
graphite_branch: stack/sprint35/live-editor-evidence
estimate_days: 2
dependencies:
  - S35-08 merged (advisory)
owner: team-unity
sprint: 35
req_trace: Req 21 presentation; S34-10 protocol placeholders
governing_adrs: ADR-011
---

# Story 035-09 — Live Editor Presentation Evidence

> **Epic:** sprint-35-c2-platform-polish

## Summary

Replace `production/qa/evidence/*-s30..s34-*.png` protocol placeholders with live **1920×1080** Unity Editor captures when host available. Lean fallback: retain placeholders + **PASS WITH NOTES** per S34-10.

## Acceptance Criteria

- [x] 12 PNG targets per presentation README protocols (s30–s34 phases) — protocol placeholders retained; see `production/qa/sprint-35-presentation-evidence-2026-06-19.md` §12-target inventory
- [x] `Invoke-C2PlayModeSignoffBatch.ps1` scenarios clean log (zero `SIGNOFF_ERROR`) when run — **deferred (lean)**; Unity Editor unavailable on Linux CI host
- [x] Evidence doc: `production/qa/sprint-35-presentation-evidence-2026-06-19.md`
- [x] Headless filter **≥58/58** PASS regardless of PNG outcome — **58/58 PASS**
- [x] If Editor blocked: story **skipped** with lean PASS WITH NOTES documented — lean PASS WITH NOTES per S34-10

## QA Test Cases

```
Manual check: Presentation evidence map
  Setup: Read sprint-35-presentation-evidence doc
  Verify: Each s30-s34 PNG mapped to protocol + capture date or lean deferral
  Pass condition: QA can trace every placeholder to live or accepted proxy

Test: Headless gate unchanged
  When: checks 14-18 filter
  Then: 58/58 PASS
```

## Test Evidence Path

- `production/qa/evidence/*-s30..s34-*.png`
- `production/qa/sprint-35-presentation-evidence-2026-06-19.md`

## Out of Scope

- New Platform Editor features
- Cesium/globe captures