# Sprint 82 — Validation Tracks A+C

**Dates:** Following S81 (est. 6–8 days)  
**Lead:** E11 / team-data + c-sharp-test-engineer  
**Program:** S81–S88 Scenario Editor (req 11)  
**Authority:** `roadmap-execute-plan-07042026.md` §3/§4 (S82), `future-sprint-roadpmap-07042026.md`, `scenario-editor-scope-boundary-2026-07-04.md`, `qa-plan-scenario-editor-2026-07-01.md`, `implementation-tracker-2026-07-04.md`

## Sprint Goal
Harden validation tracks A and C: Doctrine inheritance (AC-4), Schema conformance (AC-9 + AME-2.6), Save-vs-export gate (AC-12). Make the editor subset test filter green while maintaining all standing invariants (floor ≥1232, no DelegationBridge edits, etc.).

## Tracks (parallel day 1)

| Track | Stack prefix | Worktree path | Agent env | Owner |
|-------|--------------|---------------|-----------|-------|
| Doctrine inheritance (AC-4) | `stack/sprint82/doctrine-validation` | `.worktrees/stack/sprint82/doctrine-validation` | Cloud | team-data |
| Schema conformance (AC-9/AME-2.6) | `stack/sprint82/schema-conformance` | `.worktrees/stack/sprint82/schema-conformance` | Cloud | team-data |
| Save-vs-export gate (AC-12) | `stack/sprint82/save-export-gate` | `.worktrees/stack/sprint82/save-export-gate` | Cloud | c-sharp-test-engineer |
| Closeout | `stack/sprint82/closeout` | `.worktrees/stack/sprint82/closeout` | Local | c-sharp-devops-engineer |

**Wave order:** All three validation tracks in parallel → closeout

## Primary Files & Changes

- `src/ProjectAegis.Data.Tests/Validation/DoctrineInheritanceValidateTests.cs` (#4)
- `data/scenarios/validation/doctrine-inheritance.json` (#4)
- `src/ProjectAegis.Data.Tests/Scenario/ScenarioDocumentSchemaConformanceTests.cs` (#15)
- `src/ProjectAegis.Data.Tests/Scenario/DerivedOnlyInvariantTests.cs` (#9)
- `data/scenarios/scenario-document.schema.json` (#15)
- `src/ProjectAegis.Data.Tests/Validation/SaveVsExportGateTests.cs` (#12)
- `src/ProjectAegis.Data/Validation/ScenarioValidationExportGate.cs` (#12)

## Hard Gates
- Editor subset filter green: `dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj --filter "DoctrineInheritance|DerivedOnly|SaveVsExport|SchemaConformance"`
- Overall test floor ≥1232
- ZERO DelegationBridge edits
- GitNexus preflight on ScenarioDocumentEditor + ScenarioValidationEngine + CRITICALs
- Cite S81 boundary + this sprint plan in changes

## Verification-before
All agents must run the editor filter + full relevant tests before claiming PASS. Use verification-before-completion skill.

## Next
After closeout → S83 (Export/undo + ferry track D)

---
*Part of roadmap-execute-plan-07042026.md implementation using superpowers dispatching-parallel-agents.*
