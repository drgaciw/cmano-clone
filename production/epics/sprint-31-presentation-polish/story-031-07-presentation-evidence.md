---
id: S31-07
status: Complete
type: Visual / UI
priority: should-have
graphite_branch: stack/sprint31/presentation-evidence
estimate_days: 1
dependencies:
  - S31-01 green baseline
owner: team-unity
sprint: 31
req_trace: Req 20 Command and Control UI; Req 21 Platform Editor (import staging evidence); ADR-010 panel seam
last_updated: 2026-06-18
---

# Story 031-07 — Live Editor Presentation Evidence

> **Epic:** sprint-31-presentation-polish  
> **ADR:** ADR-010 (Accepted — headless-first; panel host seams)  
> **UX:** `design/ux/c2-command-post.md`

## Summary

Replace **S30 protocol placeholder PNGs** with **live Editor/PlayMode captures** for platform import staging, doctrine inheritance panel, and Begin Execution top bar. Headless tests remain merge authority — no production logic changes unless signoff script scenarios require extension. Extend `Invoke-C2PlayModeSignoffBatch.ps1` import + begin-execution scenarios as needed.

## Acceptance Criteria

- [x] `production/qa/evidence/platform-import-staging-s31-*.png` — live import staging review (replaces S30 protocol placeholder)
- [x] `production/qa/evidence/doctrine-panel-s31-*.png` — live doctrine inheritance panel with ROE override read-back (replaces S30 protocol placeholder)
- [x] `production/qa/evidence/begin-execution-s31-*.png` — live C2 top bar Begin Execution while Planning (replaces S30 protocol placeholder)
- [x] `Invoke-C2PlayModeSignoffBatch.ps1` scenarios (`import`, `begin-execution`) produce clean batch log when Editor available
- [x] Headless tests unchanged PASS: `PlatformImportPanelTests`, `Doctrine*`, `C2TopBarBeginExecutionTests`
- [x] `PlayModeSmokeHarnessTests` regression rows PASS
- [x] Evidence doc: `production/qa/sprint-31-presentation-evidence-*.md` maps S30 placeholders → S31 live captures
- [x] ZERO touch `DelegationBridge.cs`

## QA Test Cases

- **AC-1**: Platform import staging evidence (replaces S30-06 placeholder)
  - Setup: open `DelegationSmoke` scene; trigger platform import staging review on Baltic fixture
  - Verify: diff preview / staging list visible; no console missing-reference errors
  - Pass condition: `production/qa/evidence/platform-import-staging-s31-*.png` exists with caption; headless `PlatformImportPanelTests` still PASS

- **AC-2**: Doctrine panel evidence (replaces S30-06 placeholder)
  - Setup: select unit with doctrine inheritance; apply ROE override
  - Verify: panel shows unit id, ROE, salvo, source, override controls; read-back matches override
  - Pass condition: `production/qa/evidence/doctrine-panel-s31-*.png` exists; headless `DoctrineOverrideCommandTests` still PASS

- **AC-3**: Begin Execution evidence (replaces S30-06 placeholder)
  - Setup: scenario loaded in `SimulationPhase.Planning`
  - Verify: C2 top bar exposes Begin Execution control; score/loss counters frozen
  - Pass condition: `production/qa/evidence/begin-execution-s31-*.png` exists; `C2TopBarBeginExecutionTests` still PASS

- **AC-4**: Headless regression unchanged
  - Given: existing S30 import/doctrine/C2 test suites
  - When: full Unity adapter filter runs
  - Then: all prior tests PASS without modification
  - Edge cases: lean proxy doc acceptable if Editor unavailable (S27-10 pattern; S30 placeholders remain valid fallback)

## Verify Commands

```bash
export PATH="/home/username01/.dotnet:$PATH"
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "PlatformImport|Doctrine|C2TopBar|PlayModeSmoke" -v minimal
# Editor batch (when Unity host available)
pwsh tools/unity/Invoke-C2PlayModeSignoffBatch.ps1 -Scenario doctrine
pwsh tools/unity/Invoke-C2PlayModeSignoffBatch.ps1 -Scenario import -SkipBuild
pwsh tools/unity/Invoke-C2PlayModeSignoffBatch.ps1 -Scenario begin-execution -SkipBuild
```

## GitNexus Symbols

| Symbol | Risk |
|--------|------|
| `PlatformImportPanelHost` | LOW |
| `DoctrineInheritancePanelHost` | LOW |
| `C2TopBarPanelHost` | LOW |
| `DelegationSmokeSceneBuilder` | LOW — scene wiring only |
| `DelegationBridge.cs` | ZERO touch |

## References

- UX spec: `design/ux/c2-command-post.md`
- S30-06 pattern: `production/epics/sprint-30-c2-planning-chrome/story-030-06-presentation-evidence.md`
- S30 protocol placeholders: `production/qa/sprint-30-presentation-evidence-2026-06-18.md`
- S27-10 pattern: `production/epics/sprint-27-phase-c-presentation/story-027-10-editor-evidence.md`
- Kickoff: `production/sprints/sprint-31-corpus-combat-polish.md` (S31-07)
- QA plan: `production/qa/qa-plan-sprint-31-2026-10-30.md` *(create before implementation)*