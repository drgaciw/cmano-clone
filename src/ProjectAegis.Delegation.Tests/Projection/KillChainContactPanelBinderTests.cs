using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

[TestFixture]
public sealed class KillChainContactPanelBinderTests
{
    [Test]
    public void Bind_empty_snapshot_has_zero_rows()
    {
        var panel = KillChainContactPanelBinder.Bind(KillChainContactSnapshot.Empty);

        Assert.That(panel.ContactCountLabel, Is.EqualTo("KC: 0"));
        Assert.That(panel.TransitionCountLabel, Is.EqualTo("KC-TX: 0"));
        Assert.That(panel.Rows, Is.Empty);
        Assert.That(panel.TransitionLines, Is.Empty);
    }

    [Test]
    public void Bind_maps_phase_flags_time_correlation_and_source_refs()
    {
        var log = new DecisionLog();
        log.AppendContactChange(new ContactChangeRecord(
            0, 1.0, 1, "u1", "c1", "hostile-1", "Unknown", "Detected"));

        var snapshot = KillChainContactStateProjection.Project(log, currentSimTick: 1);
        var panel = KillChainContactPanelBinder.Bind(snapshot);

        Assert.That(panel.ContactCountLabel, Is.EqualTo("KC: 1"));
        Assert.That(panel.Rows, Has.Count.EqualTo(1));
        var row = panel.Rows[0];
        Assert.That(row.ContactId, Is.EqualTo("c1"));
        Assert.That(row.PhaseLabel, Is.EqualTo("KC: FIND"));
        Assert.That(row.PhaseClass, Is.EqualTo("kill-chain-phase--find"));
        Assert.That(row.DetectionLabel, Is.EqualTo("DET"));
        Assert.That(row.LocationLabel, Is.EqualTo("LOC: —"));
        Assert.That(row.TrackLabel, Is.EqualTo("TRK: —"));
        Assert.That(row.TargetabilityLabel, Is.EqualTo("TGT: —"));
        Assert.That(row.LossLabel, Is.EqualTo("LOSS: —"));
        Assert.That(row.TimeLabel, Does.Contain("1"));
        Assert.That(row.CorrelationLabel, Does.StartWith("SEQ:"));
        Assert.That(row.SourceLabel, Does.Contain("observer:u1"));
        Assert.That(panel.TransitionLines, Has.Count.EqualTo(1));
        Assert.That(panel.TransitionLines[0], Does.Contain("FIND"));
        Assert.That(panel.TransitionLines[0], Does.Contain("c1"));
    }

    [Test]
    public void Bind_does_not_invent_targetability_from_display()
    {
        var log = new DecisionLog();
        log.AppendContactChange(new ContactChangeRecord(
            0, 5.0, 5, "u1", "c1", "hostile-1", "Unknown", "Identified"));

        var snapshot = KillChainContactStateProjection.Project(log, currentSimTick: 6);
        var panel = KillChainContactPanelBinder.Bind(snapshot);

        Assert.That(snapshot.Contacts[0].Targetable, Is.False);
        Assert.That(panel.Rows[0].PhaseLabel, Is.EqualTo("KC: TRACK"));
        Assert.That(panel.Rows[0].TargetabilityLabel, Is.EqualTo("TGT: —"));
        Assert.That(panel.Rows[0].LocationLabel, Is.EqualTo("LOC"));
        Assert.That(panel.Rows[0].TrackLabel, Is.EqualTo("TRK"));
    }
}
