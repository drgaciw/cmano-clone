# Replay Verify Report — S42-04 Content wave 1 Scenario/data

**Date:** 2026-06-20  
**Scope:** baltic-patrol* policy JSON maint wave (replay-gated; 6 golden cases)  
**Mode:** verify (post maint, no record)  
**Sim runner:** BalticReplayHarness via dotnet test filter ReplayGoldenSuiteTests  
**Commit / baseline:** main @ post S41 (c4d6e52 tip per S41); S42 worktree stack/sprint42/content-scenario for policy changes.  
**Citations:** See full in WT report + release-enablement-scope-boundary-2026-06-20.md + scope-expansion-decision-2026-06-20-S41-close.md ("i provide the ack" S41 PASS, S42 UNBLOCKED) + sprint-42 kickoff S42-04.

## Results

| Scenario | Seed | Ticks | A vs B | A vs Golden | Verdict |
|----------|------|-------|--------|-------------|---------|
| baltic-patrol | 42 | 4 | MATCH | MATCH | PASS |
| baltic-patrol-comms | 42 | 6 | MATCH | MATCH | PASS |
| baltic-patrol-classify | 42 | 4 | MATCH | MATCH | PASS |
| baltic-patrol-stale | 11 | 3 | MATCH | MATCH | PASS |
| baltic-patrol-spoof | 7 | 5 | MATCH | MATCH | PASS |
| baltic-patrol-readiness | 7 | 5 | MATCH | MATCH | PASS |

**6/6 PASS** (duration ~170ms). No divergence. Hash `17144800277401907079` (immutable).

**Maint performed (policy JSON only, no hash impact):** See worktree report. Non-golden fixtures updated for B1 maint (opp-hold-fire, mission-roe, magazine) + ids doc. Golden untouched.

**GitNexus (impact first):** BalticReplayHarness CRITICAL; ScenarioPolicyProfile CRITICAL (per S42-04). 

**Verdict:** PASS (replay-gated gate held for S42-04). csharpexpert + team-simulation + determinism-engineer.

**Evidence paths:** 
- /home/username01/cmano-clone/stack/sprint42/content-scenario/S42-04-replay-verification-2026-06-20.md
- tests/regression/replay-golden-*.txt
- data/scenarios/*.policy.json (maint copies in WT)
