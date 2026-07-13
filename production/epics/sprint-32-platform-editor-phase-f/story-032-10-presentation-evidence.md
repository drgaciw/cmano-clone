---
id: S32-10
status: Complete
Last Updated: 2026-06-19
type: Visual / UI
priority: should-have
graphite_branch: stack/sprint32/presentation-evidence
estimate_days: 1.5
dependencies:
  - S32-01 green baseline
owner: team-unity
sprint: 32
req_trace: Req 20 Command and Control UI; Req 21 Platform Editor (damage + import evidence); ADR-010 panel seam
---

# Story 032-10 — Live Editor Presentation Evidence

> **Epic:** sprint-32-platform-editor-phase-f  
> **ADR:** ADR-010 (Accepted — headless-first; panel host seams)  
> **UX:** `design/ux/c2-command-post.md`

## Summary

Replace **S31 protocol PNGs** with **live Editor/PlayMode captures** or refresh **`*-s32-*.png`** placeholders for damage viewer, import staging, and related Phase F flows. Headless tests remain merge authority — no production logic changes unless signoff script scenarios require extension. Headless filter **≥35/35 PASS**.

## Acceptance Criteria

- [x] `production/qa/evidence/*-s32-*.png` — live damage viewer + import staging captures (replaces or refreshes S31 placeholders)
- [x] Evidence doc: `production/qa/sprint-32-presentation-evidence-*.md` maps S31 placeholders → S32 live captures
- [x] Headless tests unchanged PASS: `PlatformImport|Doctrine|C2TopBar|PlayModeSmoke|PlatformCatalogViewer` — ≥35/35
- [x] `Invoke-C2PlayModeSignoffBatch.ps1` scenarios produce clean batch log when Editor available
- [x] Lean PASS WITH NOTES documented if no Unity Editor host (S27-10 pattern)
- [x] ZERO touch `DelegationBridge.cs`

## QA Test Cases

- **AC-1**: Phase F damage evidence
  - Setup: open `DelegationSmoke` scene; `PlatformCatalogViewerHost` with damage columns visible
  - Verify: damage fields rendered; import staging diff for `MaxHp` edit visible
  - Pass condition: `production/qa/evidence/*-s32-*.png` exists with caption; headless `PlatformCatalogViewer` tests still PASS

- **AC-2**: Headless regression unchanged
  - Given: existing S31 import/doctrine/C2 test suites
  - When: full Unity adapter filter runs (`PlatformImport|Doctrine|C2TopBar|PlayModeSmoke|PlatformCatalogViewer`)
  - Then: ≥35/35 PASS without modification
  - Edge cases: lean proxy doc acceptable if Editor unavailable; S31 placeholders remain valid fallback

## Verify Commands

```bash
export PATH="/home/username01/.dotnet:$PATH"
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "PlatformImport|Doctrine|C2TopBar|PlayModeSmoke|PlatformCatalogViewer" -v minimal
# Editor batch (when Unity host available)
pwsh tools/unity/Invoke-C2PlayModeSignoffBatch.ps1 -Scenario import -SkipBuild
pwsh tools/unity/Invoke-C2PlayModeSignoffBatch.ps1 -Scenario doctrine -SkipBuild
```

## GitNexus Symbols

| Symbol | Risk |
|--------|------|
| `PlatformCatalogViewerHost` | LOW — evidence only |
| `PlatformImportPanelHost` | LOW |
| `DoctrineInheritancePanelHost` | LOW |
| `C2TopBarPanelHost` | LOW |
| `DelegationBridge.cs` | ZERO touch |

## References

- UX spec: `design/ux/c2-command-post.md`
- S31-07 pattern: `production/epics/sprint-31-presentation-polish/story-031-07-presentation-evidence.md`
- S27-10 pattern: `production/epics/sprint-27-phase-c-presentation/story-027-10-editor-evidence.md`
- Kickoff: `production/sprints/sprint-32-release-train-combat-phase6-platform-phase-f.md` (S32-10)
- QA plan: `production/qa/qa-plan-sprint-32-*.md` *(create before implementation)*