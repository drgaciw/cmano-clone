using NUnit.Framework;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Bridge;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Bridge;

[TestFixture]
public sealed class ContactOverrideCommandTests
{
    private static (DelegationBridge Bridge, SimWorldSnapshotStub Snapshot) CreateBridgeWithContact()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: false);
        bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
        bridge.Orchestrator.DecisionLog.AppendContactChange(new ContactChangeRecord(
            SequenceId: 0,
            SimTime: 1.0,
            SimTick: 1,
            ObserverId: "u1",
            ContactId: "c1",
            TargetId: "hostile-1",
            PreviousState: "Detected",
            NewState: "Classified"));
        var snapshot = new SimWorldSnapshotStub(simTime: 2.0, contactCount: 1);
        return (bridge, snapshot);
    }

    [Test]
    public void TryReclassify_records_ContactOverride_without_mutating_contact_changes_or_catalog()
    {
        var (bridge, _) = CreateBridgeWithContact();
        var contactChangesBefore = bridge.Orchestrator.DecisionLog.ContactChanges.Count;

        var result = ContactOverrideCommand.TryReclassify(bridge, "c1", "neutral", simTime: 2.0);

        Assert.That(result.Accepted, Is.True, result.Message);
        Assert.That(bridge.Orchestrator.DecisionLog.ContactOverrides, Has.Count.EqualTo(1));
        var ov = bridge.Orchestrator.DecisionLog.ContactOverrides[0];
        Assert.That(ov.Action, Is.EqualTo(ContactOverrideAction.Reclassify));
        Assert.That(ov.PreviousValue, Is.EqualTo("Hostile"));
        Assert.That(ov.NewValue, Is.EqualTo("Neutral"));
        Assert.That(bridge.Orchestrator.DecisionLog.ContactChanges, Has.Count.EqualTo(contactChangesBefore));

        var entry = ContactOverrideProjection.Project(bridge.Orchestrator.DecisionLog).Find("c1");
        Assert.That(entry, Is.Not.Null);
        Assert.That(entry!.AffiliationOverride, Is.EqualTo("Neutral"));
        Assert.That(entry.StatusLabel, Is.EqualTo("OVERRIDE: RECLASSIFIED → Neutral"));
    }

    [Test]
    public void TryDrop_records_ContactOverride_and_removes_from_map_and_sensor_hud_only()
    {
        var (bridge, snapshot) = CreateBridgeWithContact();

        var result = ContactOverrideCommand.TryDrop(bridge, "c1", simTime: 3.0);

        Assert.That(result.Accepted, Is.True, result.Message);
        Assert.That(bridge.Orchestrator.DecisionLog.ContactOverrides, Has.Count.EqualTo(1));
        Assert.That(bridge.Orchestrator.DecisionLog.ContactOverrides[0].Action, Is.EqualTo(ContactOverrideAction.Drop));

        // Sensor picture truth stays intact; presentation bridges filter dropped contacts.
        var picture = ContactPictureProjection.Project(bridge.Orchestrator.DecisionLog);
        Assert.That(picture.Any(c => c.ContactId == "c1"), Is.True);

        var symbols = MapPictureBridge.Build(snapshot, bridge.Registry, bridge.Orchestrator.DecisionLog, layoutSeed: 7);
        Assert.That(symbols.Any(s => s.SymbolId == "c1"), Is.False);

        var sensorC2 = SensorC2Bridge.Build(snapshot, bridge.Orchestrator.DecisionLog);
        Assert.That(sensorC2.Contacts.Any(c => c.ContactId == "c1"), Is.False);
    }

    [Test]
    public void TryReclassify_and_TryDrop_are_idempotent_and_reject_invalid_inputs()
    {
        var (bridge, _) = CreateBridgeWithContact();

        Assert.That(ContactOverrideCommand.TryReclassify(bridge, "", "Neutral", 1.0).ReasonCode,
            Is.EqualTo(ContactOverrideCommand.ReasonMissingContactId));
        Assert.That(ContactOverrideCommand.TryReclassify(bridge, "missing", "Neutral", 1.0).ReasonCode,
            Is.EqualTo(ContactOverrideCommand.ReasonUnknownContact));
        Assert.That(ContactOverrideCommand.TryReclassify(bridge, "c1", "Martian", 1.0).ReasonCode,
            Is.EqualTo(ContactOverrideCommand.ReasonInvalidAffiliation));
        Assert.That(bridge.Orchestrator.DecisionLog.ContactOverrides, Is.Empty);

        Assert.That(ContactOverrideCommand.TryReclassify(bridge, "c1", "Neutral", 2.0).Accepted, Is.True);
        var dupReclassify = ContactOverrideCommand.TryReclassify(bridge, "c1", "Neutral", 3.0);
        Assert.That(dupReclassify.Accepted, Is.False);
        Assert.That(dupReclassify.ReasonCode, Is.EqualTo(ContactOverrideCommand.ReasonNoChange));

        Assert.That(ContactOverrideCommand.TryDrop(bridge, "c1", 4.0).Accepted, Is.True);
        var dupDrop = ContactOverrideCommand.TryDrop(bridge, "c1", 5.0);
        Assert.That(dupDrop.Accepted, Is.False);
        Assert.That(dupDrop.ReasonCode, Is.EqualTo(ContactOverrideCommand.ReasonNoChange));
        Assert.That(bridge.Orchestrator.DecisionLog.ContactOverrides, Has.Count.EqualTo(2));
    }

    [Test]
    public void AttachReplayViewer_refuses_reclassify_and_drop_with_zero_log_writes()
    {
        var (bridge, snapshot) = CreateBridgeWithContact();
        bridge.AttachReplayViewer = true;
        var entriesBefore = bridge.Orchestrator.DecisionLog.ChronologicalEntries().Count;

        var r1 = ContactOverrideCommand.TryReclassify(bridge, "c1", "Friendly", 2.0);
        var r2 = ContactOverrideCommand.TryDrop(bridge, "c1", 2.0);

        Assert.That(r1.Accepted, Is.False);
        Assert.That(r1.ReasonCode, Is.EqualTo(ContactOverrideCommand.ReasonReplayAttached));
        Assert.That(r2.Accepted, Is.False);
        Assert.That(r2.ReasonCode, Is.EqualTo(ContactOverrideCommand.ReasonReplayAttached));
        Assert.That(bridge.Orchestrator.DecisionLog.ChronologicalEntries(), Has.Count.EqualTo(entriesBefore));

        var frame = SliceAContactFrameBridge.Build(snapshot, bridge);
        Assert.That(frame.IsReplayReadOnly, Is.True);
    }

    [Test]
    public void SliceAContactFrameBridge_populates_ContactOverrides()
    {
        var (bridge, snapshot) = CreateBridgeWithContact();
        Assume.That(ContactOverrideCommand.TryReclassify(bridge, "c1", "Unknown", 2.0).Accepted, Is.True);

        var frame = SliceAContactFrameBridge.Build(snapshot, bridge);

        Assert.That(frame.IsReplayReadOnly, Is.False);
        var ov = frame.ContactOverrides.Find("c1");
        Assert.That(ov, Is.Not.Null);
        Assert.That(ov!.AffiliationOverride, Is.EqualTo("Unknown"));
    }
}
