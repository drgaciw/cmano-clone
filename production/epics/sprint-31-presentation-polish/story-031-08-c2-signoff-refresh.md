---
id: S31-08
status: Complete
type: Config
priority: should-have
graphite_branch: stack/sprint31/c2-signoff-refresh
estimate_days: 1
dependencies:
  - S31-07 presentation evidence
owner: team-qa
sprint: 31
req_trace: Req 20 Command and Control UI; S19-01 C2 checklist carryover; S30 lean presentation debt
last_updated: 2026-06-18
---

# Story 031-08 — C2 Manual Sign-Off Refresh

> **Epic:** sprint-31-presentation-polish  
> **ADR:** ADR-010 (Accepted — headless-first; panel host seams)  
> **UX:** `design/ux/c2-command-post.md`

## Summary

Refresh the **C2 manual sign-off checklist** post-S30: extend checks for **platform import staging**, **doctrine inheritance panel**, and **Begin Execution** top bar using S31-07 evidence. Re-run checks 1–13 from baseline checklist; add new rows for S29-04/07/08 advisory gaps cleared by S30-06/S31-07. Lean **PASS WITH NOTES** if no Unity Editor host (headless proxy remains merge authority).

## Acceptance Criteria

- [x] `production/qa/c2-manual-signoff-*.md` updated with post-S31 build SHA + verdict
- [x] Checks 1–13 remain PASS (batch PlayMode or headless proxy where applicable)
- [x] **Check 14 (new):** Platform import staging review visible in Editor or headless proxy (`PlatformImportPanelTests`)
- [x] **Check 15 (new):** Doctrine inheritance panel ROE override round-trip in Editor or headless proxy (`DoctrineOverrideCommandTests`)
- [x] **Check 16 (new):** Begin Execution top bar while `SimulationPhase.Planning` in Editor or headless proxy (`C2TopBarBeginExecutionTests`)
- [x] S31-07 evidence linked for checks 14–16 (`production/qa/evidence/*-s31-*.png` or lean proxy doc)
- [x] Evidence doc: `production/qa/sprint-31-c2-signoff-*.md` records verdict + limitation notes
- [x] Lean PASS WITH NOTES documented if no Editor host
- [x] ZERO touch `DelegationBridge.cs`

## QA Test Cases

- **AC-1**: Baseline C2 checks unchanged
  - Given: post-S31 build @ stack tip
  - When: checks 1–13 re-run per `c2-manual-signoff-2026-06-02.md` runbook
  - Then: all 13 remain PASS via batch or headless proxy
  - Edge cases: comms/classify batch unavailable → headless proxy mapping documented

- **AC-2**: Import staging check (closes S29-04 / S30-06 advisory)
  - Setup: `DelegationSmoke` with `PlatformImportPanelHost`; Baltic fixture staging review
  - Verify: diff preview visible; approve gated until acknowledge
  - Pass condition: check 14 PASS with S31-07 evidence link or headless proxy note

- **AC-3**: Doctrine panel check (closes S29-07 / S30-06 advisory)
  - Setup: friendly unit selected; ROE override applied
  - Verify: panel read-back matches override; no console errors
  - Pass condition: check 15 PASS with S31-07 evidence link or headless proxy note

- **AC-4**: Begin Execution check (closes S29-08 / S30-06 advisory)
  - Setup: scenario in `SimulationPhase.Planning`
  - Verify: Begin Execution control visible; score/loss frozen
  - Pass condition: check 16 PASS with S31-07 evidence link or headless proxy note

## Verify Commands

```bash
export PATH="/home/username01/.dotnet:$PATH"
# Baseline checks 1–13 proxy
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "PlayModeSmoke|C2Selection|OobTree|LossesScoring|BalticReplay|FuelState|AttackMenu" -v minimal
# New checks 14–16
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "PlatformImport|Doctrine|C2TopBar" -v minimal
# Optional Editor batch
pwsh tools/unity/Invoke-C2PlayModeSignoffBatch.ps1 -Scenario comms -SkipBuild
pwsh tools/unity/Invoke-C2PlayModeSignoffBatch.ps1 -Scenario import -SkipBuild
pwsh tools/unity/Invoke-C2PlayModeSignoffBatch.ps1 -Scenario begin-execution -SkipBuild
```

## GitNexus Symbols

| Symbol | Risk |
|--------|------|
| `PlatformImportPanelHost` | LOW — evidence only |
| `DoctrineInheritancePanelHost` | LOW — evidence only |
| `C2TopBarPanelHost` | LOW — evidence only |
| `DelegationBridge.cs` | ZERO touch |

## References

- C2 checklist baseline: `production/qa/c2-manual-signoff-2026-06-02.md`
- S23-U05 pattern: `production/agentic/sprint-23-plan-unity-2026-06-17.md` (check 14 extension)
- S30 presentation evidence: `production/qa/sprint-30-presentation-evidence-2026-06-18.md`
- S31-07 dependency: `production/epics/sprint-31-presentation-polish/story-031-07-presentation-evidence.md`
- Runbook: `production/qa/sprint-18-c2-signoff-runbook-2026-06-04.md`
- Kickoff: `production/sprints/sprint-31-corpus-combat-polish.md` (S31-08)
- QA plan: `production/qa/qa-plan-sprint-31-2026-10-30.md` *(create before implementation)*