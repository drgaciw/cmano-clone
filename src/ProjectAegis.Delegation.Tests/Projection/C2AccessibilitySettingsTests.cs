using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

/// <summary>PE-UX-W6: scale + reduced-motion stubs for Platform / C2 hosts.</summary>
[TestFixture]
public sealed class C2AccessibilitySettingsTests
{
    [Test]
    public void Default_is_100_percent_without_reduced_motion()
    {
        var settings = C2AccessibilitySettings.Default;

        Assert.That(settings.ScalePercent, Is.EqualTo(C2AccessibilityScalePercent.OneHundred));
        Assert.That(settings.ReducedMotion, Is.False);
        Assert.That(settings.FontScaleMultiplier, Is.EqualTo(1.0f));
        Assert.That(settings.RootUssClasses, Is.EqualTo(string.Empty));
    }

    [Test]
    public void Reduced_motion_adds_root_uss_class()
    {
        var settings = new C2AccessibilitySettings(
            C2AccessibilityScalePercent.OneTwentyFive,
            ReducedMotion: true);

        Assert.That(settings.FontScaleMultiplier, Is.EqualTo(1.25f).Within(0.0001f));
        Assert.That(settings.RootUssClasses, Is.EqualTo("reduced-motion"));
        Assert.That(settings.ScaleUssClass, Is.EqualTo("aegis-scale-125"));
    }

    [Test]
    public void Scale_150_maps_uss_class()
    {
        var settings = new C2AccessibilitySettings(C2AccessibilityScalePercent.OneFifty);

        Assert.That(settings.ScaleUssClass, Is.EqualTo("aegis-scale-150"));
        Assert.That(settings.FontScaleMultiplier, Is.EqualTo(1.5f).Within(0.0001f));
    }
}
