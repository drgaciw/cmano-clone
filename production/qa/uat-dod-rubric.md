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
| C9 | Interactive behaviour (pause/resume, speed, ACK, click-select) | Driven through MCP `script-execute`: `NavigationSubmitEvent` on the control (a code-sent `ClickEvent` does not reach UI Toolkit buttons), then a state, label or log change is observed | no (round 3+) |
| C10 | Pixel-level visual acceptance (DRG-162) | Agent supplies screenshots; owner signs off | **yes** |
| C11 | Every UI function exercised | 100% of rows in `production/qa/uat-ui-function-matrix.md` (game panels, runtime editor panels, Editor windows, Mission Editor CLI verbs) are marked **Pass**, **Defect** (with ID) or **Owner** (with reason); "no exception" alone is not a pass | no |
| C12 | Shipped data stays unchanged | `sha256sum assets/data/catalog/*.db` is identical before and after a Play Mode run and the full headless suite | no |

C10 is reported as **BLOCKED (owner)**, not failed. DoD is **met** when C1–C9, C11 and C12 are met and C10 is handed to the owner with evidence.

Harness for C9/C11: `unity/ProjectAegis/.ai-game-dev/snippets/UatHarness.cs` (passed to `script-execute` via `include`). Play Mode only; it adds missing panel hosts at runtime and never saves the scene.

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
| 3 | 2026-11-21 | C1†, C2, C3, C4*, C5, C7, C9, C12 | C6 (D29–D31, D12 open), C8 (D21/D23/D25 RED compile-only, D24 RED not observed, D20 no test), C11 (Owner rows listed with reasons in the matrix; builder menu items + Linux build not run) | **None**: `~/.config/aegis/jev.env` never provided, so Jev made no decisions in any round. Mechanical results only. | *0 console errors in all 5 scenarios after the fixes; D14 cleared by the Editor restart. Unity PlayMode tests 18/20 (D32 local edit, D33 likely pre-existing). †C1: on a clean checkout of HEAD (`152a0e31`) plus `tools/copy-delegation-assemblies.sh`, the full suite is 4,598 passed / 0 failed. The failing assertion was an uncommitted round 2 guard edit reading the Editor-rewritten working copy (D34); now committed reading HEAD, and the live working copy passes 1258/1258. C7 re-verified: `DelegationBridge.cs`, `CatalogWriteGate`, replay goldens identical to main; Phase 0 smoke PASS |
| 4 | 2026-11-21 | C1, C2, C3, C4, C5, C6, C7, C9, C12 | C8 (partial red-first evidence for D20/D21/D23/D24/D25, as recorded in round 3), C11 (Owner rows with reasons: D15/D18/D26 panels, Doctrine Inheritance, LiveEdit, map-marker select, Browse dialog, Linux build) | **None** — `~/.config/aegis/jev.env` still absent | Round added at the user's request after the Editor was unblocked. Full suite passes in the live working copy (9/9 projects, UnityAdapter 1264) and on a clean checkout; Phase 0 smoke PASS; catalog hashes unchanged; 0 console errors. C6: D29, D30(+b), D31(+b), D12 fixed and verified by screenshot. C10 evidence handed to owner (`evidence/uat-2026-11-21/*.png`) |

## Rules

- Evidence passed to Jev contains no secrets, paths outside the repo, or personal data.
- A failed mechanical check is never rescued by a high Jev score.
- Each fix is its own commit on the fix branch, with a test that was red first.
- Anything unresolved after round 3 is listed in the UAT report with its owner.
