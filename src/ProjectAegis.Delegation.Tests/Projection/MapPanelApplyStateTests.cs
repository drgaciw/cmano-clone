using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

public sealed class MapPanelApplyStateTests
{
    [Test]
    public void BindAndApply_counts_symbols_and_selection()
    {
        var symbols = new[]
        {
            new MapSymbolEntry("f1", "Friendly", "F", "Blue", 0.2f, 0.3f, false),
            new MapSymbolEntry("h1", "Hostile", "H", "Red", 0.7f, 0.6f, false),
        };

        var applied = MapPanelApplyState.BindAndApply(symbols, "BALTIC", selectedUnitId: "f1");

        Assert.That(applied.TheaterLabel, Is.EqualTo("BALTIC"));
        Assert.That(applied.SymbolCount, Is.EqualTo(2));
        Assert.That(applied.SelectedCount, Is.EqualTo(1));
        Assert.That(applied.SelectedSymbolId, Is.EqualTo("f1"));
        Assert.That(applied.EnvelopeRingCount, Is.EqualTo(0));
        Assert.That(applied.DatalinkEdgeCount, Is.EqualTo(0));
    }

    [Test]
    public void Apply_null_returns_empty()
    {
        var applied = MapPanelApplyState.Apply(null);
        Assert.That(applied.SymbolCount, Is.EqualTo(0));
        Assert.That(applied.TheaterLabel, Is.Empty);
        Assert.That(applied.EnvelopeRingCount, Is.EqualTo(0));
        Assert.That(applied.DatalinkEdgeCount, Is.EqualTo(0));
    }

    [Test]
    public void Apply_with_overlays_sets_envelope_and_datalink_counts()
    {
        var symbols = new[]
        {
            new MapSymbolEntry("f1", "Friendly", "F", "Blue", 0.2f, 0.3f, false),
        };
        var rings = TacticalOverlayProjection.ProjectSelectedUnitEnvelopes("f1", 40, 20, "Surface");
        var edges = DatalinkPictureProjection.Project(
        [
            ("f1", "f2", "tactical", DatalinkPictureProjection.StatusUp),
            ("f2", "f3", "satcom", DatalinkPictureProjection.StatusDegraded),
        ]);

        var applied = MapPanelApplyState.BindAndApply(
            symbols,
            "BALTIC",
            selectedUnitId: "f1",
            selectedContactId: null,
            envelopeRings: rings,
            datalinkEdges: edges);

        Assert.That(applied.SymbolCount, Is.EqualTo(1));
        Assert.That(applied.SelectedCount, Is.EqualTo(1));
        Assert.That(applied.EnvelopeRingCount, Is.EqualTo(2));
        Assert.That(applied.DatalinkEdgeCount, Is.EqualTo(2));
    }

    [Test]
    public void Apply_with_null_overlay_lists_keeps_zero_counts()
    {
        var state = MapPanelBinder.Bind(
            [new MapSymbolEntry("f1", "Friendly", "F", "Blue", 0.1f, 0.1f, false)],
            "X");
        var applied = MapPanelApplyState.Apply(state, null, null);
        Assert.That(applied.EnvelopeRingCount, Is.EqualTo(0));
        Assert.That(applied.DatalinkEdgeCount, Is.EqualTo(0));
    }
}
