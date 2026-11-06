using NUnit.Framework;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.UnityAdapter.Bridge;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Bridge;

public sealed class CombatPresentationFrameBridgeTests
{
    [Test]
    public void Replay_cutoff_excludes_future_combat_and_assessment_without_mutating_log()
    {
        var log = new DecisionLog();
        log.AppendEngagement(new EngagementRecord(0, 10, 10, new TargetId("s"), 42, true,
            VictimTargetId: new TargetId("t"), WeaponFamilyId: "Missile", HasFireControlTrack: true));
        var before = log.ChronologicalEntries().Count;
        var early = CombatPresentationFrameBridge.Build(log, SliceAContactFrame.Empty, 5);
        Assert.That(early.Events.Events, Is.Empty);
        Assert.That(early.Explanations, Is.Empty);
        var current = CombatPresentationFrameBridge.Build(log, SliceAContactFrame.Empty, 10);
        Assert.That(current.Explanations.Single().HasFireControlTrack, Is.True);
        Assert.That(current.Explanations.Single().CorrelationId, Is.EqualTo(log.Engagements[0].SequenceId));
        Assert.That(log.ChronologicalEntries().Count, Is.EqualTo(before));
    }

    [Test]
    public void Explanations_are_sourced_from_combat_event_execution_facts()
    {
        var log = new DecisionLog();
        log.AppendEngagement(new EngagementRecord(0, 2, 2, new TargetId("s1"), 7, true,
            VictimTargetId: new TargetId("t1"), WeaponFamilyId: "Missile", SalvoSize: 2, HasFireControlTrack: true));
        log.AppendEngagement(new EngagementRecord(0, 3, 3, new TargetId("s2"), 0, false, "ROE_WEAPONS_TIGHT",
            new TargetId("t2"), "Gun"));

        var frame = CombatPresentationFrameBridge.Build(log, SliceAContactFrame.Empty, 3);

        Assert.That(frame.Explanations, Is.EqualTo(frame.Events.Execution.Select(f =>
            new CombatEngagementExplanation(
                f.CorrelationId, f.ShooterId, f.TargetId, f.HasFireControlTrack, f.SalvoSize))));
        Assert.That(frame.Explanations, Has.Count.EqualTo(2));
    }

    [Test]
    public void Frame_bridge_reads_combat_facts_only_through_the_combat_event_contract()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "ProjectAegis.sln")))
        {
            dir = dir.Parent;
        }

        Assert.That(dir, Is.Not.Null, "repo root not found");
        var source = File.ReadAllText(Path.Combine(
            dir!.FullName, "src", "ProjectAegis.Delegation.UnityAdapter", "Bridge", "CombatPresentationFrame.cs"));
        Assert.That(source, Does.Not.Contain(".Engagements"));
        Assert.That(source, Does.Not.Contain(".EngagementOutcomes"));
        Assert.That(source, Does.Contain("CombatEventLogProjection.Build"));
    }
}
