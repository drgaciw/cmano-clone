using System.IO;
using NUnit.Framework;
using ProjectAegis.Delegation.UnityAdapter.Presentation;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

/// <summary>DRG-270 — focus-ring token + keyboard discovery baseline (headless contract).</summary>
[TestFixture]
public sealed class C2FocusRingBaselineTests
{
    [Test]
    public void Aegis_tokens_declare_focus_ring_variables()
    {
        var repoRoot = UiIaSourceReader.RequireRepoRoot();
        var tokensPath = Path.Combine(
            repoRoot,
            "unity",
            "ProjectAegis",
            "Assets",
            "UI",
            "AegisTokens.uss");
        Assert.That(File.Exists(tokensPath), Is.True);

        var uss = File.ReadAllText(tokensPath);
        Assert.That(uss, Does.Contain(FocusRingTokens.RingColorVariable + ":"));
        Assert.That(uss, Does.Contain(FocusRingTokens.RingWidthVariable + ":"));
        Assert.That(uss, Does.Contain(FocusRingTokens.RingColorAlias));
        Assert.That(uss, Does.Contain(FocusRingTokens.RingWidthAlias));
    }

    [Test]
    public void C2_left_drawer_oob_and_message_log_uss_use_focus_ring_token()
    {
        var repoRoot = UiIaSourceReader.RequireRepoRoot();
        var drawerUss = File.ReadAllText(Path.Combine(
            repoRoot,
            "unity",
            "ProjectAegis",
            "Assets",
            "UI",
            "C2LeftDrawer",
            "C2LeftDrawerPanel.uss"));
        var messageUss = File.ReadAllText(Path.Combine(
            repoRoot,
            "unity",
            "ProjectAegis",
            "Assets",
            "UI",
            "MessageLog",
            "MessageLogPanel.uss"));
        var oobUss = File.ReadAllText(Path.Combine(
            repoRoot,
            "unity",
            "ProjectAegis",
            "Assets",
            "UI",
            "OobTree",
            "OobTreePanel.uss"));

        Assert.That(drawerUss, Does.Contain(FocusRingTokens.RingColorReference));
        Assert.That(drawerUss, Does.Contain(FocusRingUssSelectors.OobRowFocused));

        Assert.That(messageUss, Does.Contain(FocusRingTokens.RingColorReference));
        Assert.That(messageUss, Does.Contain(FocusRingUssSelectors.MessageLogSelectableFocused));

        Assert.That(oobUss, Does.Contain("@import url(\"../AegisTokens.uss\")"));
        Assert.That(oobUss, Does.Contain(FocusRingUssSelectors.OobRowFocused));
    }

    [Test]
    public void Oob_and_message_log_hosts_wire_focusable_list_rows()
    {
        var oobHost = UiIaSourceReader.ReadRuntime("OobTreePanelHost.cs");
        var messageHost = UiIaSourceReader.ReadRuntime("MessageLogPanelHost.cs");

        Assert.That(oobHost, Does.Contain("label.focusable = true"));
        Assert.That(oobHost, Does.Contain("KeyboardDiscoverySurfaces.OobListElementName"));
        Assert.That(oobHost, Does.Contain("bridgeHost.SelectUnit"));
        Assert.That(oobHost, Does.Contain("OnOobRowKeyDown"));

        Assert.That(messageHost, Does.Contain("label.focusable = true"));
        Assert.That(messageHost, Does.Contain("KeyboardDiscoverySurfaces.MessageLogListElementName"));
        Assert.That(messageHost, Does.Contain(FocusRingTokens.RingColorVariable));
    }
}
