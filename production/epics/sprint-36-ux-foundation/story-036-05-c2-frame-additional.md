---
id: S36-05
status: Ready
type: Integration
priority: must-have
graphite_branch: stack/s36/ux-foundation
estimate_days: 1.5
dependencies:
  - S35-04 Unity C2 frame budget baseline
  - S35-07 C2 sign-off
owner: team-unity
sprint: 36
req_trace: Req 20 TR-c2-004; perf-profile P0 Unity frame budget; production/perf/perf-profile-polish-baseline-2026-06-19.md
governing_adrs: ADR-010 (headless-first presentation); production/perf notes
sprint_gate: true
---

# Story 036-05 — C2 Frame Budget Additional Capture + Notes

> **Epic:** sprint-36-ux-foundation

## Summary

Extend S35-04 baseline: additional measurement (notes + capture where Profiler available), update perf baseline appendix. Headless panel bind timing regression guard remains (C2PanelBindTimingTests). File backlog only if over; no sim hot path changes. Presentation measurement only.

## Acceptance Criteria

- [ ] Evidence update: `production/perf/unity-c2-frame-baseline-s36-*.md` (or appendix to S35 doc) capturing any new Editor/Win host data vs 16.67 ms
- [ ] Headless panel selection path regression: still <100 ms wall; C2 filters 1–13 + 14–18 unchanged PASS
- [ ] ReplayGolden 6/6 PASS on branch
- [ ] Notes added for Linux CI limitation, SimplePlayModeSimHost, UI Toolkit layout cost (from C2PanelBindTimingTests context)
- [ ] If p95 > budget: backlog items only (no edits to DelegationBridge or sim)
- [ ] ZERO touch to production simulation or data pins

## QA Test Cases

```
Test: Headless panel bind regression under budget
  Given: Branch based on S36-05 + C2 selection scenario in UnityAdapter test host
  When: Run C2_panel_selection_bind_path_completes_under_100ms_budget (and related PlayModeSmoke)
  Then: Wall time p95 << 100ms; 85/85 + 58/58 C2 proxy filters PASS; ReplayGolden 6/6 PASS

Test: Perf doc appendix
  Given: Any new Unity Editor host capture available
  When: Measure frame for C2 binders (SimplePlayModeSimHost + left drawer + map + detail)
  Then: Record mean/p95; compare to 16.67 ms; update baseline notes
  Edge: Profiler unavailable → document limitation explicitly
```

## Test Evidence Path

- `production/perf/unity-c2-frame-baseline-s35-2026-06-19.md` + S36 appendix or new file
- `src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/C2PanelBindTimingTests.cs`
- `src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/PlayModeSmokeHarnessTests.cs`
- `production/perf/perf-profile-polish-baseline-2026-06-19.md`

## Out of Scope

- Frame time optimizations or sim edits
- DOTS/ECS UI work
- Non-C2 surfaces
- Globe perf
