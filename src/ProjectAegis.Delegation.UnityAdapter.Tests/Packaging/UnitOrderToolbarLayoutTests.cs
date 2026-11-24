using System.Text.RegularExpressions;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Packaging;

/// <summary>
/// Source-text guard for UAT D15 (layout-safe subset): the unit-order toolbar must not be centred
/// with <c>left: 50%</c> (it overlapped the map control-button row); it anchors with <c>right:</c>
/// clear of the bottom-right Message Log.
/// </summary>
public sealed class UnitOrderToolbarLayoutTests
{
    private static string ToolbarBlock()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8 && !File.Exists(Path.Combine(dir, "ProjectAegis.sln")); i++)
        {
            dir = Directory.GetParent(dir)?.FullName ?? dir;
        }

        var path = Path.Combine(dir, "unity", "ProjectAegis", "Assets", "UI", "UnitOrderToolbar", "UnitOrderToolbarPanel.uss");
        Assert.That(File.Exists(path), Is.True, path);
        var m = Regex.Match(File.ReadAllText(path), @"\.unit-order-toolbar\s*\{([^}]*)\}", RegexOptions.CultureInvariant);
        Assert.That(m.Success, Is.True, ".unit-order-toolbar rule not found");
        return m.Groups[1].Value;
    }

    [Test]
    public void Toolbar_is_not_centred_with_left_50_percent()
    {
        Assert.That(ToolbarBlock(), Does.Not.Match(@"left:\s*50%"));
    }

    [Test]
    public void Toolbar_anchors_with_right_clear_of_message_log()
    {
        Assert.That(ToolbarBlock(), Does.Match(@"(^|[\s;])right:\s*(var\(--c2-message-log-clearance-x\)|\d+px)"));
    }

    [Test]
    public void Toolbar_max_width_keeps_left_edge_clear_of_map_control_row()
    {
        // 2056px reference panel - 440px right anchor - max-width must leave the left edge >= 1000px.
        var block = ToolbarBlock();
        var max = int.Parse(Regex.Match(block, @"max-width:\s*(\d+)px").Groups[1].Value);
        var min = int.Parse(Regex.Match(block, @"min-width:\s*(\d+)px").Groups[1].Value);
        Assert.That(max, Is.LessThanOrEqualTo(560));
        Assert.That(min, Is.LessThanOrEqualTo(max));
    }
}
