using System.Collections.Generic;
using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

/// <summary>SE-UX P2.1/P2.2: Unified Scenario Editor shell (Map | Mission Board) + product chrome.</summary>
[TestFixture]
public sealed class ScenarioEditorShellProjectionTests
{
    [Test]
    public void Bind_default_is_map_mode()
    {
        var state = ScenarioEditorShellProjection.Bind();

        Assert.That(state.Mode, Is.EqualTo(ScenarioEditorShellMode.Map));
        Assert.That(state.ModeTitle, Is.EqualTo("MAP AUTHORING"));
        Assert.That(state.RootUssClass, Is.EqualTo("scenario-editor-shell--map"));
        Assert.That(state.MapTabActive, Is.True);
        Assert.That(state.MissionBoardTabActive, Is.False);
        Assert.That(state.MapContentVisible, Is.True);
        Assert.That(state.MissionBoardContentVisible, Is.False);
        Assert.That(state.EventsTabEnabled, Is.False);
        Assert.That(state.PlayBlocked, Is.False);
        Assert.That(state.ScenarioTitle, Is.EqualTo("Untitled Scenario"));
        Assert.That(state.IsDirty, Is.False);
        Assert.That(state.EditVersion, Is.EqualTo(0));
        Assert.That(state.DirtyLabel, Is.EqualTo("saved"));
        Assert.That(state.SessionOpen, Is.False);
        Assert.That(state.SaveEnabled, Is.False);
        Assert.That(state.PlayEnabled, Is.False);
        Assert.That(state.SampleEnabled, Is.False);
        Assert.That(state.LoadEnabled, Is.True);
        Assert.That(state.FindingsFilter, Is.EqualTo(ScenarioEditorFindingsFilter.All));
        Assert.That(state.FindingsSummaryText, Is.EqualTo("Findings: 0 errors · 0 warnings"));
        Assert.That(state.SelectionInspectorText, Is.EqualTo("No selection"));
        Assert.That(state.VisibleFindingRows, Is.Empty);
    }

    [Test]
    public void Bind_mission_board_mode_swaps_visibility_and_title()
    {
        var state = ScenarioEditorShellProjection.Bind(
            ScenarioEditorShellMode.MissionBoard,
            selectedEntityId: "patrol-asuw",
            statusSummary: "Findings: 2 errors",
            errorFindingCount: 2,
            warningFindingCount: 1);

        Assert.That(state.Mode, Is.EqualTo(ScenarioEditorShellMode.MissionBoard));
        Assert.That(state.ModeTitle, Is.EqualTo("MISSION BOARD"));
        Assert.That(state.RootUssClass, Is.EqualTo("scenario-editor-shell--mission-board"));
        Assert.That(state.MapTabActive, Is.False);
        Assert.That(state.MissionBoardTabActive, Is.True);
        Assert.That(state.MapContentVisible, Is.False);
        Assert.That(state.MissionBoardContentVisible, Is.True);
        Assert.That(state.SelectedEntityId, Is.EqualTo("patrol-asuw"));
        Assert.That(state.StatusSummary, Is.EqualTo("Findings: 2 errors"));
        Assert.That(state.ErrorFindingCount, Is.EqualTo(2));
        Assert.That(state.WarningFindingCount, Is.EqualTo(1));
        Assert.That(state.PlayBlocked, Is.True);
        Assert.That(state.PlayBlockReason, Is.EqualTo("Blocked by 2 error findings"));
        Assert.That(state.SelectionInspectorText, Is.EqualTo("Selected: patrol-asuw"));
        Assert.That(state.FindingsSummaryText, Is.EqualTo("Findings: 2 errors · 1 warning"));
    }

    [Test]
    public void Bind_singular_error_play_reason()
    {
        var state = ScenarioEditorShellProjection.Bind(errorFindingCount: 1);

        Assert.That(state.PlayBlocked, Is.True);
        Assert.That(state.PlayBlockReason, Is.EqualTo("Blocked by 1 error finding"));
        Assert.That(state.FindingsSummaryText, Is.EqualTo("Findings: 1 error · 0 warnings"));
    }

    [Test]
    public void WithMode_preserves_selection_status_and_finding_counts()
    {
        var map = ScenarioEditorShellProjection.Bind(
            ScenarioEditorShellMode.Map,
            selectedEntityId: "u1",
            statusSummary: "Tool: Move",
            errorFindingCount: 1,
            warningFindingCount: 0);

        var board = ScenarioEditorShellProjection.WithMode(map, ScenarioEditorShellMode.MissionBoard);

        Assert.That(board.Mode, Is.EqualTo(ScenarioEditorShellMode.MissionBoard));
        Assert.That(board.SelectedEntityId, Is.EqualTo("u1"));
        Assert.That(board.StatusSummary, Is.EqualTo("Tool: Move"));
        Assert.That(board.ErrorFindingCount, Is.EqualTo(1));
        Assert.That(board.PlayBlocked, Is.True);
        Assert.That(board.MissionBoardContentVisible, Is.True);
    }

