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
    }

    [Test]
    public void Apply_null_returns_empty()
    {
        var applied = MapPanelApplyState.Apply(null);
        Assert.That(applied.SymbolCount, Is.EqualTo(0));
        Assert.That(applied.TheaterLabel, Is.Empty);
    }
}
