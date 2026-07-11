# Requirement 11 Scenario Editor Evidence Manifest

**As of:** 2026-07-11

**Authority:** [11-Agentic-Mission-Editor.md](../requirements/11-Agentic-Mission-Editor.md)

**Scope:** Path inventory for headless AC-1…AC-12. This manifest records evidence locations; it does not expand shipped scope or close residual UI/Phase 3 gates.

| AC | Primary evidence | Supporting evidence |
|----|------------------|---------------------|
| AC-1 | `src/ProjectAegis.Data.Tests/Validation/ScenarioValidationEngineTests.cs` | `src/ProjectAegis.MissionEditor.Cli.Tests/ScenarioValidateCliTests.cs` |
| AC-2 | `src/ProjectAegis.MissionEditor.Cli.Tests/ScenarioSimulateSampleCliTests.cs` | Parallel-isolation and deterministic hash facts in the same file |
| AC-3 | `src/ProjectAegis.Data.Tests/Validation/ScenarioValidationEngineTests.cs`; `src/ProjectAegis.Data.Tests/Validation/ValidationGoldenTests.cs` | `src/ProjectAegis.Data.Tests/Validation/ScenarioDocumentEditorLiveValidationTests.cs` |
| AC-4 | `src/ProjectAegis.Data.Tests/Validation/DoctrineInheritanceValidateTests.cs` | `data/scenarios/validation/doctrine-inheritance.json` |
| AC-5 | `src/ProjectAegis.MissionEditor.Cli.Tests/SampleCompletePipelineTests.cs` | `src/ProjectAegis.MissionEditor.Cli.Tests/ScenarioSimulateSampleCliTests.cs` |
| AC-6 | `tools/ci/smoke-ac6.sh` | Script documents the editVersion diff caveat |
| AC-7 | `src/ProjectAegis.Data.Tests/Scenario/EventDebuggerTests.cs`; `src/ProjectAegis.Data.Tests/Scenario/StubScopePinTests.cs` | `data/scenarios/examples/event-no-fire.scenario.json`; `src/ProjectAegis.Data/Scenario/Authoring/EventDebuggerTrace.cs` |
| AC-8 | `src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/PlayModeSmokeHarnessTests.cs` | `production/qa/ac8-unity-roundtrip-evidence-2026-07-08.md` |
| AC-9 | `src/ProjectAegis.Data.Tests/Scenario/DerivedOnlyInvariantTests.cs` | `src/ProjectAegis.Data.Tests/Architecture/DerivedOnlyInvariantTests.cs` |
| AC-10 | `src/ProjectAegis.Data.Tests/Scenario/ScenarioEditVersionGuardTests.cs`; `src/ProjectAegis.MissionEditor.Cli.Tests/McpMissionToolCliTests.cs` | `src/ProjectAegis.MissionEditor.Cli.Tests/MissionAddFerryCommandTests.cs` |
| AC-11 | `src/ProjectAegis.Data.Tests/Scenario/TeleportUnitExportTests.cs` | Export manifest assertions in the same file |
| AC-12 | `src/ProjectAegis.Data.Tests/Validation/SaveVsExportGateTests.cs` | Save/export/play/sample gate assertions in the same file |

## Gate Evidence

- Last program gate used for this manifest: Mission Editor Phase 2 completion, 2026-07-09.
- Mission Editor Phase 2 gate: `production/qa/mission-editor-phase2-gate-2026-07-09.md`.
- AC-8 host round-trip evidence: `production/qa/ac8-unity-roundtrip-evidence-2026-07-08.md`.
- Current implementation tracker: `Game-Requirements/implementation-tracker-2026-07-04.md`.

## Refresh Rule

When an evidence path moves, update this manifest and the corresponding AC citation in req 11 in the same changeset. A green AC does not imply completion of the residual gates listed in req 11.
