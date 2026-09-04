using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Map;

/// <summary>
/// Source contract: map host dirty-flag uses LastCommsState (DRG-164).
/// </summary>
public sealed class MapPlaceholderPanelHostContractTests
{
    [Test]
    public void MapPlaceholderPanelHost_uses_LastCommsState_not_per_frame_project()
    {
        var root = FindRepoRoot();
        Assert.That(root, Is.Not.Null);

        var hostPath = Path.Combine(
            root!,
            "unity",
            "ProjectAegis",
            "Assets",
            "Scripts",
            "Runtime",
            "MapPlaceholderPanelHost.cs");
        Assert.That(File.Exists(hostPath), Is.True, hostPath);

        var text = File.ReadAllText(hostPath);
        Assert.That(text, Does.Contain("LastCommsState"));
        Assert.That(text, Does.Contain("MapCanvasOverlayRenderer"));
        Assert.That(text, Does.Contain("ENVELOPES:"));
        Assert.That(text, Does.Contain("DATALINKS:"));
        Assert.That(text, Does.Contain("CatalogReader"));
        Assert.That(text, Does.Not.Contain("CommsStateProjection.Project"));
        Assert.That(text, Does.Not.Contain("ProjectCommsSnapshot"));
        Assert.That(text, Does.Not.Contain("DelegationBridge.Tick"));
        Assert.That(text, Does.Not.Contain("CatalogWriteGate"));
        Assert.That(text, Does.Not.Contain("SimulationSession"));
    }

    /// <summary>
    /// Locks CS0246: SimulationPhase lives in Delegation.Orchestration (player build 2026-08-17).
    /// </summary>
    [Test]
    public void MapPlaceholderPanelHost_imports_Orchestration_for_SimulationPhase()
    {
        var root = FindRepoRoot();
        Assert.That(root, Is.Not.Null);

        var hostPath = Path.Combine(
            root!,
            "unity",
            "ProjectAegis",
            "Assets",
            "Scripts",
            "Runtime",
            "MapPlaceholderPanelHost.cs");
        Assert.That(File.Exists(hostPath), Is.True, hostPath);

        var text = File.ReadAllText(hostPath);
        Assert.That(text, Does.Contain("using ProjectAegis.Delegation.Orchestration;"));
        Assert.That(text, Does.Contain("SimulationPhase"));
    }

    private static string? FindRepoRoot()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "unity", "ProjectAegis")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        return null;
    }
}
