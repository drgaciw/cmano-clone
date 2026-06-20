---
id: S36-20
status: Complete
type: Config
priority: nice-to-have
Last Updated: 2026-06-20
graphite_branch: stack/s36/perf-determinism-reprofile
estimate_days: 0.5
dependencies:
  - S36-05 + S36-10 merged (harness + DecisionLog stable)
  - All prior S36 stories
owner: perf-profile
sprint: 36
req_trace: perf-profile-polish-baseline; ARCH-NFR-1; TR-simcore-005
governing_adrs: N/A — measurement + gate enforcement appendix only
sprint_gate: true
---

# Story 036-20 — Perf Re-Profile + Replay-Verify Gate Enforcement

> **Epic:** sprint-36-perf-determinism

## Summary

Re-run benchmarks post all S36 changes (audit, maintenance, polish). Append to `production/perf/perf-profile-polish-baseline-2026-06-19.md` with Post-S36 section. Record ReplayGolden timing, full sln test time, tick rate delta. Enforce `/replay-verify` as explicit gate in CI/docs for all sim changes. No sim code changes — measurement + doc + gate polish only. Hash immutable confirmed in numbers.

GitNexus: Re-index note only (after merge).

## Acceptance Criteria

- [ ] `perf-profile-polish-baseline-2026-06-19.md` updated with **Post-S36** benchmark appendix (dated)
- [ ] ReplayGolden suite timing re-recorded (6/6 wall + per-case)
- [ ] Full sln `dotnet test -c Release` wall time recorded
- [ ] Tick-rate derivation table updated (ms/tick amortized)
- [ ] P0/P1 hotspot table notes S36 items closed vs remaining (cross-ref S35)
- [ ] Replay-verify gate explicitly called out in profile + polish-boundary cross-refs
- [ ] All S36 stories' golden hashes unchanged (documented)
- [ ] No sim code changes in this story — pure measurement + doc
- [ ] `/replay-verify` documented as mandatory pre-merge for perf/det stories

## QA Test Cases

```
Test: Benchmark commands reproducible
  Given: All S36 stories merged @ trunk (worktree clean)
  When: Run documented /usr/bin/time dotnet test commands from perf-profile
  Then: Results appended with env note (OS, dotnet, machine parity note)

Manual check: Delta vs S35 + gate
  Setup: Compare pre-S36 vs post-S36 §Benchmarks tables + goldens
  Verify: Executive summary states deltas; replay-verify gate reinforced
  Pass condition: No false claims on budgets; hashes match

Test: Replay-verify gate doc
  Given: Updated profile + boundary docs
  When: Search for "replay-verify"
  Then: Explicit in S36 appendix and polish scope
```

## Test Evidence Path

- `production/perf/perf-profile-polish-baseline-2026-06-19.md` (Post-S36 appendix)
- `production/agentic/sprint-36-perf-determinism-*.md` (raw output if generated)
- `production/determinism/replay-2026-06-19.md`
- Command outputs captured

## Out of Scope

- Actual perf work (prior stories)
- Unity profiler (other sprints)
- 5k-entity scale
- DOTS assessment

**Enforcement:** replay-verify any implied; ZERO Delegation (none here); hash immutable.
