namespace ProjectAegis.Delegation.UnityAdapter.Tests.Bridge;

using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Delegation.Watch;
using NUnit.Framework;

/// <summary>CMD-39 Track A — UXML / host / scene-builder contracts (headless file asserts).</summary>
[TestFixture]
public sealed class AttentionToastHostContractTests
{
    [Test]
    public void Attention_toast_uxml_declares_stable_element_names()
    {
        var repoRoot = FindRepoRoot();
        Assert.That(repoRoot, Is.Not.Null);
        var uxmlPath = Path.Combine(
            repoRoot!,
            "unity",
            "ProjectAegis",
            "Assets",
            "UI",
            "AttentionToast",
            "AttentionToastPanel.uxml");
        Assert.That(File.Exists(uxmlPath), Is.True);
        var uxml = File.ReadAllText(uxmlPath);
        foreach (var name in new[]
                 {
                     "attention-toast-root",
                     "attention-toast-card",
                     "attention-toast-severity",
                     "attention-toast-title",
                     "attention-toast-body",
                     "attention-toast-queue",
                     "attention-toast-ack",
                     "attention-toast-dismiss",
                 })
        {
            Assert.That(uxml, Does.Contain($"name=\"{name}\""), "Missing UXML element: " + name);
        }
    }

    [Test]
    public void Attention_toast_host_binds_projection_not_bridge_tick()
    {
        var repoRoot = FindRepoRoot();
        Assert.That(repoRoot, Is.Not.Null);
        var hostPath = Path.Combine(
            repoRoot!,
            "unity",
            "ProjectAegis",
            "Assets",
            "Scripts",
            "Runtime",
            "AttentionToastPanelHost.cs");
        Assert.That(File.Exists(hostPath), Is.True);
        var host = File.ReadAllText(hostPath);
        Assert.That(host, Does.Contain("RefreshAttentionToast()"));
        Assert.That(host, Does.Contain("TryAcknowledgeAttentionToast"));
        Assert.That(host, Does.Not.Contain("Bridge.Tick"));
        Assert.That(host, Does.Not.Contain("DelegationBridge.Tick"));
    }

    [Test]
    public void Top_bar_uxml_declares_interactive_compression_controls()
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
        var hostPath = Path.Combine(
            repoRoot!,
            "unity",
            "ProjectAegis",
            "Assets",
            "Scripts",
            "Runtime",
            "C2TopBarPanelHost.cs");
        var uxml = File.ReadAllText(uxmlPath);
        var host = File.ReadAllText(hostPath);
        Assert.That(uxml, Does.Contain("compression-slower-button"));
        Assert.That(uxml, Does.Contain("compression-faster-button"));
        Assert.That(uxml, Does.Contain("pause-resume-button"));
        Assert.That(uxml, Does.Contain("compression-label"));
        Assert.That(host, Does.Contain("TrySetTimeAcceleration"));
        Assert.That(host, Does.Contain("TryPauseSim"));
        Assert.That(host, Does.Contain("TryResumeSim"));
        Assert.That(host, Does.Contain("LiveCompressionLabel"));
        Assert.That(host, Does.Not.Contain("Bridge.Tick"));
    }

    [Test]
    public void Delegation_smoke_scene_builder_includes_attention_toast_host()
    {
        var repoRoot = FindRepoRoot();
        Assert.That(repoRoot, Is.Not.Null);
        var builderPath = Path.Combine(
            repoRoot!,
            "unity",
            "ProjectAegis",
            "Assets",
            "Editor",
            "DelegationSmokeSceneBuilder.cs");
        var builder = File.ReadAllText(builderPath);
        var smokeBuildStart = builder.IndexOf(
            "public static void Build(string scenarioPolicyId",
            StringComparison.Ordinal);
        var cesiumBuildStart = builder.IndexOf(
            "public static void BuildCesiumSpikeScene(",
            StringComparison.Ordinal);
        var smokeSection = builder.Substring(smokeBuildStart, cesiumBuildStart - smokeBuildStart);
        Assert.That(smokeSection, Does.Contain("AttentionToastPanelHost"));
        Assert.That(smokeSection, Does.Contain("\"AttentionToast\""));
        Assert.That(smokeSection, Does.Contain("Assets/UI/AttentionToast/AttentionToastPanel.uxml"));
        Assert.That(builder, Does.Contain("EnsurePanelHostIfMissing<AttentionToastPanelHost>"));
    }

    [Test]
    public void Host_seed_demo_watch_pauses_and_projects_toast()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: true);
        Assert.That(bridge.Session, Is.Not.Null);
        bridge.Session!.ReportWatchAttention(new WatchAttentionEvent(
            "watch:demo:hostile-1",
            WatchAttentionKind.HostileOrUnknownContact,
            WatchAttentionPriority.Critical,
            1,
            "hostile-1",
            ReasonDetail: "Play Mode demo contact"));

        var binder = new ProjectAegis.Delegation.Projection.AttentionToastBinder();
        var toast = binder.Refresh(
            bridge.Orchestrator.DecisionLog,
            bridge.Session.WatchQueue,
            bridge.Session.WatchPauseGate);
        Assert.That(bridge.Session.IsSimPaused, Is.True);
        Assert.That(toast.HasActiveCard, Is.True);
        Assert.That(toast.Active!.IsPauseClass, Is.True);
        Assert.That(C2ClockCommand.FormatCompressionLabel(true, 1), Is.EqualTo("TIME: PAUSED"));
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
