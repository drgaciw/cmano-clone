using NUnit.Framework;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Presentation;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

[TestFixture]
public sealed class AirOpsReadyAggregateBinderTests
{
    [Test]
    public void Binder_maps_ready_and_not_ready_counts_from_aggregate()
    {
        var entries = AirOpsProjection.Project([
            ("a1", true, "Fighter", "CVN-1"),
            ("a2", false, "Strike", "CVN-1"),
            ("a3", true, "Helo", "LHA-1"),
        ]);
        var presentation = AirOpsApplyState.Apply(entries);

        var labels = AirOpsReadyAggregateBinder.Bind(presentation);

        Assert.That(labels.ReadyCount, Is.EqualTo(2));
        Assert.That(labels.NotReadyCount, Is.EqualTo(1));
        Assert.That(labels.TotalCount, Is.EqualTo(3));
        Assert.That(labels.ReadySummaryLine, Is.EqualTo("READY 2/3"));
        Assert.That(labels.ReadyLineText, Is.EqualTo("READY 2/3 · NOT READY 1"));
        Assert.That(labels.HeaderLine, Is.EqualTo("AIR OPS  ·  READY 2/3"));
        Assert.That(labels.HasReadinessData, Is.True);
    }

    [Test]
    public void All_ready_uses_all_ready_cue_and_declutter()
    {
        var entries = AirOpsProjection.Project([
            ("a1", true, "Fighter", "CVN-1"),
            ("a2", true, "Strike", "CVN-1"),
        ]);
        var labels = AirOpsReadyAggregateBinder.Bind(AirOpsApplyState.Apply(entries));

        Assert.That(labels.CueClass, Is.EqualTo(AirOpsReadyCueClasses.AllReady));
        Assert.That(labels.DeclutterToken, Is.EqualTo(AirOpsReadyDeclutterTokens.AllReady));
        Assert.That(labels.NotReadyCount, Is.Zero);
    }

    [Test]
    public void None_ready_uses_none_ready_cue()
    {
        var entries = AirOpsProjection.Project([
            ("a1", false, "Fighter", "CVN-1"),
        ]);
        var labels = AirOpsReadyAggregateBinder.Bind(AirOpsApplyState.Apply(entries));

        Assert.That(labels.CueClass, Is.EqualTo(AirOpsReadyCueClasses.NoneReady));
        Assert.That(labels.DeclutterToken, Is.EqualTo(AirOpsReadyDeclutterTokens.NoneReady));
        Assert.That(labels.ReadyLineText, Is.EqualTo("READY 0/1 · NOT READY 1"));
    }

    [Test]
    public void Partial_ready_uses_partial_cue()
    {
        var entries = AirOpsProjection.Project([
            ("a1", true, "Fighter", "CVN-1"),
            ("a2", false, "Strike", "CVN-1"),
        ]);
        var labels = AirOpsReadyAggregateBinder.Bind(AirOpsApplyState.Apply(entries));

        Assert.That(labels.CueClass, Is.EqualTo(AirOpsReadyCueClasses.Partial));
        Assert.That(labels.DeclutterToken, Is.EqualTo(AirOpsReadyDeclutterTokens.Partial));
    }

    [Test]
    public void No_readiness_data_uses_unknown_cue_and_honest_line()
    {
        var labels = AirOpsReadyAggregateBinder.Bind(AirOpsPresentation.NoReadinessData);

        Assert.That(labels.CueClass, Is.EqualTo(AirOpsReadyCueClasses.Unknown));
        Assert.That(labels.DeclutterToken, Is.EqualTo(AirOpsReadyDeclutterTokens.NoData));
        Assert.That(labels.ReadyLineText, Is.EqualTo(AirOpsApplyState.NoReadinessDataLine));
        Assert.That(labels.Fingerprint, Is.EqualTo("ao:no-data"));
    }

    [Test]
    public void Bind_rows_exposes_ready_line_element_name()
    {
        var entries = AirOpsProjection.Project([("a1", true, "Fighter", "CVN-1")]);
        var labels = AirOpsReadyAggregateBinder.Bind(AirOpsApplyState.Apply(entries));
        var rows = AirOpsReadyAggregateBinder.BindRows(labels);

        Assert.That(rows, Has.Count.EqualTo(1));
        Assert.That(rows[0].ElementName, Is.EqualTo("air-ops-ready-line"));
        Assert.That(rows[0].Text, Does.Contain("READY 1/1"));
        Assert.That(rows[0].CueClass, Is.EqualTo(AirOpsReadyCueClasses.AllReady));
    }

    [Test]
    public void Fingerprint_changes_when_ready_flags_change()
    {
        var ready = AirOpsApplyState.Apply(AirOpsProjection.Project([("a1", true, "F", "H")]));
        var notReady = AirOpsApplyState.Apply(AirOpsProjection.Project([("a1", false, "F", "H")]));

        Assert.That(
            AirOpsReadyAggregatePresenter.ComputeFingerprint(ready),
            Is.Not.EqualTo(AirOpsReadyAggregatePresenter.ComputeFingerprint(notReady)));
    }
}
