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

    private static string RuleBody(string uss, string selector)
    {
        var m = Regex.Match(uss, Regex.Escape(selector) + @"\s*\{([^}]*)\}");
        Assert.That(m.Success, Is.True, $"USS rule {selector} missing");
        return m.Groups[1].Value;
    }

    private static double? OptPx(string body, string prop)
    {
        var m = Regex.Match(body, @"(?<![\w-])" + prop + @":\s*(-?\d+(?:\.\d+)?)px");
        return m.Success ? double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture) : null;
    }

    /// <summary>
    /// UAT D39: Unity's default field label is ~150px wide; inside the 226px inner width of the Doctrine panel
    /// it pushed the ROE dropdown input past the right edge (clipped by overflow: hidden).
    /// </summary>
    [Test]
    public void Override_dropdown_stacks_label_above_full_width_input()
    {
        const double MinInnerWidth = 150; // longest ROE value + dropdown arrow
        var uss = UiIaSourceReader.ReadUnder("unity", "ProjectAegis", "Assets", "UI", "DoctrineInheritance", "DoctrineInheritancePanel.uss");

        var panel = RuleBody(uss, ".doctrine-panel");
        var innerWidth = Px(panel, "width") - 2 * Px(panel, "padding") - 2 * Px(panel, "border-width");

        var labelMatch = Regex.Match(uss, @"\.doctrine-dropdown\s*(?:>\s*)?\.unity-base-field__label\s*\{([^}]*)\}");
        Assert.That(labelMatch.Success, Is.True, ".doctrine-dropdown .unity-base-field__label rule missing (Unity default label is 150px)");
        var label = labelMatch.Groups[1].Value;
        Assert.That(OptPx(label, "min-width"), Is.EqualTo(0), "label min-width must be 0px (no 150px default, no fixed side label)");
        Assert.That(label, Does.Not.Match(@"(?<![\w-])width:\s*\d"), "label must not have a fixed px width (use auto)");
        var color = Regex.Match(label, @"(?<![\w-])color:\s*rgb\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s*\)");
        Assert.That(color.Success, Is.True, "label colour must be set (default is near-invisible on the dark panel)");
        for (var i = 1; i <= 3; i++)
            Assert.That(int.Parse(color.Groups[i].Value), Is.GreaterThanOrEqualTo(150), "label colour must be light");

        var dd = RuleBody(uss, ".doctrine-dropdown");
        var ddMin = OptPx(dd, "min-width") ?? 0;
        Assert.That(ddMin, Is.LessThanOrEqualTo(innerWidth), "dropdown min-width must fit the panel inner width");
        Assert.That(dd, Does.Match(@"(?<![\w-])flex-direction:\s*column"), "label must stack above the input");
        Assert.That(OptPx(dd, "margin-left") ?? 0, Is.EqualTo(0), "default left margin pushes the dropdown past the row edge");
        Assert.That(OptPx(dd, "margin-right") ?? 0, Is.EqualTo(0), "margin-right must not push the input past the edge");

        var row = RuleBody(uss, ".doctrine-override-row");
        Assert.That(row, Does.Match(@"(?<![\w-])flex-direction:\s*column"), "row must stack dropdown above Apply");
        Assert.That(row, Does.Not.Contain("flex-wrap"), "row must not rely on flex-wrap");
        Assert.That(row, Does.Match(@"(?<![\w-])align-items:\s*stretch"));

        var inputMatch = Regex.Match(uss, @"\.doctrine-dropdown\s*>\s*\.unity-base-field__input\s*\{([^}]*)\}");
        Assert.That(inputMatch.Success, Is.True, ".doctrine-dropdown > .unity-base-field__input rule missing");
        Assert.That(inputMatch.Groups[1].Value, Does.Match(@"(?<![\w-])flex-grow:\s*1\b"));

        Assert.That(innerWidth, Is.GreaterThanOrEqualTo(MinInnerWidth), "inner width must fit the longest ROE value plus arrow");
    }
}
