using NUnit.Framework;
using ProjectAegis.Delegation.AfterAction;
using ProjectAegis.Delegation.CombatEvents;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Delegation.UnityAdapter.Presentation;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

public sealed class CommandReviewTimelineTests
{
    private static CombatEvent Row(double time, string shooter = "s", string outcome = "Launch") =>
        new(CombatEventPhase.Firing, shooter, "t", "Gun", outcome, 1, time, (ulong)time, "launch");
    private static CombatPresentationFrame Frame(double time, params CombatEvent[] rows) =>
        CombatPresentationFrame.Empty with { SimTime = time, Events = new CombatEventSnapshot(rows) };
    private static MapSymbolEntry Pose(float x) => new("s", "Friendly", "S", "s", x, .5f, false);

    [Test]
    public void Filters_combine_all_fields_and_preserve_exact_log_identity()
    {
        var timeline = new CommandReviewTimeline();
        timeline.Capture(Frame(4, Row(1), Row(2, "other"), Row(3, outcome: "Miss")), new[] { Pose(.1f) });
        var rows = timeline.Filter(new AfterActionLedgerFilter("s", "t", "Gun", "Miss"));
        Assert.That(rows, Has.Count.EqualTo(1));
        Assert.That(rows[0].SimTime, Is.EqualTo(3));
        Assert.That(timeline.Filter(new AfterActionLedgerFilter(WeaponFamilyId: "Laser")), Is.Empty);
    }

    [Test]
    public void Inspection_freezes_historical_map_and_explanation_without_future_facts()
    {
        var timeline = new CommandReviewTimeline();
        var early = Row(1);
        timeline.Capture(Frame(1, early), new[] { Pose(.1f) });
        timeline.Capture(Frame(2, early, Row(2)), new[] { Pose(.9f) });
        Assert.That(timeline.Inspect(timeline.Filter(new())[0]), Is.True);
        Assert.That(timeline.DisplayFrame.SimTime, Is.EqualTo(1));
        Assert.That(timeline.DisplayFrame.Events.Events, Has.Count.EqualTo(1));
        Assert.That(timeline.DisplaySymbols[0].NormalizedX, Is.EqualTo(.1f));
        timeline.Capture(Frame(3, early, Row(2), Row(3)), new[] { Pose(.8f) });
        Assert.That(timeline.DisplayFrame.SimTime, Is.EqualTo(1));
        timeline.ReturnToLive();
        Assert.That(timeline.DisplayFrame.SimTime, Is.EqualTo(3));
        Assert.That(timeline.SelectedKey, Is.Null);
    }

    [Test]
    public void Evicted_map_evidence_is_explicitly_unavailable_not_current_pose()
    {
        var timeline = new CommandReviewTimeline(1);
        var early = Row(1);
        timeline.Capture(Frame(1, early), new[] { Pose(.1f) });
        timeline.Capture(Frame(2, early, Row(2)), new[] { Pose(.9f) });
        Assert.That(timeline.Inspect(timeline.Filter(new())[0]), Is.True);
        Assert.That(timeline.DisplaySymbols, Is.Empty);
        Assert.That(timeline.StatusLine, Does.Contain("unavailable"));
        Assert.That(timeline.DisplayFrame.Events.Events, Has.Count.EqualTo(1));
    }

    [Test]
    public void Intent_review_does_not_disclose_later_execution_solution()
    {
        var timeline = new CommandReviewTimeline();
        var intent = Row(1) with { Phase = CombatEventPhase.IntentAccepted };
        timeline.Capture(Frame(1, intent, Row(1)) with {
            Explanations = new[] { new CombatEngagementExplanation(1, "s", "t", true, 2) }
        }, new[] { Pose(.1f) });
        Assert.That(timeline.Inspect(timeline.Filter(new())[0]), Is.True);
        Assert.That(timeline.DisplayFrame.Explanations, Is.Empty);
        Assert.That(timeline.Inspect(timeline.Filter(new())[1]), Is.True);
        Assert.That(timeline.DisplayFrame.Explanations, Has.Count.EqualTo(1));
    }

    [Test]
    public void Earlier_event_within_tick_uses_only_previous_tick_map_evidence()
    {
        var timeline = new CommandReviewTimeline();
        timeline.Capture(Frame(0), new[] { Pose(.1f) });
        timeline.Capture(Frame(1, Row(1), Row(1, outcome: "Kill")), new[] { Pose(.9f) });
        Assert.That(timeline.Inspect(timeline.Filter(new())[0]), Is.True);
        Assert.That(timeline.DisplaySymbols[0].NormalizedX, Is.EqualTo(.1f));
        Assert.That(timeline.StatusLine, Does.Contain("captured map t=0"));
        Assert.That(timeline.DisplayFrame.Events.Events, Has.Count.EqualTo(1));
        Assert.That(timeline.Inspect(timeline.Filter(new())[1]), Is.True);
        Assert.That(timeline.DisplaySymbols[0].NormalizedX, Is.EqualTo(.9f));
    }

    [Test]
    public void Fabricated_event_is_rejected_and_rewind_clears_old_run_inspection()
    {
        var timeline = new CommandReviewTimeline();
        timeline.Capture(Frame(2, Row(2)), new[] { Pose(.2f) });
        var row = timeline.Filter(new())[0];
        Assert.That(timeline.Inspect(row with { TargetId = "invented" }), Is.False);
        Assert.That(timeline.Inspect(row), Is.True);
        timeline.Capture(Frame(0), Array.Empty<MapSymbolEntry>());
        Assert.That(timeline.IsInspecting, Is.False);
        Assert.That(timeline.Filter(new()), Is.Empty);
    }
}
