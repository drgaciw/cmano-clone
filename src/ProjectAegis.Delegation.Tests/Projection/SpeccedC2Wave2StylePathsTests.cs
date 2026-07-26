using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

/// <summary>S107 — ASSET-009…013 Specced production style-path parity.</summary>
public sealed class SpeccedC2Wave2StylePathsTests
{
    [Test]
    public void Wave2_production_uss_paths_resolve_and_have_parity()
    {
        var root = FindRepoRoot();
        Assert.That(root, Is.Not.Null);

        Assert.That(SpeccedC2PanelStylePaths.TryResolveUnderRepoRoot(root!, SpeccedC2PanelStylePaths.Asset009MapPlaceholderUss, out _), Is.True);
        Assert.That(SpeccedC2PanelStylePaths.TryResolveUnderRepoRoot(root!, SpeccedC2PanelStylePaths.Asset010MapCommsDegradeUss, out _), Is.True);
        Assert.That(SpeccedC2PanelStylePaths.TryResolveUnderRepoRoot(root!, SpeccedC2PanelStylePaths.Asset011DelegationBadgeUss, out _), Is.True);
        Assert.That(SpeccedC2PanelStylePaths.TryResolveUnderRepoRoot(root!, SpeccedC2PanelStylePaths.Asset012PolicyEmconHudUss, out _), Is.True);
        Assert.That(SpeccedC2PanelStylePaths.TryResolveUnderRepoRoot(root!, SpeccedC2PanelStylePaths.Asset013ReplayScrubberUss, out _), Is.True);

        Assert.That(SpeccedC2PanelStylePaths.Wave2ProductionPanelsHaveStyleParity(root!), Is.True);
    }

    [Test]
    public void S106_panels_still_have_style_parity()
    {
        var root = FindRepoRoot();
        Assert.That(root, Is.Not.Null);
        Assert.That(SpeccedC2PanelStylePaths.BothProductionPanelsHaveStyleParity(root!), Is.True);
    }

    private static string? FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 10; i++)
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
