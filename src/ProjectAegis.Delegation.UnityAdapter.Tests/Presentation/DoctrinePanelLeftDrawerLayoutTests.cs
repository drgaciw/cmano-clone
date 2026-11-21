using System.Globalization;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

/// <summary>UAT D37: Doctrine panel must not cover Unit Detail attack buttons; it docks bottom-left inside the left drawer.</summary>
[TestFixture]
public sealed class DoctrinePanelLeftDrawerLayoutTests
{
    private static double Px(string body, string prop)
    {
        var m = Regex.Match(body, @"(?<![\w-])" + prop + @":\s*(\d+(?:\.\d+)?)px");
        Assert.That(m.Success, Is.True, $"{prop}: <n>px missing from .doctrine-panel");
        return double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
    }

    [Test]
    public void Doctrine_root_anchors_bottom_left_within_left_drawer_width()
    {
        var uss = UiIaSourceReader.ReadUnder("unity", "ProjectAegis", "Assets", "UI", "DoctrineInheritance", "DoctrineInheritancePanel.uss");
        var tokens = UiIaSourceReader.ReadUnder("unity", "ProjectAegis", "Assets", "UI", "AegisTokens.uss");
        var m = Regex.Match(uss, @"\.doctrine-panel\s*\{([^}]*)\}");
        Assert.That(m.Success, Is.True, ".doctrine-panel rule missing");
        var body = m.Groups[1].Value;

        var tok = Regex.Match(tokens, @"--c2-left-drawer-width:\s*(\d+)px");
        Assert.That(tok.Success, Is.True, "--c2-left-drawer-width token missing");
        var drawerWidth = double.Parse(tok.Groups[1].Value, CultureInfo.InvariantCulture);

        Assert.That(body, Does.Match(@"(?<![\w-])left:\s*\d+px"), "must anchor with left:");
        Assert.That(body, Does.Match(@"(?<![\w-])bottom:\s*\d+px"), "must anchor with bottom:");
        Assert.That(body, Does.Not.Match(@"(?<![\w-])right:"), "right: places it in the Unit Detail band");
        Assert.That(body, Does.Not.Match(@"(?<![\w-])top:"));

        var left = Px(body, "left");
        var width = Px(body, "width");
        Assert.That(left + width, Is.LessThanOrEqualTo(drawerWidth), "panel must end inside the drawer");
        if (Regex.IsMatch(body, @"min-width:\s*\d+px"))
            Assert.That(Px(body, "min-width"), Is.LessThanOrEqualTo(width));
        Assert.That(Px(body, "max-height"), Is.LessThanOrEqualTo(480), "top must stay below the drawer content (~y 520 of 1004)");
        Assert.That(uss, Does.Contain("white-space: normal"));
    }
}
