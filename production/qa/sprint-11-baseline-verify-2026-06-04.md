# Sprint 11 — Baseline verify evidence (2026-06-04)

**Track:** S11-01 / S11-02 / S11-07  
**Worktree:** `.worktrees/sprint11-verify`  
**Branch:** `stack/sprint11-baseline-verify`  
**Evidence commit:** `da6f7a64f715bf5f56c865a159d024000515669e`  
**Base SHA (pre-evidence):** `949282745dcbad566566ecf5d7fba681f89a3387`

## Summary

| Gate | Result | Count / notes |
|------|--------|----------------|
| GitNexus analyze | **PASS** | 6,060 nodes · 15,208 edges · 159 clusters · 300 flows (59.6s) |
| `dotnet restore ProjectAegis.sln` | **PASS** | All projects up-to-date |
| `dotnet build ProjectAegis.sln -c Release` | **PASS** | 0 warnings · 0 errors |
| `dotnet test ProjectAegis.sln -c Release` | **PASS** | **359/359** (0 failed, 0 skipped) |
| PlayMode smoke filter | **PASS** | `PlayModeSmokeHarnessTests` **7/7** |
| Headless QA proxy (`Invoke-ManualQaHeadlessGate.ps1 -SkipBuild`) | **PASS** | **18/18** filtered (7 Delegation + 11 UnityAdapter) |

**Overall:** **PASS**

## GitNexus (S11-01)

```text
npx gitnexus analyze
Repository indexed successfully (59.6s)
6,060 nodes | 15,208 edges | 159 clusters | 300 flows
```

Skipped 6 large files (>512KB). `DelegationBridge` remains CRITICAL per sprint kickoff — no bridge edits on this track.

## .NET baseline (S11-02)

| Assembly | Passed | Total |
|----------|--------|-------|
| Sim.Tests | 84 | 84 |
| MissionEditor.Cli.Tests | 14 | 14 |
| Delegation.Tests | 144 | 144 |
| UnityAdapter.Tests | 73 | 73 |
| Data.Tests | 44 | 44 |
| **Solution total** | **359** | **359** |

## Re-verify (main worktree, 2026-06-04)

After Wave 5 attack-menu tests landed on `feat/wave5-attack-readiness-spoof` (uncommitted working tree):

| Gate | Result |
|------|--------|
| `dotnet test ProjectAegis.sln -c Release` | **365/365** PASS |
| PlayMode smoke | **7/7** PASS |
| Headless QA proxy | **18/18** PASS |
| GitNexus analyze | **6,531** nodes (post attack-menu + superpowers docs) |

## Related

- Kickoff: `production/agentic/sprint-11-kickoff-2026-06-04.md`
- Hindsight: `production/agentic/sprint-11-hindsight-recall-2026-06-04.md`
- Wave 5 evidence: `production/qa/sprint-11-wave5-evidence-2026-06-04.md`