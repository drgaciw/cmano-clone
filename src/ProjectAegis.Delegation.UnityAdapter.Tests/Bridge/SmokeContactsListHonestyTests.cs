using System.Linq;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Bridge;

/// <summary>
/// S122-13: left CONTACTS list includes c1 whenever Contact Detail would show CONTACT: c1.
/// Seeded DecisionLog must project into SensorC2Bridge without a Tick (paused Play).
/// </summary>
[TestFixture]
public sealed class SmokeContactsListHonestyTests
{
    [Test]
    public void Seeded_orbat_sensor_c2_lists_c1_without_tick()
    {
        var bridge = new DelegationBridge(42);
        Assert.That(PlayModeSmokeOrbatSeeder.TrySeed(bridge), Is.True);

        var snapshotStub = new SimWorldSnapshotStub(
            simTime: 0,
            contactCount: 1,
            primaryHostileContactId: new TargetId(PlayModeSmokeOrbatSeeder.HostileUnitId),
            hasFireControlTrackOnPrimaryContact: true);

        var snapshot = SensorC2Bridge.Build(snapshotStub, bridge.Orchestrator.DecisionLog);
        Assert.That(
            snapshot.Contacts.Any(c => c.ContactId == PlayModeSmokeOrbatSeeder.ContactId),
            Is.True,
            "SensorC2Bridge.Build after TrySeed must include seeded contact c1 without Tick.");

        var panel = SensorC2PanelBinder.Bind(snapshot);
        Assert.That(
            panel.ContactRows.Any(r => r.ContactId == PlayModeSmokeOrbatSeeder.ContactId),
            Is.True,
            "C2LeftDrawer ContactRows must contain c1 so the list matches Contact Detail.");

        var c1Row = panel.ContactRows.Single(r => r.ContactId == PlayModeSmokeOrbatSeeder.ContactId);
        Assert.That(c1Row.DisplayLine, Does.Contain("c1"));

        var detail = ContactDetailApplyState.ProjectAndApply(
            PlayModeSmokeOrbatSeeder.ContactId,
            snapshot.Contacts,
            currentSimTick: 0);
        Assert.That(detail.ContactIdLine, Is.EqualTo("CONTACT: c1"));
    }
}
