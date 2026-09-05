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
}
