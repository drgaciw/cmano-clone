namespace ProjectAegis.Delegation.UnityAdapter.Tests.PlayEntry;

using Core;
using Orchestration;
using Data.Scenario;
using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.Delegation.UnityAdapter.PlayEntry;
using NUnit.Framework;

/// <summary>
/// S123-03 W2-CORE-01 briefing content panel, S123-04 DRG-246 MODE-01 top-bar selector,
/// S123-05 W2-MODE-01 side picker, S123-06 W3-MODE-01 Begin button gate — presentation models only.
/// </summary>
[TestFixture]
public sealed class PlayEntryPresentationTests
{
    [Test]
    public void Briefing_without_package_reports_no_package_state()
    {
        var briefing = BriefingContentPresentation.Bind(new PlayEntrySession());

        Assert.That(briefing.State, Is.EqualTo(BriefingContentState.NoPackage));
        Assert.That(briefing.StatusLabel, Is.EqualTo(BriefingContentPresentation.NoPackageLabel));
        Assert.That(briefing.Narrative, Is.Null);
        Assert.That(briefing.MissionLines, Is.Empty);
    }

    [Test]
    public void Briefing_for_baltic_package_shows_explicit_missing_content_and_existing_mission_rows()
    {
        var session = new PlayEntrySession();
        Assert.That(session.TryLoad(PlayEntrySessionTests.BalticEntry()).Succeeded, Is.True);

        var briefing = BriefingContentPresentation.Bind(session);

        Assert.That(briefing.State, Is.EqualTo(BriefingContentState.MissingContent));
        Assert.That(briefing.StatusLabel, Is.EqualTo(BriefingContentPresentation.MissingContentLabel));
        Assert.That(briefing.Narrative, Is.Null);
        Assert.That(briefing.Title, Is.EqualTo(session.State.ScenarioId));
        Assert.That(briefing.PolicyLabel, Is.EqualTo("POLICY: baltic-patrol-catalog · TL-2 · SEED 42"));
        Assert.That(briefing.MissionLines, Is.EqualTo(new[] { "patrol-1 · Patrol · units: u1" }));
    }

    [Test]
    public void Briefing_binds_authored_title_and_description_when_present()
    {
        var package = new ScenarioPackage("auth-1", "baltic-patrol", "snap-1", seed: 7);
        var document = new ScenarioDocumentDto
        {
            Metadata = new ScenarioMetadataDto
            {
                Title = "  Gotland Watch ",
                Description = "  Hold the patrol box until relieved.  ",
            },
            Missions = new[]
            {
                new ScenarioMissionDto { Id = "m1", Type = "Patrol", AssignedUnitIds = new[] { "u1", "u2" } },
                new ScenarioMissionDto { Id = "m2", Type = "Strike" },
            },
        };

        var briefing = BriefingContentPresentation.Bind(package, document);

        Assert.That(briefing.State, Is.EqualTo(BriefingContentState.Available));
        Assert.That(briefing.Title, Is.EqualTo("Gotland Watch"));
        Assert.That(briefing.Narrative, Is.EqualTo("Hold the patrol box until relieved."));
        Assert.That(briefing.StatusLabel, Is.EqualTo(BriefingContentPresentation.AvailableLabel));
        Assert.That(briefing.MissionLines, Is.EqualTo(new[]
        {
            "m1 · Patrol · units: u1, u2",
            "m2 · Strike · units: none",
        }));
    }

    [Test]
    public void Briefing_whitespace_description_is_missing_content_not_blank_text()
    {
        var package = new ScenarioPackage("ws", "baltic-patrol", "snap-1");
        var document = new ScenarioDocumentDto { Metadata = new ScenarioMetadataDto { Description = "   " } };

        var briefing = BriefingContentPresentation.Bind(package, document);

        Assert.That(briefing.State, Is.EqualTo(BriefingContentState.MissingContent));
        Assert.That(briefing.Narrative, Is.Null);
    }

    [Test]
    public void Mode_selector_offers_human_mixed_ava_mapped_to_simulation_mode_enum()
    {
        var selector = C2ModeSelectorPresentation.Project(PlayEntryState.Empty);

        Assert.That(selector.Options.Select(o => o.Kind), Is.EqualTo(new[]
        {
            SimulationModeKind.Human,
            SimulationModeKind.Mixed,
            SimulationModeKind.AgentVsAgent,
        }));
        Assert.That(selector.Options.Select(o => o.Label), Is.EqualTo(new[] { "Human", "Mixed", "AvA" }));
        Assert.That(selector.IsEnabled, Is.False);
        Assert.That(selector.Options.Any(o => o.IsSelected), Is.False);
        Assert.That(selector.TopBarModeLabel, Is.EqualTo(C2ModeSelectorPresentation.UnsetLabel));
    }

    [Test]
    public void Mode_selector_enabled_in_planning_and_marks_selection()
    {
        var session = LoadedBaltic();
        session.TrySelectMode(SimulationModeKind.AgentVsAgent);

        var selector = C2ModeSelectorPresentation.Project(session.State);

        Assert.That(selector.IsEnabled, Is.True);
        Assert.That(selector.Options.Single(o => o.IsSelected).Kind, Is.EqualTo(SimulationModeKind.AgentVsAgent));
        Assert.That(selector.TopBarModeLabel, Is.EqualTo("AvA"));
    }

