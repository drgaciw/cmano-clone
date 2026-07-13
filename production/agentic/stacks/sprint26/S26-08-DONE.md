# S26-08 story-done evidence — C2 signoff tooling doctrine scenario

**Story:** `production/sprints/sprint-26-cmo-phase2-presentation-closeout.md` §S26-08  
**Status:** Complete  
**Completed:** 2026-06-18

## Deliverables

- `tools/unity/Invoke-C2PlayModeSignoffBatch.ps1` — `-Scenario doctrine` in `ValidateSet`; maps to `RunDoctrineBatch`
- `C2PlayModeSignoffBatchRunner.RunDoctrineBatch()` — `baltic-patrol-mission-roe` policy fixture
- `unity/ProjectAegis/PLAYMODE-SMOKE.md` — doctrine batch documented in tri-batch table
- Headless proxy: `PlayModeSmoke|Doctrine` filter **17/17 PASS**

## Verify

```bash
export PATH="/home/username01/.dotnet:$PATH"
# PS1 syntax (static): ValidateSet includes doctrine; switch maps RunDoctrineBatch
grep -E "doctrine|RunDoctrineBatch" tools/unity/Invoke-C2PlayModeSignoffBatch.ps1 \
  unity/ProjectAegis/Assets/Editor/C2PlayModeSignoffBatchRunner.cs
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "PlayModeSmoke|Doctrine" -v minimal
# Doctrine proxy: 17/17 PASS
```

## Acceptance criteria

| AC | Evidence | Verdict |
|----|----------|---------|
| Script accepts `-Scenario doctrine` | `ValidateSet("comms", "classify", "doctrine")` | **PASS** |
| `RunDoctrineBatch` documented | XML doc on runner + `PLAYMODE-SMOKE.md` tri-batch section | **PASS** |
| Doctrine batch preconditions | `Baltic_doctrine_mission_roe_harness_matches_doctrine_batch_preconditions` | **PASS** |
| Headless proxy green | `PlayModeSmoke\|Doctrine` **17/17** | **PASS** |

## Advisory (lean mode)

Unity Editor batchmode not run on Linux host; headless proxy is merge authority per S25-11 pattern.

## Verdict

**COMPLETE** — doctrine signoff tooling wired; closes S25-11 follow-up item #2.