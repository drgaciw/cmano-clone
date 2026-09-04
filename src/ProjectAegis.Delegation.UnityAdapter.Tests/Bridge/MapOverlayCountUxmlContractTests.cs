namespace ProjectAegis.Delegation.UnityAdapter.Tests.Bridge;

using NUnit.Framework;

/// <summary>
/// S122-03 lock: map overlay count labels exist in MapPlaceholder UXML and the host
/// Q()s those names into ApplyOverlayCounts. Headless file contract — no Editor MCP.
/// Does not re-test overlay projection math (see MapPanelApplyStateTests).
/// </summary>
[TestFixture]
public sealed class MapOverlayCountUxmlContractTests
{
    [Test]
    public void Map_placeholder_uxml_declares_envelope_and_datalink_count_labels()
    {
        var uxmlPath = RequireMapPlaceholderUxmlPath();
        Assert.That(File.Exists(uxmlPath), Is.True, $"Missing UXML: {uxmlPath}");

        var uxml = File.ReadAllText(uxmlPath);
        Assert.That(uxml, Does.Contain("name=\"envelope-ring-count\""), "UXML missing envelope-ring-count");
        Assert.That(uxml, Does.Contain("name=\"datalink-edge-count\""), "UXML missing datalink-edge-count");
        Assert.That(uxml, Does.Contain("ENVELOPES:"), "UXML default text missing ENVELOPES:");
        Assert.That(uxml, Does.Contain("DATALINKS:"), "UXML default text missing DATALINKS:");
        Assert.That(uxml, Does.Contain("text=\"ENVELOPES: 0\""), "UXML default envelope label text");
        Assert.That(uxml, Does.Contain("text=\"DATALINKS: 0\""), "UXML default datalink label text");
    }

    [Test]
    public void Map_placeholder_host_qs_overlay_count_names_and_formats_apply_overlay_counts()
    {
        var hostPath = RequireMapPlaceholderHostPath();
        Assert.That(File.Exists(hostPath), Is.True, $"Missing host: {hostPath}");

        var host = File.ReadAllText(hostPath);
        Assert.That(host, Does.Contain("\"envelope-ring-count\""), "Host missing envelope-ring-count constant");
        Assert.That(host, Does.Contain("\"datalink-edge-count\""), "Host missing datalink-edge-count constant");
        Assert.That(host, Does.Contain("ApplyOverlayCounts"), "Host missing ApplyOverlayCounts");
        Assert.That(host, Does.Contain("Q<Label>(EnvelopeRingCountName)"), "Host must Q envelope-ring-count");
        Assert.That(host, Does.Contain("Q<Label>(DatalinkEdgeCountName)"), "Host must Q datalink-edge-count");
        Assert.That(host, Does.Contain("ENVELOPES:"), "Host missing ENVELOPES: format");
        Assert.That(host, Does.Contain("DATALINKS:"), "Host missing DATALINKS: format");
        Assert.That(host, Does.Contain("$\"ENVELOPES: {LastEnvelopeRingCount}\""), "Host ApplyOverlayCounts envelope format");
        Assert.That(host, Does.Contain("$\"DATALINKS: {LastDatalinkEdgeCount}\""), "Host ApplyOverlayCounts datalink format");
    }

    [Test]
    public void Map_placeholder_host_reuses_existing_overlay_projections()
    {
        var host = File.ReadAllText(RequireMapPlaceholderHostPath());
        Assert.That(host, Does.Contain("TacticalOverlayProjection"), "Host must keep TacticalOverlayProjection");
        Assert.That(host, Does.Contain("DatalinkUnitPairFeed"), "Host must keep DatalinkUnitPairFeed");
        Assert.That(host, Does.Contain("MapPanelApplyState.Apply"), "Host must keep MapPanelApplyState.Apply");
        Assert.That(host, Does.Not.Contain("DelegationBridge.Tick"));
        Assert.That(host, Does.Not.Contain("CatalogWriteGate"));
    }

    private static string RequireMapPlaceholderUxmlPath()
    {
        var repoRoot = FindRepoRoot();
        Assert.That(repoRoot, Is.Not.Null, "Could not locate repo root");
        return Path.Combine(
            repoRoot!,
            "unity",
            "ProjectAegis",
            "Assets",
            "UI",
            "MapPlaceholder",
            "MapPlaceholderPanel.uxml");
    }

    private static string RequireMapPlaceholderHostPath()
    {
        var repoRoot = FindRepoRoot();
        Assert.That(repoRoot, Is.Not.Null, "Could not locate repo root");
        return Path.Combine(
            repoRoot!,
            "unity",
            "ProjectAegis",
            "Assets",
            "Scripts",
            "Runtime",
            "MapPlaceholderPanelHost.cs");
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
