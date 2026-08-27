using System;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Bridge;

/// <summary>
/// DRG-179: headless C2 publish seam. Does not bind Unity hosts (DRG-180).
/// </summary>
[TestFixture]
public sealed class KillChainContactStateBridgeTests
{
    [Test]
    public void Build_publishes_Find_from_order_log_without_ui_state()
    {
        var log = new DecisionLog();
        log.AppendContactChange(new ContactChangeRecord(
            0, 1.0, 1, "u1", "c1", "hostile-1", "Unknown", "Detected"));
        var world = new SimWorldSnapshotStub(simTime: 1, contactCount: 1, primaryHostileContactId: new TargetId("hostile-1"));

        var snapshot = KillChainContactStateBridge.Build(world, log);

        Assert.That(snapshot.Contacts, Has.Count.EqualTo(1));
        Assert.That(snapshot.Contacts[0].Phase, Is.EqualTo(KillChainPhase.Find));
        Assert.That(snapshot.Contacts[0].Targetable, Is.False);
        Assert.That(snapshot.Transitions.Select(t => t.Kind), Is.EqualTo(new[] { KillChainTransitionKind.Find }));
    }

    [Test]
    public void Build_uses_snapshot_fire_control_on_primary_target_not_selection()
    {
        var log = new DecisionLog();
        log.AppendContactChange(new ContactChangeRecord(
            0, 1.0, 1, "u1", "c1", "hostile-1", "Unknown", "Identified"));
        var world = new SimWorldSnapshotStub(
            simTime: 2,
            contactCount: 1,
            primaryHostileContactId: new TargetId("hostile-1"),
            hasFireControlTrackOnPrimaryContact: true);

        var snapshot = KillChainContactStateBridge.Build(world, log);

        Assert.That(snapshot.Contacts[0].Phase, Is.EqualTo(KillChainPhase.Target));
        Assert.That(snapshot.Contacts[0].Targetable, Is.True);
        Assert.That(snapshot.Contacts[0].LocationSufficient, Is.True);
        Assert.That(snapshot.Contacts[0].TrackContinuous, Is.True);
    }

    [Test]
    public void BindPanel_matches_projection_binder()
    {
        var log = new DecisionLog();
        log.AppendContactChange(new ContactChangeRecord(
            0, 1.0, 1, "u1", "c1", "hostile-1", "Unknown", "Classified"));
        var world = new SimWorldSnapshotStub(simTime: 1, contactCount: 1);
        var snapshot = KillChainContactStateBridge.Build(world, log);

        var expected = KillChainContactPanelBinder.Bind(snapshot);
        var viaBridge = KillChainContactStateBridge.BindPanel(snapshot);

        Assert.That(viaBridge.ContactCountLabel, Is.EqualTo(expected.ContactCountLabel));
        Assert.That(viaBridge.TransitionCountLabel, Is.EqualTo(expected.TransitionCountLabel));
        Assert.That(viaBridge.Rows[0].PhaseLabel, Is.EqualTo(expected.Rows[0].PhaseLabel));
        Assert.That(viaBridge.TransitionLines, Is.EqualTo(expected.TransitionLines));
    }

    [Test]
    public void Build_null_snapshot_throws()
    {
        Assert.Throws<ArgumentNullException>((Action)(() =>
            KillChainContactStateBridge.Build(null!, new DecisionLog())));
    }

    [Test]
    public void Build_null_log_throws()
    {
        Assert.Throws<ArgumentNullException>((Action)(() =>
            KillChainContactStateBridge.Build(new SimWorldSnapshotStub(), null!)));
    }

    [Test]
    public void BindPanel_null_snapshot_throws()
    {
        Assert.Throws<ArgumentNullException>((Action)(() =>
            KillChainContactStateBridge.BindPanel(null!)));
    }
}
