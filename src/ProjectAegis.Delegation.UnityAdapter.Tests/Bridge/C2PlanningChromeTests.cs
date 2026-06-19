namespace ProjectAegis.Delegation.UnityAdapter.Tests.Bridge;

using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using NUnit.Framework;

/// <summary>
/// Headless merge authority for S30-07 Planning-phase C2 chrome (map dimmed, drawer read-only).
/// </summary>
[TestFixture]
public sealed class C2PlanningChromeTests
{
    [Test]
    public void Planning_chrome_projection_dims_map_and_readonly_drawer()
    {
        var chrome = C2PlanningChromeProjection.Project(SimulationPhase.Planning);

        Assert.That(chrome.IsMapDimmed, Is.True);
        Assert.That(chrome.IsDrawerReadOnly, Is.True);
        Assert.That(chrome.Phase, Is.EqualTo(SimulationPhase.Planning));
    }

    [Test]
    public void Executing_phase_skips_planning_chrome()
    {
        var chrome = C2PlanningChromeProjection.Project(SimulationPhase.Executing);

        Assert.That(chrome.IsMapDimmed, Is.False);
        Assert.That(chrome.IsDrawerReadOnly, Is.False);
        Assert.That(chrome.Phase, Is.EqualTo(SimulationPhase.Executing));
    }

    [Test]
    public void BeginExecution_clears_planning_chrome_projection()
    {
        var bridge = new DelegationBridge(42);
        Assert.That(C2PlanningChromeProjection.Project(bridge.Phase).IsMapDimmed, Is.True);
        Assert.That(C2PlanningChromeProjection.Project(bridge.Phase).IsDrawerReadOnly, Is.True);

        bridge.BeginExecution();

        var chrome = C2PlanningChromeProjection.Project(bridge.Phase);
        Assert.That(chrome.IsMapDimmed, Is.False);
        Assert.That(chrome.IsDrawerReadOnly, Is.False);
        Assert.That(chrome.Phase, Is.EqualTo(SimulationPhase.Executing));
        Assert.That(bridge.Orchestrator.DecisionLog.ModeChanges, Has.Count.EqualTo(1));
        Assert.That(bridge.Orchestrator.DecisionLog.ModeChanges[0].NewMode, Is.EqualTo("Executing"));
    }

    [Test]
    public void Map_placeholder_panel_binds_dimmed_state_while_planning()
    {
        var repoRoot = RequireRepoRoot();
        var hostPath = Path.Combine(
            repoRoot,
            "unity",
            "ProjectAegis",
            "Assets",
            "Scripts",
            "Runtime",
            "MapPlaceholderPanelHost.cs");
        var ussPath = Path.Combine(
            repoRoot,
            "unity",
            "ProjectAegis",
            "Assets",
            "UI",
            "MapPlaceholder",
            "MapPlaceholderPanel.uss");
        var uxmlPath = Path.Combine(
            repoRoot,
            "unity",
            "ProjectAegis",
            "Assets",
            "UI",
            "MapPlaceholder",
            "MapPlaceholderPanel.uxml");

        Assert.That(File.Exists(hostPath), Is.True);
        Assert.That(File.Exists(ussPath), Is.True);
        Assert.That(File.Exists(uxmlPath), Is.True);

        var host = File.ReadAllText(hostPath);
        var uss = File.ReadAllText(ussPath);
        var uxml = File.ReadAllText(uxmlPath);

        Assert.That(host, Does.Contain("C2PlanningChromeProjection.Project"));
        Assert.That(host, Does.Contain("IsDimmed"));
        Assert.That(host, Does.Contain("map-placeholder-panel--planning-dimmed"));
        Assert.That(uss, Does.Contain(".map-placeholder-panel--planning-dimmed"));
        Assert.That(uxml, Does.Contain("planning-dim-overlay"));
    }

    [Test]
    public void Left_drawer_panel_gates_read_only_while_planning()
    {
        var repoRoot = RequireRepoRoot();
        var hostPath = Path.Combine(
            repoRoot,
            "unity",
            "ProjectAegis",
            "Assets",
            "Scripts",
            "Runtime",
            "C2LeftDrawerPanelHost.cs");
        var ussPath = Path.Combine(
            repoRoot,
            "unity",
            "ProjectAegis",
            "Assets",
            "UI",
            "C2LeftDrawer",
            "C2LeftDrawerPanel.uss");

        Assert.That(File.Exists(hostPath), Is.True);
        Assert.That(File.Exists(ussPath), Is.True);

        var host = File.ReadAllText(hostPath);
        var uss = File.ReadAllText(ussPath);

        Assert.That(host, Does.Contain("C2PlanningChromeProjection.Project"));
        Assert.That(host, Does.Contain("IsDrawerReadOnly"));
        Assert.That(host, Does.Contain("c2-drawer-panel--planning-readonly"));
        Assert.That(host, Does.Contain("OnTabChanged"));
        Assert.That(host, Does.Contain("bridgeHost.SelectUnit"));
        Assert.That(uss, Does.Contain(".c2-drawer-panel--planning-readonly"));
    }

    [Test]
    public void Begin_execution_still_routes_through_bridge_host_seam()
    {
        var repoRoot = RequireRepoRoot();
        var topBarHostPath = Path.Combine(
            repoRoot,
            "unity",
            "ProjectAegis",
            "Assets",
            "Scripts",
            "Runtime",
            "C2TopBarPanelHost.cs");
        var bridgeHostPath = Path.Combine(
            repoRoot,
            "unity",
            "ProjectAegis",
            "Assets",
            "Scripts",
            "Runtime",
            "DelegationBridgeHost.cs");

        Assert.That(File.Exists(topBarHostPath), Is.True);
        Assert.That(File.Exists(bridgeHostPath), Is.True);

        var topBarHost = File.ReadAllText(topBarHostPath);
        var bridgeHost = File.ReadAllText(bridgeHostPath);

        Assert.That(topBarHost, Does.Contain("bridgeHost.BeginExecution()"));
        Assert.That(topBarHost, Does.Not.Contain("Bridge.BeginExecution()"));
        Assert.That(bridgeHost, Does.Contain("public void BeginExecution() => Bridge.BeginExecution();"));
    }

    [Test]
    public void Planning_top_bar_projection_freezes_score_until_execution_regression()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: true);
        var log = bridge.Orchestrator.DecisionLog;
        log.AppendEngagementOutcome(new EngagementOutcomeRecord(
            1, 1, 1, new TargetId("u1"), new TargetId("hostile-1"), 1,
            ProjectAegis.Sim.Engage.EngagementOutcomeCodes.Kill, 0.1));

        var planning = C2TopBarProjection.Project(5, bridge.Phase, "1x", "Mixed", log);
        bridge.BeginExecution();
        var executing = C2TopBarProjection.Project(5, bridge.Phase, "1x", "Mixed", log);

        Assert.That(planning.ScoreLabel, Is.EqualTo("SCORE: 0  KILLS: 0  MSLS: 0"));
        Assert.That(executing.ScoreLabel, Does.Contain("KILLS: 1"));
    }

    private static string RequireRepoRoot()
    {
        var repoRoot = FindRepoRoot();
        Assert.That(repoRoot, Is.Not.Null);
        return repoRoot!;
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