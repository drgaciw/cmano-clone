using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

public sealed class OobTreePanelBinderTests
{
    [Test]
    public void Bind_marks_destroyed_units_in_display_line()
    {
        var state = OobTreePanelBinder.Bind(
        [
            new OobTreeEntry("u1", true),
            new OobTreeEntry("u2", false),
        ]);

        Assert.That(state.UnitRows[1].DisplayLine, Does.Contain("DESTROYED"));
        Assert.That(state.UnitRows[1].IsAlive, Is.False);
    }

    [Test]
    public void Bind_selected_unit_marks_row_selected()
    {
        var state = OobTreePanelBinder.Bind(
            [new OobTreeEntry("u1", true), new OobTreeEntry("u2", true)],
            selectedUnitId: "u2");

        Assert.That(state.UnitRows[0].IsSelected, Is.False);
        Assert.That(state.UnitRows[1].IsSelected, Is.True);
        Assert.That(state.UnitRows[1].StyleClass, Is.EqualTo("oob-row--selected"));
    }
}