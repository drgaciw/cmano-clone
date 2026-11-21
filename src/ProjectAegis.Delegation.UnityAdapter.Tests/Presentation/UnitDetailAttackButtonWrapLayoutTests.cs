using System.Text.RegularExpressions;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

/// <summary>UAT D29: attack buttons carry long disabled-reason text and must wrap, not clip.</summary>
[TestFixture]
public sealed class UnitDetailAttackButtonWrapLayoutTests
{
    [Test]
    public void Attack_button_class_wraps_text_and_grows_in_height()
    {
        var uss = UiIaSourceReader.ReadUnder("unity", "ProjectAegis", "Assets", "UI", "UnitDetail", "UnitDetailPanel.uss");
        var xml = UiIaSourceReader.ReadUnder("unity", "ProjectAegis", "Assets", "UI", "UnitDetail", "UnitDetailPanel.uxml");
        foreach (var name in new[] { "attack-fire-single", "attack-fire-salvo", "attack-hold-fire" })
        {
            Assert.That(xml, Does.Match("name=\"" + name + "\"[^>]*class=\"unit-detail-attack-btn"));
        }

        var m = Regex.Match(uss, @"\.unit-detail-attack-btn\s*\{([^}]*)\}");
        Assert.That(m.Success, Is.True, ".unit-detail-attack-btn rule missing");
        var body = m.Groups[1].Value;
        Assert.That(body, Does.Contain("white-space: normal"));
        Assert.That(body, Does.Contain("height: auto"));
        Assert.That(body, Does.Contain("max-width: 100%"));
    }
}
