using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

public sealed class MapPanelBinderTests
{
    [Test]
    public void Bind_selected_unit_adds_selected_style_class()
    {
        var state = MapPanelBinder.Bind(
            [new MapSymbolEntry("u1", "Friendly", "■", "u1", 0.2f, 0.3f, false)],
            "baltic-patrol",
            selectedUnitId: "u1",
            selectedContactId: null);

        Assert.That(state.Symbols[0].IsSelected, Is.True);
        Assert.That(state.Symbols[0].StyleClass, Does.Contain("map-symbol--selected"));
    }
}