    [Test]
    public void CycleMode_toggles_map_and_mission_board()
    {
        var map = ScenarioEditorShellProjection.Bind(ScenarioEditorShellMode.Map);
        var board = ScenarioEditorShellProjection.CycleMode(map);
        var back = ScenarioEditorShellProjection.CycleMode(board);

        Assert.That(board.Mode, Is.EqualTo(ScenarioEditorShellMode.MissionBoard));
        Assert.That(back.Mode, Is.EqualTo(ScenarioEditorShellMode.Map));
    }

    [Test]
    public void WithFindings_updates_play_block_without_changing_mode()
    {
        var state = ScenarioEditorShellProjection.Bind(ScenarioEditorShellMode.Map);
        var updated = ScenarioEditorShellProjection.WithFindings(state, errorCount: 0, warningCount: 2);

        Assert.That(updated.Mode, Is.EqualTo(ScenarioEditorShellMode.Map));
        Assert.That(updated.PlayBlocked, Is.False);
        Assert.That(updated.WarningFindingCount, Is.EqualTo(2));
        Assert.That(updated.PlayBlockReason, Is.EqualTo(string.Empty));
    }

    [Test]
    public void TabUssClass_marks_active_and_disabled()
    {
        Assert.That(
            ScenarioEditorShellProjection.TabUssClass(isActive: true, isEnabled: true),
            Is.EqualTo("scenario-editor-shell-tab scenario-editor-shell-tab--active"));
        Assert.That(
            ScenarioEditorShellProjection.TabUssClass(isActive: false, isEnabled: false),
            Is.EqualTo("scenario-editor-shell-tab scenario-editor-shell-tab--disabled"));
        Assert.That(
            ScenarioEditorShellProjection.TabUssClass(isActive: false, isEnabled: true),
            Is.EqualTo("scenario-editor-shell-tab"));
    }

    [Test]
    public void WithTopBar_session_open_dirty_enables_save_and_blocks_play_on_errors()
    {
        var baseState = ScenarioEditorShellProjection.Bind(errorFindingCount: 1, warningFindingCount: 0);
        var state = ScenarioEditorShellProjection.WithTopBar(
            baseState,
            scenarioTitle: "Baltic Patrol v3",
            isDirty: true,
            editVersion: 7,
            sessionOpen: true,
            undoEnabled: true,
            redoEnabled: false);

        Assert.That(state.ScenarioTitle, Is.EqualTo("Baltic Patrol v3"));
        Assert.That(state.IsDirty, Is.True);
        Assert.That(state.EditVersion, Is.EqualTo(7));
        Assert.That(state.DirtyLabel, Is.EqualTo("• unsaved"));
        Assert.That(state.SessionOpen, Is.True);
        Assert.That(state.SaveEnabled, Is.True);
        Assert.That(state.LoadEnabled, Is.True);
        Assert.That(state.UndoEnabled, Is.True);
        Assert.That(state.RedoEnabled, Is.False);
        Assert.That(state.PlayBlocked, Is.True);
        Assert.That(state.PlayEnabled, Is.False);
        Assert.That(state.SampleEnabled, Is.False);
    }

    [Test]
    public void WithTopBar_session_open_clean_enables_play_when_no_errors()
    {
        var baseState = ScenarioEditorShellProjection.Bind(errorFindingCount: 0, warningFindingCount: 3);
        var state = ScenarioEditorShellProjection.WithTopBar(
            baseState,
            scenarioTitle: "Clean Theater",
            isDirty: false,
            editVersion: 2,
            sessionOpen: true);

        Assert.That(state.DirtyLabel, Is.EqualTo("saved"));
        Assert.That(state.SaveEnabled, Is.True);
        Assert.That(state.PlayEnabled, Is.True);
        Assert.That(state.SampleEnabled, Is.True);
        Assert.That(state.PlayBlocked, Is.False);
    }

    [Test]
    public void FilterFindings_all_errors_warnings()
    {
        var rows = new List<ScenarioEditorFindingRow>
        {
            ScenarioEditorFindingsProjection.CreateRow(
                ScenarioEditorFindingSeverity.Error, "ORBAT_MISSING", "Missing unit", "u1", preferMissionBoard: false),
            ScenarioEditorFindingsProjection.CreateRow(
                ScenarioEditorFindingSeverity.Warning, "RP_NEAR", "Near edge", "zone-1", preferMissionBoard: false),
            ScenarioEditorFindingsProjection.CreateRow(
                ScenarioEditorFindingSeverity.Error, "STRIKE_NO_TARGETS", "No targets", "strike-1", preferMissionBoard: true),
        };

        Assert.That(ScenarioEditorFindingsProjection.Filter(rows, ScenarioEditorFindingsFilter.All), Has.Count.EqualTo(3));
        Assert.That(ScenarioEditorFindingsProjection.Filter(rows, ScenarioEditorFindingsFilter.Errors), Has.Count.EqualTo(2));
        Assert.That(ScenarioEditorFindingsProjection.Filter(rows, ScenarioEditorFindingsFilter.Warnings), Has.Count.EqualTo(1));
    }

