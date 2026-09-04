namespace ProjectAegis.Delegation.UnityAdapter.Tests.Bridge;

using NUnit.Framework;

/// <summary>
/// S122-08 / DRG-168: EngageExplain presentation bind + smoke CombatDomains GO.
/// Headless file contracts — no Editor MCP. StatusLine/ReasonPlain stay text.
/// Display: Flex when selected unit OR selected contact OR non-empty explain; else None.
/// </summary>
[TestFixture]
public sealed class EngageExplainPanelHostContractTests
{
    [Test]
    public void Engage_explain_uxml_declares_root_status_and_reason_labels()
    {
        var uxmlPath = RequireEngageExplainUxmlPath();
        Assert.That(File.Exists(uxmlPath), Is.True, $"Missing UXML: {uxmlPath}");

        var uxml = File.ReadAllText(uxmlPath);
        Assert.That(uxml, Does.Contain("name=\"engage-explain-root\""), "UXML missing engage-explain-root");
        Assert.That(uxml, Does.Contain("name=\"engage-explain-status\""), "UXML missing engage-explain-status");
        Assert.That(uxml, Does.Contain("name=\"engage-explain-reason\""), "UXML missing engage-explain-reason");
        Assert.That(uxml, Does.Contain("<ui:Label"), "Status/reason must be Label text, not custom widgets");
    }

    [Test]
    public void Engage_explain_host_binds_statusline_and_reasonplain_as_text()
    {
        var host = File.ReadAllText(RequireEngageExplainHostPath());
        Assert.That(host, Does.Contain("\"engage-explain-root\""), "Host missing root name");
        Assert.That(host, Does.Contain("\"engage-explain-status\""), "Host missing status name");
        Assert.That(host, Does.Contain("\"engage-explain-reason\""), "Host missing reason name");
        Assert.That(host, Does.Contain("Q<Label>(StatusName)"), "Host must Q status as Label");
        Assert.That(host, Does.Contain("Q<Label>(ReasonName)"), "Host must Q reason as Label");
        Assert.That(host, Does.Contain("_statusLabel.text = _last.StatusLine"), "StatusLine stays text");
        Assert.That(host, Does.Contain("_reasonLabel.text = _last.ReasonPlain"), "ReasonPlain stays text");
        Assert.That(host, Does.Contain("ProjectSelectedEngageExplain()"), "Host binds existing projection");
        Assert.That(host, Does.Not.Contain("DelegationBridge.Tick"));
        Assert.That(host, Does.Not.Contain("CatalogWriteGate"));
    }

    [Test]
    public void Engage_explain_host_shows_on_selection_or_non_empty_explain_and_hides_otherwise()
    {
        var host = File.ReadAllText(RequireEngageExplainHostPath());
        Assert.That(host, Does.Contain("SelectedUnitId"), "Show when a unit is selected");
        Assert.That(host, Does.Contain("SelectedContactId"), "Show when a contact is selected (DRG-180 EXPLAIN deep-link)");
        Assert.That(host, Does.Contain("EngageExplain.Empty"), "Hide when explain is Empty and nothing selected");
        Assert.That(host, Does.Contain("DisplayStyle.Flex"), "Visible panel uses display flex");
        Assert.That(host, Does.Contain("DisplayStyle.None"), "Hidden panel uses display none");
        Assert.That(
            host,
            Does.Contain("hasSelectedUnit || hasSelectedContact || hasExplain"),
            "Flex only when selected unit OR selected contact OR non-empty explain");
        Assert.That(
            host,
            Does.Not.Contain("rootEl.style.display = showPanel ? DisplayStyle.Flex : DisplayStyle.None"),
            "Must not always show when showPanel is true");
    }

    [Test]
    public void Combat_domains_uxml_and_uss_exist_for_smoke_host()
    {
        var repoRoot = RequireRepoRoot();
        var uxml = Path.Combine(
            repoRoot,
            "unity",
            "ProjectAegis",
            "Assets",
            "UI",
            "CombatDomains",
            "CombatDomainsHotTick.uxml");
        var uss = Path.Combine(
            repoRoot,
            "unity",
            "ProjectAegis",
            "Assets",
            "UI",
            "CombatDomains",
            "CombatDomainsHotTick.uss");
        Assert.That(File.Exists(uxml), Is.True, $"Missing CombatDomains UXML: {uxml}");
        Assert.That(File.Exists(uss), Is.True, $"Missing CombatDomains USS: {uss}");
        Assert.That(File.ReadAllText(uxml), Does.Contain("name=\"combat-domains-hot-tick-root\""));
    }

    [Test]
    public void Delegation_smoke_scene_builder_creates_and_ensures_combat_domains_host()
    {
        var builder = File.ReadAllText(RequireSmokeBuilderPath());
        var smokeBuildStart = builder.IndexOf(
            "public static void Build(string scenarioPolicyId",
            StringComparison.Ordinal);
        var cesiumBuildStart = builder.IndexOf(
            "public static void BuildCesiumSpikeScene(",
            StringComparison.Ordinal);
        Assert.That(smokeBuildStart, Is.GreaterThanOrEqualTo(0));
        Assert.That(cesiumBuildStart, Is.GreaterThan(smokeBuildStart));
        var smokeSection = builder.Substring(smokeBuildStart, cesiumBuildStart - smokeBuildStart);

        Assert.That(smokeSection, Does.Contain("CreatePanelHost<CombatDomainsHotTickHost>"));
        Assert.That(smokeSection, Does.Contain("\"CombatDomains\""));
        Assert.That(smokeSection, Does.Contain("Assets/UI/CombatDomains/CombatDomainsHotTick.uxml"));
        Assert.That(smokeSection, Does.Contain("Assets/UI/CombatDomains/CombatDomainsHotTick.uss"));
        Assert.That(smokeSection, Does.Contain("CreatePanelHost<ContactDetailPanelHost>"), "Do not revert ContactDetail");
        Assert.That(smokeSection, Does.Contain("CreatePanelHost<SensorToShooterPanelHost>"), "Do not revert SensorToShooter");

        var ensureStart = builder.IndexOf(
            "public static void EnsureUiMaturityHostsOnOpenScene()",
            StringComparison.Ordinal);
        Assert.That(ensureStart, Is.GreaterThanOrEqualTo(0));
        var ensureSection = builder.Substring(ensureStart);
        Assert.That(ensureSection, Does.Contain("EnsurePanelHostIfMissing<CombatDomainsHotTickHost>"));
        Assert.That(ensureSection, Does.Contain("EnsurePanelHostIfMissing<ContactDetailPanelHost>"));
        Assert.That(ensureSection, Does.Contain("EnsurePanelHostIfMissing<SensorToShooterPanelHost>"));
    }

    private static string RequireEngageExplainUxmlPath()
    {
        return Path.Combine(
            RequireRepoRoot(),
            "unity",
            "ProjectAegis",
            "Assets",
            "UI",
            "EngageExplain",
            "EngageExplainPanel.uxml");
    }

    private static string RequireEngageExplainHostPath()
    {
        return Path.Combine(
            RequireRepoRoot(),
            "unity",
            "ProjectAegis",
            "Assets",
            "Scripts",
            "Runtime",
            "EngageExplainPanelHost.cs");
    }

    private static string RequireSmokeBuilderPath()
    {
        return Path.Combine(
            RequireRepoRoot(),
            "unity",
            "ProjectAegis",
            "Assets",
            "Editor",
            "DelegationSmokeSceneBuilder.cs");
    }

    private static string RequireRepoRoot()
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

        Assert.Fail("Could not locate repo root");
        return string.Empty;
    }
}
