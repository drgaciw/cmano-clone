using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

public sealed class LeftDrawerApplyStateTests
{
    [Test]
    public void BindAndApply_selects_unit_and_preserves_rows()
    {
        var entries = new[]
        {
            new OobTreeEntry("u1", true),
            new OobTreeEntry("u2", false),
        };

        var applied = LeftDrawerApplyState.BindAndApply(entries, selectedUnitId: "u2");

        Assert.That(applied.RowCount, Is.EqualTo(2));
        Assert.That(applied.SelectedUnitId, Is.EqualTo("u2"));
        Assert.That(applied.Rows[1].IsSelected, Is.True);
        Assert.That(applied.Rows[1].StyleClass, Is.EqualTo("oob-row--selected"));
        Assert.That(applied.Rows[0].DisplayLine, Does.Contain("u1"));
        Assert.That(applied.Rows[1].IsAlive, Is.False);
    }

    [Test]
    public void BindAndApply_graph_highlight_marks_style()
    {
        var entries = new[] { new OobTreeEntry("alpha", true), new OobTreeEntry("beta", true) };
        var applied = LeftDrawerApplyState.BindAndApply(entries, null, new[] { "beta" });

        Assert.That(applied.Rows[1].StyleClass, Is.EqualTo("oob-row--graph"));
        Assert.That(applied.Rows[1].DisplayLine, Does.Contain("⚡"));
    }

    [Test]
    public void Apply_empty_returns_empty_presentation()
    {
        var applied = LeftDrawerApplyState.Apply(new OobTreePanelState(Array.Empty<OobTreeDisplayRow>()));
        Assert.That(applied.RowCount, Is.EqualTo(0));
        Assert.That(applied.SelectedUnitId, Is.Null);
    }
}
