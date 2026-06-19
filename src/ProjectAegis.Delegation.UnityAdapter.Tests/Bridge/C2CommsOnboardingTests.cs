namespace ProjectAegis.Delegation.UnityAdapter.Tests.Bridge;

using NUnit.Framework;

/// <summary>S35-06: headless proxy for comms legend, datalink lag helper, and NPE onboarding copy.</summary>
[TestFixture]
public sealed class C2CommsOnboardingTests
{
    [Test]
    public void C2_top_bar_panel_declares_comms_legend_with_text_labels()
    {
        var repoRoot = FindRepoRoot();
        Assert.That(repoRoot, Is.Not.Null);

        var uxmlPath = Path.Combine(
            repoRoot!,
            "unity",
            "ProjectAegis",
            "Assets",
            "UI",
            "TopBar",
            "C2TopBarPanel.uxml");
        var ussPath = Path.Combine(
            repoRoot!,
            "unity",
            "ProjectAegis",
            "Assets",
            "UI",
            "TopBar",
            "C2TopBarPanel.uss");

        var uxml = File.ReadAllText(uxmlPath);
        var uss = File.ReadAllText(ussPath);

        Assert.That(uxml, Does.Contain("name=\"c2-topbar-comms-legend\""));
        Assert.That(uxml, Does.Contain("NOMINAL = full picture"));
        Assert.That(uxml, Does.Contain("DEGRADED = stale contacts, reduced symbol opacity"));
        Assert.That(uxml, Does.Contain("DENIED = no new engagements, map dimmed"));
        Assert.That(uxml, Does.Contain("name=\"comms-label\""));
        Assert.That(uss, Does.Contain(".c2-topbar-legend-item--nominal"));
        Assert.That(uss, Does.Contain(".c2-topbar-legend-item--degraded"));
        Assert.That(uss, Does.Contain(".c2-topbar-legend-item--denied"));
    }

    [Test]
    public void Platform_catalog_comms_section_declares_datalink_lag_helper()
    {
        var repoRoot = FindRepoRoot();
        Assert.That(repoRoot, Is.Not.Null);

        var uxmlPath = Path.Combine(
            repoRoot!,
            "unity",
            "ProjectAegis",
            "Assets",
            "UI",
            "PlatformCatalog",
            "PlatformCatalogPanel.uxml");

        var uxml = File.ReadAllText(uxmlPath);

        Assert.That(uxml, Does.Contain("name=\"platform-catalog-comms-lag-helper\""));
        Assert.That(uxml, Does.Contain("LatencyMsNominal (catalog) drives share-lag ticks in sim (S34-07)"));
    }

    [Test]
    public void Message_log_panel_declares_baltic_onboarding_hint()
    {
        var repoRoot = FindRepoRoot();
        Assert.That(repoRoot, Is.Not.Null);

        var uxmlPath = Path.Combine(
            repoRoot!,
            "unity",
            "ProjectAegis",
            "Assets",
            "UI",
            "MessageLog",
            "MessageLogPanel.uxml");

        var uxml = File.ReadAllText(uxmlPath);

        Assert.That(uxml, Does.Contain("name=\"message-log-onboarding-hint\""));
        Assert.That(uxml, Does.Contain("MISSION: Patrol Baltic"));
        Assert.That(uxml, Does.Contain("Begin Execution"));
    }

    [Test]
    public void Onboarding_baltic_spec_declares_mission_goal_and_first_actions()
    {
        var repoRoot = FindRepoRoot();
        Assert.That(repoRoot, Is.Not.Null);

        var specPath = Path.Combine(repoRoot!, "design", "ux", "onboarding-baltic.md");
        Assert.That(File.Exists(specPath), Is.True);

        var spec = File.ReadAllText(specPath);

        Assert.That(spec, Does.Contain("## Mission Goal"));
        Assert.That(spec, Does.Contain("## First Actions"));
        Assert.That(spec, Does.Contain("classify hostile"));
        Assert.That(spec, Does.Contain("LatencyMsNominal"));
        Assert.That(spec, Does.Contain("share-lag"));
    }

    private static string? FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8; i++)
        {
            if (File.Exists(Path.Combine(dir, "ProjectAegis.sln")))
            {
                return dir;
            }

            dir = Directory.GetParent(dir)?.FullName ?? dir;
        }

        return null;
    }
}