    [Test]
    public void FormatRowLabel_includes_text_severity_tag()
    {
        var row = ScenarioEditorFindingsProjection.CreateRow(
            ScenarioEditorFindingSeverity.Error, "ORBAT_MISSING", "Missing unit", "u1", preferMissionBoard: false);

        Assert.That(row.SeverityTag, Is.EqualTo("ERROR"));
        Assert.That(
            ScenarioEditorFindingsProjection.FormatRowLabel(row),
            Is.EqualTo("[ERROR] ORBAT_MISSING — Missing unit"));
    }

    [Test]
    public void WithFindingRows_applies_filter_and_counts()
    {
        var rows = new[]
        {
            ScenarioEditorFindingsProjection.CreateRow(
                ScenarioEditorFindingSeverity.Error, "A", "a", "e1", preferMissionBoard: false),
            ScenarioEditorFindingsProjection.CreateRow(
                ScenarioEditorFindingSeverity.Warning, "B", "b", null, preferMissionBoard: false),
        };

        var state = ScenarioEditorShellProjection.WithFindingRows(
            ScenarioEditorShellProjection.Bind(),
            rows,
            ScenarioEditorFindingsFilter.Errors);

        Assert.That(state.ErrorFindingCount, Is.EqualTo(1));
        Assert.That(state.WarningFindingCount, Is.EqualTo(1));
        Assert.That(state.PlayBlocked, Is.True);
        Assert.That(state.FindingsFilter, Is.EqualTo(ScenarioEditorFindingsFilter.Errors));
        Assert.That(state.FindingRows, Has.Count.EqualTo(2));
        Assert.That(state.VisibleFindingRows, Has.Count.EqualTo(1));
        Assert.That(state.VisibleFindingRows[0].Code, Is.EqualTo("A"));
    }

    [Test]
    public void JumpToFinding_selects_entity_and_prefers_mission_board_for_mission_codes()
    {
        var map = ScenarioEditorShellProjection.Bind(ScenarioEditorShellMode.Map);
        var missionFinding = ScenarioEditorFindingsProjection.CreateRow(
            ScenarioEditorFindingSeverity.Error,
            "STRIKE_NO_TARGETS",
            "No targets",
            "strike-1",
            preferMissionBoard: true);

        var jumped = ScenarioEditorShellProjection.JumpToFinding(map, missionFinding);

        Assert.That(jumped.Mode, Is.EqualTo(ScenarioEditorShellMode.MissionBoard));
        Assert.That(jumped.SelectedEntityId, Is.EqualTo("strike-1"));
        Assert.That(jumped.SelectionInspectorText, Is.EqualTo("Selected: strike-1"));
    }

    [Test]
    public void JumpToFinding_keeps_mode_when_not_mission_preferring()
    {
        var map = ScenarioEditorShellProjection.Bind(ScenarioEditorShellMode.Map);
        var mapFinding = ScenarioEditorFindingsProjection.CreateRow(
            ScenarioEditorFindingSeverity.Warning,
            "ZONE_INVALID",
            "Invalid geometry",
            "zone-9",
            preferMissionBoard: false);

        var jumped = ScenarioEditorShellProjection.JumpToFinding(map, mapFinding);

        Assert.That(jumped.Mode, Is.EqualTo(ScenarioEditorShellMode.Map));
        Assert.That(jumped.SelectedEntityId, Is.EqualTo("zone-9"));
    }

    [Test]
    public void PreferMissionBoard_heuristic_for_common_mission_codes()
    {
        Assert.That(ScenarioEditorFindingsProjection.PreferMissionBoardForCode("STRIKE_NO_TARGETS"), Is.True);
        Assert.That(ScenarioEditorFindingsProjection.PreferMissionBoardForCode("MISSION_UNASSIGNED"), Is.True);
        Assert.That(ScenarioEditorFindingsProjection.PreferMissionBoardForCode("ORBAT_MISSING"), Is.False);
    }

    [Test]
    public void FilterChipUssClass_marks_active_filter()
    {
        Assert.That(
            ScenarioEditorFindingsProjection.FilterChipUssClass(
                ScenarioEditorFindingsFilter.All,
                ScenarioEditorFindingsFilter.All),
            Does.Contain("--active"));
        Assert.That(
            ScenarioEditorFindingsProjection.FilterChipUssClass(
                ScenarioEditorFindingsFilter.Errors,
                ScenarioEditorFindingsFilter.All),
            Does.Not.Contain("--active"));
    }

    [Test]
    public void ActionOpacityClass_marks_disabled_play_controls()
    {
        Assert.That(
            ScenarioEditorShellProjection.ActionEnabledUssClass(enabled: false),
            Is.EqualTo("scenario-editor-shell-action scenario-editor-shell-action--disabled"));
        Assert.That(
            ScenarioEditorShellProjection.ActionEnabledUssClass(enabled: true),
            Is.EqualTo("scenario-editor-shell-action"));
    }
}
