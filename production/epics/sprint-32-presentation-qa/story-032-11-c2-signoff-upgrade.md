---
id: S32-11
status: Complete
Last Updated: 2026-06-19
type: Config
priority: should-have
graphite_branch: stack/sprint32/c2-signoff-upgrade
estimate_days: 1
dependencies:
  - S32-06 Phase F damage surfacing
  - S32-10 presentation evidence
owner: team-qa
sprint: 32
req_trace: Req 20 Command and Control UI; S31-08 C2 checklist carryover; Phase F damage sign-off
---

# Story 032-11 — C2 Manual Sign-Off Upgrade

> **Epic:** sprint-32-presentation-qa  
> **ADR:** ADR-010 (Accepted — headless-first; panel host seams)  
> **UX:** `design/ux/c2-command-post.md`

## Summary

Re-run C2 manual sign-off **checks 14–16**; upgrade **S31 PASS WITH NOTES** when **S32-10 live evidence** and **S32-06 Phase F damage surfacing** exist. Updated `c2-manual-signoff-*.md`; evidence `sprint-32-c2-signoff-*.md`. Lean PASS WITH NOTES if no Unity Editor host.

## Acceptance Criteria

- [x] `production/qa/c2-manual-signoff-*.md` updated with post-S32 build SHA + verdict
- [x] Checks 14–16 re-run; upgraded from S31 PASS WITH NOTES when S32-10 live evidence exists
- [x] **Check 14:** Platform import staging review visible in Editor or headless proxy (`PlatformImportPanelTests`)
- [x] **Check 15:** Doctrine inheritance panel ROE override round-trip in Editor or headless proxy (`DoctrineOverrideCommandTests`)
- [x] **Check 16:** Begin Execution top bar while `SimulationPhase.Planning` in Editor or headless proxy (`C2TopBarBeginExecutionTests`)
- [x] S32-06 damage viewer evidence linked where Phase F checks added
- [x] Evidence doc: `production/qa/sprint-32-c2-signoff-*.md` records verdict + limitation notes
- [x] ZERO touch `DelegationBridge.cs`

## QA Test Cases

- **AC-1**: Checks 14–16 upgrade with live evidence
  - Given: S32-10 live PNG evidence and S32-06 damage viewer surfacing
  - When: checks 14–16 re-run per `c2-manual-signoff-2026-06-02.md` runbook
  - Then: verdict upgraded from S31 PASS WITH NOTES where live captures exist
  - Edge cases: Editor unavailable → headless proxy mapping documented; partial evidence package

- **AC-2**: Phase F damage sign-off extension
  - Given: `PlatformCatalogViewerHost` with damage columns per S32-06
  - When: manual check run for damage workbook visibility
  - Then: damage fields verified in Editor or headless proxy; linked in sign-off doc
  - Edge cases: missing MaxHp row; staging diff not visible in lean mode

## Verify Commands

```bash
export PATH="/home/username01/.dotnet:$PATH"
# Checks 14–16 proxy
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "PlatformImport|Doctrine|C2TopBar|PlatformCatalogViewer" -v minimal
# Optional Editor batch
pwsh tools/unity/Invoke-C2PlayModeSignoffBatch.ps1 -Scenario import -SkipBuild
pwsh tools/unity/Invoke-C2PlayModeSignoffBatch.ps1 -Scenario begin-execution -SkipBuild
```

## GitNexus Symbols

| Symbol | Risk |
|--------|------|
| `PlatformCatalogViewerHost` | LOW — evidence only |
| `PlatformImportPanelHost` | LOW — evidence only |
| `DoctrineInheritancePanelHost` | LOW — evidence only |
| `C2TopBarPanelHost` | LOW — evidence only |
| `DelegationBridge.cs` | ZERO touch |

## References

- C2 checklist baseline: `production/qa/c2-manual-signoff-2026-06-02.md`
- S31-08 pattern: `production/epics/sprint-31-presentation-polish/story-031-08-c2-signoff-refresh.md`
- S32-10 dependency: `production/epics/sprint-32-platform-editor-phase-f/story-032-10-presentation-evidence.md`
- S32-06 dependency: `production/epics/sprint-32-platform-editor-phase-f/story-032-06-platform-phase-f-damage.md`
- Runbook: `production/qa/sprint-18-c2-signoff-runbook-2026-06-04.md`
- Kickoff: `production/sprints/sprint-32-release-train-combat-phase6-platform-phase-f.md` (S32-11)
- QA plan: `production/qa/qa-plan-sprint-32-*.md` *(create before implementation)*