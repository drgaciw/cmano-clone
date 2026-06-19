---
id: S35-04
status: Complete
completed: 2026-06-19
type: Integration
priority: must-have
graphite_branch: stack/sprint35/c2-profiler
estimate_days: 1.5
dependencies:
  - S35-01 green baseline
owner: team-unity
sprint: 35
req_trace: Req 20 TR-c2-004; perf-profile P0 Unity frame budget
governing_adrs: ADR-010; production/perf/perf-profile-polish-baseline-2026-06-19.md
sprint_gate: true
---

# Story 035-04 — Unity C2 Frame Budget Baseline

> **Epic:** sprint-35-c2-platform-polish

## Summary

Measure Unity C2 frame budget vs **16.67 ms** P0 target. Unity Profiler capture on Editor host when available; headless panel-bind timing test as Linux CI fallback. File optimization backlog if p95 exceeds budget.

## Acceptance Criteria

- [x] Evidence doc: `production/perf/unity-c2-frame-baseline-s35-2026-06-19.md`
- [x] Records mean/p95 frame time for `SimplePlayModeSimHost` + C2 binders (≥300 frames if Profiler available) — **deferred** (Profiler unavailable Linux CI); backlog BL-C2-01 filed
- [x] Headless test: panel selection path **< 100 ms** wall (Req 20) in `ProjectAegis.Delegation.UnityAdapter.Tests` — p95 **0.013 ms**
- [x] Headless C2 regression filters unchanged: checks 1–13 **85/85** PASS, 14–18 **58/58** PASS
- [x] `ReplayGoldenSuiteTests` — **6/6** PASS
- [x] ZERO touch `DelegationBridge.cs`
- [x] If p95 > 16.67 ms: backlog filed in perf doc (no sim hot-path edits in S35-04) — frame p95 unknown; BL-C2-01..03 filed

## QA Test Cases

```
Test: Headless panel bind under 100ms budget
  Given: UnityAdapter test host with C2 selection scenario
  When: Run panel-bind timing test
  Then: Wall time < 100ms; no test regressions in C2 filter suite

Test: ReplayGolden unchanged
  Given: S35-04 branch
  When: ReplayGoldenSuiteTests
  Then: 6/6 PASS
```

## Test Evidence Path

- `production/perf/unity-c2-frame-baseline-s35-YYYY-MM-DD.md`
- `src/ProjectAegis.Delegation.UnityAdapter.Tests/` (new or extended timing test)

## Out of Scope

- Frame optimization implementation (file backlog only)
- Sim perf P0 (S35-05)