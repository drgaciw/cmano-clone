# Graphite stack author: split Mission Editor v1 uncommitted work into ordered branches.
# Usage: run from repo root on `main` with dirty tree. Requires `gt` on PATH.
$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '../..')
$root = Get-Location

function Invoke-GtCreate {
    param([string]$Branch, [string]$Message, [string[]]$Paths)
    git reset
    foreach ($p in $Paths) {
        if (-not (Test-Path $p)) { Write-Warning "Missing path (skip): $p"; continue }
        git add -- $p
    }
    $staged = git diff --cached --name-only
    if ($staged) {
        gt create --no-interactive $Branch -m $Message
    }
    else {
        Write-Host "  (reserved / doc-only slice — empty branch)"
        gt create --no-interactive $Branch -m $Message
    }
    if ($LASTEXITCODE -ne 0) { throw "gt create failed: $Branch" }
}

Write-Host "=== Mission Editor v1 Graphite stack ==="
Write-Host "Root: $root"
git checkout main 2>$null | Out-Null
gt checkout main 2>$null | Out-Null

# --- Slice definitions (story order, dependency-first) ---
$slices = @(
    @{
        Branch = 'stack/mission-editor/me-000-production-plan'
        Message = 'docs(mission-editor): ME-000 milestone epic stories and program plan'
        Paths = @(
            'production/milestones/mission-editor-v1.md',
            'production/epics/agentic-mission-editor-v1',
            'docs/superpowers/plans/2026-06-14-mission-editor-v1-program.md'
        )
    },
    @{
        Branch = 'stack/mission-editor/me-001-expand-metadata'
        Message = 'feat(mission-editor): ME-001 expand scenario metadata DTO'
        Paths = @('src/ProjectAegis.Data/Scenario/Authoring/ScenarioMetadataDto.cs')
    },
    @{
        Branch = 'stack/mission-editor/me-002-canonical-sides-orbat'
        Message = 'feat(mission-editor): ME-002 canonical sides ORBAT reference points'
        Paths = @(
            'src/ProjectAegis.Data/Scenario/Authoring/ScenarioCanonicalSectionsDto.cs',
            'src/ProjectAegis.Data/Scenario/Authoring/ScenarioDocumentDto.cs',
            'src/ProjectAegis.Data/Scenario/Authoring/ScenarioDocumentEditor.cs'
        )
    },
    @{
        Branch = 'stack/mission-editor/me-003-stable-json'
        Message = 'feat(mission-editor): ME-003 stable JSON serialization AC-6'
        Paths = @(
            'src/ProjectAegis.Data/Scenario/Authoring/ScenarioStableJsonWriter.cs',
            'tools/ci/smoke-ac6.sh'
        )
    },
    @{
        Branch = 'stack/mission-editor/me-004-aegis-scenario-zip'
        Message = 'feat(mission-editor): ME-004 aegis-scenario ZIP package'
        Paths = @('src/ProjectAegis.Data/Scenario/Authoring/AegisScenarioPackage.cs')
    },
    @{ Branch = 'stack/mission-editor/me-005-reserved-ops-timeline'; Message = 'chore(mission-editor): ME-005 reserved operations timeline schema slot'; Paths = @() },
    @{ Branch = 'stack/mission-editor/me-006-reserved-events-slot'; Message = 'chore(mission-editor): ME-006 reserved events schema slot'; Paths = @() },
    @{ Branch = 'stack/mission-editor/me-007-reserved-legacy-adapter'; Message = 'chore(mission-editor): ME-007 reserved legacy JSON adapter'; Paths = @() },
    @{ Branch = 'stack/mission-editor/me-008-reserved-migration-hook'; Message = 'chore(mission-editor): ME-008 reserved schema migration hook'; Paths = @() },
    @{
        Branch = 'stack/mission-editor/me-009-editorstate-schema-lint'
        Message = 'feat(mission-editor): ME-009 AC-9 editorState schema lint'
        Paths = @(
            'src/ProjectAegis.Data/Validation/EditorStateSchemaLint.cs',
            'src/ProjectAegis.Data/Validation/ValidationConfigLoader.cs',
            'src/ProjectAegis.Data/Validation/ValidationConfig.cs',
            'assets/data/editor/validation-config.json'
        )
    },
    @{
        Branch = 'stack/mission-editor/me-010-event-dtos'
        Message = 'feat(mission-editor): ME-010 P0 event trigger action DTOs'
        Paths = @()
    },
    @{
        Branch = 'stack/mission-editor/me-011-event-mcp-verbs'
        Message = 'feat(mission-editor): ME-011 event_add event_validate MCP verbs'
        Paths = @('src/ProjectAegis.MissionEditor.Cli/EventCommands.cs')
    },
    @{
        Branch = 'stack/mission-editor/me-012-fire-order'
        Message = 'feat(mission-editor): ME-012 fire_order stable sort'
        Paths = @('src/ProjectAegis.Data/Scenario/Authoring/EventFireOrderCalculator.cs')
    },
    @{
        Branch = 'stack/mission-editor/me-013-ac2-simulate-determinism'
        Message = 'feat(mission-editor): ME-013 AC-2 parallel simulate determinism'
        Paths = @('src/ProjectAegis.MissionEditor.Cli/ScenarioSimulateSampleCommand.cs')
    },
    @{
        Branch = 'stack/mission-editor/me-014-event-debugger-projection'
        Message = 'feat(mission-editor): ME-014 event debugger JSON projection AC-7'
        Paths = @('src/ProjectAegis.Data/Scenario/Authoring/EventDebuggerJsonProjection.cs')
    },
    @{ Branch = 'stack/mission-editor/me-015-reserved-event-runtime'; Message = 'chore(mission-editor): ME-015 reserved event runtime evaluator'; Paths = @() },
    @{ Branch = 'stack/mission-editor/me-016-reserved-mission-timeline'; Message = 'chore(mission-editor): ME-016 reserved mission timeline integration'; Paths = @() },
    @{ Branch = 'stack/mission-editor/me-017-reserved-variable-store'; Message = 'chore(mission-editor): ME-017 reserved variable store runtime'; Paths = @() },
    @{ Branch = 'stack/mission-editor/me-018-reserved-side-posture'; Message = 'chore(mission-editor): ME-018 reserved side posture model'; Paths = @() },
    @{ Branch = 'stack/mission-editor/me-019-reserved-score-model'; Message = 'chore(mission-editor): ME-019 reserved score model'; Paths = @() },
    @{
        Branch = 'stack/mission-editor/me-020-support-ferry-validation'
        Message = 'feat(mission-editor): ME-020 support and ferry validation rules'
        Paths = @(
            'src/ProjectAegis.Data/Validation/Rules/ValidationRules.cs',
            'src/ProjectAegis.Data/Validation/ScenarioValidationEngine.cs'
        )
    },
    @{
        Branch = 'stack/mission-editor/me-021-support-ferry-mcp'
        Message = 'feat(mission-editor): ME-021 support ferry MCP verbs'
        Paths = @(
            'src/ProjectAegis.MissionEditor.Cli/MissionAddSupportCommand.cs',
            'src/ProjectAegis.MissionEditor.Cli/MissionAddFerryCommand.cs'
        )
    },
    @{
        Branch = 'stack/mission-editor/me-022-reference-point-set'
        Message = 'feat(mission-editor): ME-022 reference_point_set MCP'
        Paths = @('src/ProjectAegis.MissionEditor.Cli/ReferencePointSetCommand.cs')
    },
    @{
        Branch = 'stack/mission-editor/me-023-scenario-load-save'
        Message = 'feat(mission-editor): ME-023 mission assign units scenario load save'
        Paths = @('src/ProjectAegis.MissionEditor.Cli/ScenarioLoadSaveCommands.cs')
    },
    @{ Branch = 'stack/mission-editor/me-024-ac5-headless-sample'; Message = 'chore(mission-editor): ME-024 AC-5 headless sample four missions'; Paths = @() },
    @{
        Branch = 'stack/mission-editor/me-025-doctrine-inheritance'
        Message = 'feat(mission-editor): ME-025 AC-4 doctrine inheritance'
        Paths = @('src/ProjectAegis.Data/Validation/ScenarioDoctrineResolver.cs')
    },
    @{ Branch = 'stack/mission-editor/me-026-reserved-mining-cargo'; Message = 'chore(mission-editor): ME-026 reserved mining cargo P1 types'; Paths = @() },
    @{ Branch = 'stack/mission-editor/me-027-reserved-briefing-fields'; Message = 'chore(mission-editor): ME-027 reserved briefing side fields'; Paths = @() },
    @{ Branch = 'stack/mission-editor/me-028-reserved-realism-toggles'; Message = 'chore(mission-editor): ME-028 reserved realism toggles'; Paths = @() },
    @{ Branch = 'stack/mission-editor/me-029-reserved-time-compression'; Message = 'chore(mission-editor): ME-029 reserved time compression limits'; Paths = @() },
    @{
        Branch = 'stack/mission-editor/me-030-save-export-gate'
        Message = 'feat(mission-editor): ME-030 AC-12 save vs export gate'
        Paths = @('src/ProjectAegis.Data/Validation/ScenarioSaveExportGate.cs')
    },
    @{
        Branch = 'stack/mission-editor/me-031-teleportunit-export-strip'
        Message = 'feat(mission-editor): ME-031 AC-11 TeleportUnit export strip'
        Paths = @('src/ProjectAegis.Data/Scenario/Authoring/ScenarioExportTransformer.cs')
    },
    @{ Branch = 'stack/mission-editor/me-032-scenario-export-brief'; Message = 'chore(mission-editor): ME-032 scenario export brief CLI wiring'; Paths = @() },
    @{ Branch = 'stack/mission-editor/me-033-ac10-editversion-conflict'; Message = 'chore(mission-editor): ME-033 AC-10 mutating tools editVersion conflict reject'; Paths = @() },
    @{
        Branch = 'stack/mission-editor/me-034-event-complexity-warnings'
        Message = 'feat(mission-editor): ME-034 event complexity warnings'
        Paths = @('src/ProjectAegis.Data/Validation/EventComplexityAnalyzer.cs')
    },
    @{ Branch = 'stack/mission-editor/me-035-reserved-nl-authoring'; Message = 'chore(mission-editor): ME-035 reserved NL authoring hook'; Paths = @() },
    @{ Branch = 'stack/mission-editor/me-036-reserved-agent-provenance'; Message = 'chore(mission-editor): ME-036 reserved agent provenance'; Paths = @() },
    @{ Branch = 'stack/mission-editor/me-037-reserved-cmo-import'; Message = 'chore(mission-editor): ME-037 reserved CMO import'; Paths = @() },
    @{ Branch = 'stack/mission-editor/me-038-reserved-lua-shim'; Message = 'chore(mission-editor): ME-038 reserved Lua shim'; Paths = @() },
    @{ Branch = 'stack/mission-editor/me-039-reserved-workshop'; Message = 'chore(mission-editor): ME-039 reserved workshop sharing'; Paths = @() },
    @{
        Branch = 'stack/mission-editor/me-040-scenarioeditorhost-unity'
        Message = 'feat(mission-editor): ME-040 ScenarioEditorHost Unity shell'
        Paths = @('src/ProjectAegis.Delegation.UnityAdapter/Editor/ScenarioEditorHostAdapter.cs')
    },
    @{ Branch = 'stack/mission-editor/me-041-map-orbat-tree'; Message = 'chore(mission-editor): ME-041 map ORBAT tree unit placement'; Paths = @() },
    @{ Branch = 'stack/mission-editor/me-042-patrol-zone-gesture'; Message = 'chore(mission-editor): ME-042 patrol zone draw gesture'; Paths = @() },
    @{ Branch = 'stack/mission-editor/me-043-invalid-overlay-undo'; Message = 'chore(mission-editor): ME-043 invalid overlay undo'; Paths = @() },
    @{
        Branch = 'stack/mission-editor/me-044-ac8-unity-roundtrip'
        Message = 'feat(mission-editor): ME-044 AC-8 Unity round-trip tests'
        Paths = @('src/ProjectAegis.Delegation.UnityAdapter.Tests/Editor/ScenarioEditorHostAdapterTests.cs')
    },
    @{ Branch = 'stack/mission-editor/me-045-reserved-cesium-map'; Message = 'chore(mission-editor): ME-045 reserved Cesium map integration'; Paths = @() },
    @{ Branch = 'stack/mission-editor/me-046-reserved-play-mode-lock'; Message = 'chore(mission-editor): ME-046 reserved editor play mode lock'; Paths = @() },
    @{ Branch = 'stack/mission-editor/me-047-reserved-headless-edit-cli'; Message = 'chore(mission-editor): ME-047 reserved headless edit mode CLI'; Paths = @() },
    @{ Branch = 'stack/mission-editor/me-048-reserved-validation-panel'; Message = 'chore(mission-editor): ME-048 reserved editor validation panel'; Paths = @() },
    @{ Branch = 'stack/mission-editor/me-049-reserved-mission-list-panel'; Message = 'chore(mission-editor): ME-049 reserved mission list panel'; Paths = @() },
    @{
        Branch = 'stack/mission-editor/me-050-consolidate-editor-tests'
        Message = 'test(mission-editor): ME-050 consolidate editor test suites'
        Paths = @(
            'src/ProjectAegis.Data.Tests/Scenario/MissionEditorV1Tests.cs',
            'src/ProjectAegis.MissionEditor.Cli.Tests/MissionEditorV1CliTests.cs'
        )
    },
    @{ Branch = 'stack/mission-editor/me-051-replay-verify-golden'; Message = 'chore(mission-editor): ME-051 replay verify golden editor samples'; Paths = @() },
    @{
        Branch = 'stack/mission-editor/me-052-req11-tracker-mvp'
        Message = 'docs(mission-editor): ME-052 Req 11 tracker MVP status'
        Paths = @('Game-Requirements/requirements/11-Agentic-Mission-Editor-MVP-STATUS.md')
    },
    @{ Branch = 'stack/mission-editor/me-053-qa-plan-exit-gate'; Message = 'chore(mission-editor): ME-053 QA plan program exit gate'; Paths = @() },
    @{
        Branch = 'stack/mission-editor/me-054-cli-program-wiring'
        Message = 'feat(mission-editor): wire Mission Editor CLI commands in Program.cs'
        Paths = @('src/ProjectAegis.MissionEditor.Cli/Program.cs')
    }
)

$i = 0
foreach ($slice in $slices) {
    $i++
    Write-Host "`n[$i/$($slices.Count)] $($slice.Branch)"
    Invoke-GtCreate -Branch $slice.Branch -Message $slice.Message -Paths $slice.Paths
}

Write-Host "`n=== Remaining unstaged ==="
git status --short
Write-Host "`n=== Stack (gt log short) ==="
gt log short
