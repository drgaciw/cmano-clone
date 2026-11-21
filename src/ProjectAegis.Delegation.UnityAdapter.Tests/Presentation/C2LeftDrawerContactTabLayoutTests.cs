using System.Text.RegularExpressions;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

/// <summary>UAT D31 / D12: left-drawer contacts chrome legibility, list height, collapse toggle offset.</summary>
[TestFixture]
public sealed class C2LeftDrawerContactTabLayoutTests
{
    private static string Uss() => UiIaSourceReader.ReadUnder(
        "unity", "ProjectAegis", "Assets", "UI", "C2LeftDrawer", "C2LeftDrawerPanel.uss");

    private static string Rule(string uss, string selector)
    {
        var m = Regex.Match(uss, Regex.Escape(selector) + @"\s*\{([^}]*)\}");
        Assert.That(m.Success, Is.True, $"USS rule {selector} missing");
        return m.Groups[1].Value;
    }

    [Test]
    public void Contact_chrome_dropdown_and_text_inputs_have_visible_text_color()
    {
        var uss = Uss();
        Assert.That(Rule(uss, ".c2-drawer-contact-chrome .unity-base-popup-field__text"), Does.Contain("color: var(--text-data)"));
        var input = Rule(uss, ".c2-drawer-contact-chrome .unity-base-text-field__input");
        Assert.That(input, Does.Contain("color: var(--text-data)"));
        Assert.That(input, Does.Contain("background-color"));
    }

    /// <summary>
    /// UAT D31b: Unity's default field label is 150px wide, which leaves the 63px popup input no room;
    /// the value text ("Contact ID", "All") collapsed to 0 width in Play Mode even with a visible colour.
    /// </summary>
    [Test]
    public void Contact_chrome_label_is_narrow_so_the_value_text_has_width()
    {
        var uss = Uss();
        var label = Rule(uss, ".c2-drawer-contact-chrome .unity-base-field__label");
        var minWidth = Regex.Match(label, @"min-width:\s*(\d+)px");
        Assert.That(minWidth.Success, Is.True, "Label must override Unity's 150px default min-width.");
        Assert.That(int.Parse(minWidth.Groups[1].Value), Is.LessThanOrEqualTo(64));
        Assert.That(Rule(uss, ".c2-drawer-contact-chrome .unity-base-popup-field__text"), Does.Contain("flex-grow: 1"));
    }

    [Test]
    public void Contact_list_has_height_and_visible_rows()
    {
        var uss = Uss();
        var body = Rule(uss, "#contact-list");
        Assert.That(body, Does.Contain("flex-grow: 1"));
        Assert.That(body, Does.Match(@"min-height:\s*\d+px"));
        var item = Rule(uss, "#contact-list .unity-list-view__item");
        Assert.That(item, Does.Contain("min-height"));
        Assert.That(item, Does.Contain("color: var(--text-data)"));
    }
}
