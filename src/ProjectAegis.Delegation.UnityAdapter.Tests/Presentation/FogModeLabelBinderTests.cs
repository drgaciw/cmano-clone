using NUnit.Framework;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using ProjectAegis.Sim.Scenario;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

[TestFixture]
public sealed class FogModeLabelBinderTests
{
    [Test]
    public void Full_transparency_maps_to_full_picture_label()
    {
        var presentation = FogModeLabelBinder.Bind(PlayerInfoModel.FullTransparency);
        Assert.That(presentation.Label, Is.EqualTo("FOG: FULL PICTURE"));
        Assert.That(presentation.CssClass, Is.EqualTo("c2-topbar-item--fog-full"));
    }

    [Test]
    public void Delegation_fog_maps_to_delegation_label()
    {
        var presentation = FogModeLabelBinder.Bind(PlayerInfoModel.DelegationFog);
        Assert.That(presentation.Label, Is.EqualTo("FOG: DELEGATION"));
        Assert.That(presentation.CssClass, Is.EqualTo("c2-topbar-item--fog-delegation"));
    }

    [Test]
    public void Tiered_by_autonomy_maps_to_tiered_label()
    {
        var presentation = FogModeLabelBinder.Bind(PlayerInfoModel.TieredByAutonomy);
        Assert.That(presentation.Label, Is.EqualTo("FOG: TIERED"));
        Assert.That(presentation.CssClass, Is.EqualTo("c2-topbar-item--fog-tiered"));
    }
}
