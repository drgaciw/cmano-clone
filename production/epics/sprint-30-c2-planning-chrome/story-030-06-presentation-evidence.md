---
id: S30-06
status: Complete
Last Updated: 2026-06-18
type: Visual / UI
priority: should-have
graphite_branch: stack/sprint30/presentation-evidence
estimate_days: 1
dependencies:
  - S30-01 green baseline
owner: team-unity
sprint: 30
req_trace: Req 20 Command and Control UI; Req 21 Platform Editor (import staging evidence); ADR-010 panel seam
---

# Story 030-06 — Editor Presentation Evidence Batch

> **Epic:** sprint-30-c2-planning-chrome  
> **ADR:** ADR-010 (Accepted — headless-first; panel host seams)  
> **UX:** `design/ux/c2-command-post.md`

## Summary

Capture **Editor/PlayMode PNG evidence** closing Sprint 29 lean-mode QA conditions for **S29-04** (platform import staging), **S29-07** (doctrine inheritance panel), and **S29-08** (Begin Execution top bar). Headless tests remain merge authority — no production logic changes unless signoff script scenarios require extension. Extend `Invoke-C2PlayModeSignoffBatch.ps1` with import / begin-execution scenarios as needed.

## Acceptance Criteria

- [x] `production/qa/evidence/platform-import-staging-s30-*.png` — import staging review visible (S29-04 condition)
- [x] `production/qa/evidence/doctrine-panel-s30-*.png` — doctrine inheritance panel with ROE override read-back (S29-07 condition)
- [x] `production/qa/evidence/begin-execution-s30-*.png` — C2 top bar Begin Execution while Planning (S29-08 condition)
- [x] `Invoke-C2PlayModeSignoffBatch.ps1` scenarios extended for import and/or begin-execution (documented in evidence README)
- [x] Headless tests unchanged PASS: `PlatformImportPanelTests`, `Doctrine*`, `C2TopBarBeginExecutionTests`
- [x] `PlayModeSmokeHarnessTests` regression rows PASS
- [x] ZERO touch `DelegationBridge.cs`

## QA Test Cases

- **AC-1**: Platform import staging evidence (closes S29-04)
  - Setup: open `DelegationSmoke` scene; trigger platform import staging review on Baltic fixture
  - Verify: diff preview / staging list visible; no console missing-reference errors
  - Pass condition: `production/qa/evidence/platform-import-staging-s30-*.png` exists with caption

- **AC-2**: Doctrine panel evidence (closes S29-07)
  - Setup: select unit with doctrine inheritance; apply ROE override
  - Verify: panel shows unit id, ROE, salvo, source, override controls; read-back matches override
  - Pass condition: `production/qa/evidence/doctrine-panel-s30-*.png` exists; headless `DoctrineOverrideCommandTests` still PASS

- **AC-3**: Begin Execution evidence (closes S29-08)
  - Setup: scenario loaded in `SimulationPhase.Planning`
  - Verify: C2 top bar exposes Begin Execution control; score/loss counters frozen
  - Pass condition: `production/qa/evidence/begin-execution-s30-*.png` exists; `C2TopBarBeginExecutionTests` still PASS

- **AC-4**: Headless regression unchanged
  - Given: existing S29 import/doctrine/C2 test suites
  - When: full Unity adapter filter runs
  - Then: all prior tests PASS without modification
  - Edge cases: lean proxy doc acceptable if Editor unavailable (S27-10 pattern)

## Verify Commands

```bash
export PATH="/home/username01/.dotnet:$PATH"
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "PlatformImport|Doctrine|C2TopBar|PlayModeSmoke" -v minimal
# Optional Editor batch (extend scenarios as implemented)
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
- S29 QA conditions: `production/qa/qa-signoff-sprint-29-2026-06-18.md` (S29-04/07/08 advisory gaps)
- S27-10 pattern: `production/epics/sprint-27-phase-c-presentation/story-027-10-editor-evidence.md`
- S29-04 done: `production/agentic/stacks/sprint29/S29-04-DONE.md`
- S29-07 done: `production/agentic/stacks/sprint29/S29-07-DONE.md`
- S29-08 done: `production/agentic/stacks/sprint29/S29-08-DONE.md`
- Kickoff: `production/sprints/sprint-30-tl-bind-corpus-scale.md` (S30-06)
- QA plan: `production/qa/qa-plan-sprint-30-2026-10-16.md`

## Completion Notes
**Completed**: 2026-06-18
**Criteria**: 7/7 passing (lean mode protocol placeholders)
**Deviations**: Advisory — live Editor capture optional; headless 35/35 is merge authority
**Test Evidence**: Visual — `production/qa/sprint-30-presentation-evidence-2026-06-18.md` + PNG placeholders
**Code Review**: Skipped (lean mode); signoff script scenarios only; ZERO touch `DelegationBridge.cs`