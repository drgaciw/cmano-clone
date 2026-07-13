---
id: S35-17
status: Complete
completed: 2026-06-19
type: Config
priority: nice-to-have
graphite_branch: stack/sprint35/perf-reprofile
estimate_days: 0.5
dependencies:
  - S35-05 sim P0 merged
owner: perf-profile
sprint: 35
req_trace: perf-profile-polish-baseline; ARCH-NFR-1
governing_adrs: N/A — measurement appendix only
---

# Story 035-17 — Perf Re-Profile Appendix (Post P0)

> **Epic:** sprint-35-sim-perf

## Summary

Re-run timed benchmarks and update `production/perf/perf-profile-polish-baseline-2026-06-19.md` **§Benchmarks** with delta vs pre-S35-05 baseline. Documents P0 detection hot-path impact on tick rate and CI wall time — no new optimization work.

## Acceptance Criteria

- [x] `perf-profile-polish-baseline-2026-06-19.md` updated with **Post-S35-05** benchmark appendix (dated section)
- [x] ReplayGolden suite timing re-recorded (6/6 wall + per-case if changed)
- [x] Full sln `dotnet test -c Release` wall time recorded (1204 tests)
- [x] Tick-rate derivation table updated if P0 changed amortized ms/tick
- [x] P0/P1 hotspot table notes which items S35-05 closed vs remain open (DecisionLog/Datalink → S35-10)
- [x] Unity C2 frame budget row unchanged or cross-ref S35-04 baseline if landed
- [x] No sim code changes in this story — measurement + doc only

## QA Test Cases

```
Test: Benchmark commands reproducible
  Given: S35-05 merged @ trunk
  When: Run documented dotnet test timing commands from perf-profile
  Then: Results recorded in appendix with environment note (OS, dotnet path)

Manual check: Delta narrative clear
  Setup: Compare pre-S35 and post-S35 §Benchmarks tables
  Verify: Executive summary or appendix states improvement, regression, or unchanged per metric
  Pass condition: No false PASS on Unity 16.67 ms if still unmeasured
```

## Test Evidence Path

- `production/perf/perf-profile-polish-baseline-2026-06-19.md` (§Benchmarks appendix)
- Optional: `production/agentic/sprint-35-perf-reprofile-YYYY-MM-DD.md` (raw command output)

## Out of Scope

- Additional sim optimizations (S35-10)
- Unity Profiler capture (S35-04)
- 5k-entity scale benchmarks
- DOTS/ECS migration assessment