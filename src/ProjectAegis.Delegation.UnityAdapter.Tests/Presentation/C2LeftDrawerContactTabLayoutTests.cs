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
