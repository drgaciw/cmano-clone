# UAT Definition of Done — Play Mode (Project Aegis)

**Scope:** Play Mode UAT of `DelegationSmoke` across five scenarios: `baltic-patrol`, `-classify`, `-comms`, `-mission-roe`, `-mission`.
**Rounds:** at most **3**. Stop earlier when every non-owner criterion is met.
**Scoring:** each criterion has a mechanical check. Jev (`jev-worker`, typed `noul`) then judges whether the written evidence supports the criterion. **Script-first:** Jev can confirm a passing mechanical check; it never overrides a failed one. A criterion is **met** only when the mechanical check passes **and** Jev's `noul` is ≥ **0.80**.

## Criteria

| ID | Criterion | Mechanical check | Owner-only |
|----|-----------|------------------|------------|
| C1 | Headless suite green | `dotnet test ProjectAegis.sln`: 0 failures | no |
| C2 | Determinism preserved | `ReplayGolden` green; Baltic v2 hash `17144800277401907079` unchanged | no |
| C3 | Play Mode smoke | `PlayModeSmokeHarnessTests` ≥ 20 passed | no |
| C4 | Clean console | 0 `Error`/`Exception` entries during Play Mode, each of the 5 scenarios (after the Editor `Library/` cache is cleared) | no |
| C5 | Scenario behaviour matches spec | Per `PLAYMODE-SMOKE.md`: comms transitions (`baltic-patrol-comms`), doctrine/EMCON/source (`-mission-roe`), hostile symbols (`-classify`), overlay HUD counts (`baltic-patrol`) | no |
| C6 | Presentation defects closed | Message-log abort lines collapsed, Unit Detail line wraps, no clipped chrome, score behaviour matches spec — each with a screenshot | no |
| C7 | Forbidden surfaces untouched | `git diff` shows no change to `DelegationBridge.Tick`, `CatalogWriteGate`, replay goldens | no |
| C8 | Every fix has a prior red test | Each defect commit adds a test that fails on the parent commit | no |
| C9 | Interactive behaviour (pause/resume, speed, ACK, click-select) | Needs a click tool this MCP does not expose | **yes** |
| C10 | Pixel-level visual acceptance (DRG-162) | Agent supplies screenshots; owner signs off | **yes** |

C9 and C10 are reported as **BLOCKED (owner)**, not failed. DoD is **met** when C1–C8 are met and C9/C10 are handed to the owner with evidence.

## Jev request shape (one batched call per round)

One `noul` question per criterion, named `c1` … `c8`, plus one `choice` for triage of anything failing:

```json
{
  "state": "<facts only: counts, test names, hashes, console error count, screenshot paths>",
  "questions": {
    "c1": {
      "type": "noul",
      "instructions": "Does the evidence show the full headless suite passed with zero failures?",
      "criteria": { "true": "Evidence states zero failures across the full suite",
                    "false": "Evidence shows failures or does not cover the full suite" }
    },
    "triage": {
      "type": "choice",
      "instructions": "For the failing criterion, what is the best next action?",
      "criteria": {
        "fix_now": "A code or data defect that can be fixed with a failing test first",
        "accept_as_intended": "The behaviour matches a cited spec",
        "defer_to_owner": "Needs a human decision or a capability the agent lacks"
      }
    }
  }
}
```

## Round log

| Round | Date | Met | Not met | Jev scores | Notes |
|-------|------|-----|---------|------------|-------|
| 1 | 2026-11-21 | C2, C3, C4*, C7, C8 | C1 (1 failing guard: committed `Server` define), C5 (D4b open; `baltic-patrol` not re-run), C6 (D4b), C9/C10 owner | **Pending**: jev-worker not yet redeployed, so no Jev scores. Mechanical results only. | *C4: 0 new console errors in 3 scenario runs; 4 stale `.meta` errors remain in the Editor log until a restart clears `Library/`. |
| 2 | 2026-11-21 | C2, C3, C4*, C5**, C6, C7, C8 | C1 (1 failing guard: committed `Server` define), C9/C10 owner | **Pending**: no Jev scores (worker not deployed). | *0 new console errors in all 5 scenario runs. **All 5 scenarios run post-fix; MISSIONS tab contents and clicks are owner-only. |
| 3 | | | | | |

## Rules

- Evidence passed to Jev contains no secrets, paths outside the repo, or personal data.
- A failed mechanical check is never rescued by a high Jev score.
- Each fix is its own commit on the fix branch, with a test that was red first.
- Anything unresolved after round 3 is listed in the UAT report with its owner.
