using System.Text.RegularExpressions;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

/// <summary>
/// UAT D11: every unit-detail text line (notably the long ENGAGE preview) must wrap inside
/// the right column instead of running past the panel edge and being clipped.
/// </summary>
[TestFixture]
public sealed class UnitDetailPanelWrapLayoutTests
{
    private static string RuleBody(string uss, string selector)
    {
        var m = Regex.Match(uss, Regex.Escape(selector) + @"\s*\{([^}]*)\}");
        Assert.That(m.Success, Is.True, $"USS rule {selector} missing");
        return m.Groups[1].Value;
    }

    [Test]
    public void Unit_detail_line_class_wraps_text_so_engage_line_is_not_clipped()
    {
        var uss = UiIaSourceReader.ReadUnder(
            "unity", "ProjectAegis", "Assets", "UI", "UnitDetail", "UnitDetailPanel.uss");
        var xml = UiIaSourceReader.ReadUnder(
            "unity", "ProjectAegis", "Assets", "UI", "UnitDetail", "UnitDetailPanel.uxml");

        Assert.That(xml, Does.Match("name=\"engage-line\"[^>]*class=\"unit-detail-line"));
        Assert.That(RuleBody(uss, ".unit-detail-line"), Does.Contain("white-space: normal"));
    }
}
