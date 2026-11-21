using System.Text.RegularExpressions;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

/// <summary>UAT D30: Contact Detail must be opaque over Unit Detail and end above the Message Log.</summary>
[TestFixture]
public sealed class ContactDetailPanelStackingLayoutTests
{
    [Test]
    public void Contact_detail_root_uses_opaque_panel_token_and_clears_message_log()
    {
        var uss = UiIaSourceReader.ReadUnder("unity", "ProjectAegis", "Assets", "UI", "ContactDetail", "ContactDetailPanel.uss");
        var tokens = UiIaSourceReader.ReadUnder("unity", "ProjectAegis", "Assets", "UI", "AegisTokens.uss");
        var m = Regex.Match(uss, @"\.contact-detail-panel\s*\{([^}]*)\}");
        Assert.That(m.Success, Is.True, ".contact-detail-panel rule missing");
        var body = m.Groups[1].Value;

        Assert.That(uss, Does.Contain("@import url(\"../AegisTokens.uss\")"));
        Assert.That(body, Does.Contain("background-color: var(--surface-panel-opaque)"));
        Assert.That(body, Does.Not.Contain("rgba("));
        Assert.That(body, Does.Contain("bottom: var(--c2-message-log-clearance)"));
        // UAT D30b: the Engagement / Posture lines had no colour rule and rendered near-black
        // (rgb 27,27,27) on the opaque dark panel.
        foreach (var id in new[] { "#contact-engagement-line", "#contact-posture-line" })
        {
            Assert.That(uss, Does.Match(Regex.Escape(id) + @"[^{]*\{[^}]*color:\s*var\(--text-"), $"{id} needs a text colour token.");
        }
        Assert.That(tokens, Does.Match(@"--surface-panel-opaque:\s*(#[0-9A-Fa-f]{6}|rgb\()"));
        Assert.That(tokens, Does.Match(@"--c2-message-log-clearance:\s*\d+px"));
    }
}