    [Test]
    public void Mode_selector_locks_after_begin()
    {
        var session = LoadedBaltic();
        session.TrySelectMode(SimulationModeKind.Mixed);
        session.TrySelectSide(PlaySide.Friendly);
        var (friendly, opposing) = PlayEntrySessionTests.RegisterBalticForces(session.Bridge!);
        session.TryBeginExecution(new[] { friendly }, new[] { opposing });

        var selector = C2ModeSelectorPresentation.Project(session.State);
        var picker = PlaySidePickerPresentation.Project(session.State);

        Assert.That(selector.IsEnabled, Is.False);
        Assert.That(selector.TopBarModeLabel, Is.EqualTo("Mixed"));
        Assert.That(picker.IsVisible, Is.True);
        Assert.That(picker.IsEnabled, Is.False);
    }

    [Test]
    public void Side_picker_hidden_until_mode_selected()
    {
        var session = LoadedBaltic();

        var picker = PlaySidePickerPresentation.Project(session.State);

        Assert.That(picker.IsVisible, Is.False);
        Assert.That(picker.Options, Is.Empty);
    }

    [Test]
    public void Side_picker_after_human_mode_offers_friendly_only()
    {
        var session = LoadedBaltic();
        session.TrySelectMode(SimulationModeKind.Human);

        var picker = PlaySidePickerPresentation.Project(session.State);

        Assert.That(picker.IsVisible, Is.True);
        Assert.That(picker.IsEnabled, Is.True);
        Assert.That(picker.Options.Select(o => o.Side), Is.EqualTo(new[] { PlaySide.Friendly }));
        Assert.That(picker.Prompt, Is.EqualTo(PlaySidePickerPresentation.CommandPrompt));
    }

    [Test]
    public void Side_picker_after_mixed_mode_offers_both_sides_and_marks_pick()
    {
        var session = LoadedBaltic();
        session.TrySelectMode(SimulationModeKind.Mixed);
        session.TrySelectSide(PlaySide.Opposing);

        var picker = PlaySidePickerPresentation.Project(session.State);

        Assert.That(picker.Options.Select(o => o.Label), Is.EqualTo(new[] { "Blue (friendly)", "Red (opposing)" }));
        Assert.That(picker.Options.Single(o => o.IsSelected).Side, Is.EqualTo(PlaySide.Opposing));
    }

    [Test]
    public void Side_picker_after_ava_mode_uses_observe_prompt()
    {
        var session = LoadedBaltic();
        session.TrySelectMode(SimulationModeKind.AgentVsAgent);

        var picker = PlaySidePickerPresentation.Project(session.State);

        Assert.That(picker.Prompt, Is.EqualTo(PlaySidePickerPresentation.ObservePrompt));
        Assert.That(picker.Options, Has.Count.EqualTo(2));
    }

    [Test]
    public void Begin_button_disabled_with_reason_when_mode_and_side_missing()
    {
        var button = BeginExecutionButtonPresentation.Project(LoadedBaltic().EvaluateBeginGate());

        Assert.That(button.IsEnabled, Is.False);
        Assert.That(button.BlockedReasonLabel, Is.EqualTo("Select a simulation mode · Select a play side"));
    }

    [Test]
    public void Begin_button_disabled_with_reason_when_side_missing()
    {
        var session = LoadedBaltic();
        session.TrySelectMode(SimulationModeKind.Mixed);

        var button = BeginExecutionButtonPresentation.Project(session.EvaluateBeginGate());

        Assert.That(button.IsEnabled, Is.False);
        Assert.That(button.BlockedReasonLabel, Is.EqualTo("Select a play side"));
    }

    [Test]
    public void Begin_button_enabled_when_mode_and_side_selected()
    {
        var session = LoadedBaltic();
        session.TrySelectMode(SimulationModeKind.Mixed);
        session.TrySelectSide(PlaySide.Friendly);

        var button = BeginExecutionButtonPresentation.Project(session.EvaluateBeginGate());

        Assert.That(button.IsEnabled, Is.True);
        Assert.That(button.Label, Is.EqualTo(BeginExecutionButtonPresentation.BeginLabel));
        Assert.That(button.BlockedReasonLabel, Is.Null);
    }

    [Test]
    public void Begin_button_without_package_explains_load_first()
    {
        var button = BeginExecutionButtonPresentation.Project(new PlayEntrySession().EvaluateBeginGate());

        Assert.That(button.IsEnabled, Is.False);
        Assert.That(button.BlockedReasonLabel, Is.EqualTo("Load a scenario package"));
    }

    private static PlayEntrySession LoadedBaltic()
    {
        var session = new PlayEntrySession();
        var result = session.TryLoad(PlayEntrySessionTests.BalticEntry());
        Assert.That(result.Succeeded, Is.True, result.Message);
        Assert.That(session.State.Phase, Is.EqualTo(SimulationPhase.Planning));
        return session;
    }
}
