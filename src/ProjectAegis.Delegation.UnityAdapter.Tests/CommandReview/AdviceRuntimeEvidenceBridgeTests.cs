namespace ProjectAegis.Delegation.UnityAdapter.Tests.CommandReview;

using NUnit.Framework;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Delegation.UnityAdapter.CommandReview;

[TestFixture]
public sealed class AdviceRuntimeEvidenceBridgeTests
{
    [Test]
    public void Current_selected_contact_returns_native_facts_without_fictional_scores()
    {
        var frame = SliceAContactFrame.Empty with
        {
            SimTick = 7, SimTime = 7,
            Contacts = [new ContactPictureEntry("c1", "t1", "observer-1", "Tracked", 7, 7)],
        };

        var result = AdviceRuntimeEvidenceBridge.Build(new DelegationBridge(1), new Snapshot(7, "t1"), frame, "c1");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.ThreatAssessment, Is.Null);
        Assert.That(result.ResourceRanking, Is.Null);
        Assert.That(result.MissionPackageFacts, Does.Contain("target:t1"));
        Assert.That(result.MissionPackageFacts, Does.Contain("weapon-range-and-resource-scores:unknown-no-exact-runtime-inputs"));
    }

    [Test]
    public void Missing_selection_or_nonidentical_frame_time_returns_no_evidence()
    {
        var frame = SliceAContactFrame.Empty with { SimTick = 6, SimTime = 6, Contacts = [new ContactPictureEntry("c1", "t1", "obs", "Tracked", 6, 6)] };
        var bridge = new DelegationBridge(1);

        Assert.That(AdviceRuntimeEvidenceBridge.Build(bridge, new Snapshot(7, "t1"), frame, "c1"), Is.Null);
        Assert.That(AdviceRuntimeEvidenceBridge.Build(bridge, new Snapshot(6, "t1"), frame, "other"), Is.Null);
    }

    [Test]
    public void Nonprimary_selected_contact_does_not_inherit_primary_fire_control_fact()
    {
        var frame = SliceAContactFrame.Empty with { SimTick = 5, SimTime = 5, Contacts = [new ContactPictureEntry("c2", "t2", "obs", "Tracked", 5, 5)] };
        var result = AdviceRuntimeEvidenceBridge.Build(new DelegationBridge(1), new Snapshot(5, "t1"), frame, "c2");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.MissionPackageFacts.Any(f => f.Contains("fire-control", StringComparison.OrdinalIgnoreCase)), Is.False);
    }

    private sealed class Snapshot(double time, string primary) : ISimWorldSnapshot
    {
        public double SimTime => time;
        public int ContactCount => 1;
        public int ActiveEngagementCount => 0;
        public bool IsMemberAlive(TargetId memberId) => true;
        public TargetId? PrimaryHostileContactId => new(primary);
        public bool HasFireControlTrackOnPrimaryContact => true;
        public bool ObserverRadarEmconActive => true;
    }
}
