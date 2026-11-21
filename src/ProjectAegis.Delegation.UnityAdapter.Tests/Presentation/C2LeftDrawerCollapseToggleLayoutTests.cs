using System.Text.RegularExpressions;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

/// <summary>UAT D12: the collapse toggle must sit fully inside the drawer, not clipped at the top edge.</summary>
[TestFixture]
public sealed class C2LeftDrawerCollapseToggleLayoutTests
{
    [Test]
    public void Collapse_toggle_sits_fully_inside_drawer()
    {
        var uss = UiIaSourceReader.ReadUnder(
            "unity", "ProjectAegis", "Assets", "UI", "C2LeftDrawer", "C2LeftDrawerPanel.uss");
        var rule = Regex.Match(uss, @"\.c2-drawer-collapse-toggle\s*\{([^}]*)\}");
        Assert.That(rule.Success, Is.True, ".c2-drawer-collapse-toggle rule missing");
        var m = Regex.Match(rule.Groups[1].Value, @"margin-top:\s*(-?\d+)px");
        Assert.That(m.Success, Is.True, "collapse toggle needs explicit margin-top");
        Assert.That(int.Parse(m.Groups[1].Value), Is.GreaterThanOrEqualTo(8));
    }
}
