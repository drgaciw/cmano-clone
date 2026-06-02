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
}