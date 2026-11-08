using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

/// <summary>S106 — ASSET-007/008 production style-path parity (no invent Approved).</summary>
public sealed class SpeccedC2PanelStylePathsTests
{
    [Test]
    public void Asset007_left_drawer_production_uss_resolves_and_has_style_parity()
    {
        var root = FindRepoRoot();
        Assert.That(root, Is.Not.Null);

        Assert.That(
            SpeccedC2PanelStylePaths.TryResolveUnderRepoRoot(
                root!,
                SpeccedC2PanelStylePaths.Asset007LeftDrawerUss,
                out var full),
            Is.True);
        Assert.That(File.Exists(full), Is.True);

        var text = File.ReadAllText(full);
        Assert.That(text, Does.Contain("c2-drawer").IgnoreCase.Or.Contain("AegisTokens"));
        Assert.That(SpeccedC2PanelStylePaths.ProductionLeftDrawerUssHasStyleParity(root!), Is.True);
    }

    [Test]
    public void Asset008_right_unit_production_uss_resolves_and_has_style_parity()
    {
        var root = FindRepoRoot();
        Assert.That(root, Is.Not.Null);

        Assert.That(
            SpeccedC2PanelStylePaths.TryResolveUnderRepoRoot(
                root!,
                SpeccedC2PanelStylePaths.Asset008RightUnitDetailUss,
                out var full),
            Is.True);
        Assert.That(File.Exists(full), Is.True);

        var text = File.ReadAllText(full);
        Assert.That(text, Does.Contain("unit-detail").IgnoreCase.Or.Contain("AegisTokens"));
        Assert.That(SpeccedC2PanelStylePaths.ProductionRightUnitUssHasStyleParity(root!), Is.True);
    }

    [Test]
    public void BothProductionPanelsHaveStyleParity_true_for_repo()
    {
        var root = FindRepoRoot();
        Assert.That(root, Is.Not.Null);
        Assert.That(SpeccedC2PanelStylePaths.BothProductionPanelsHaveStyleParity(root!), Is.True);
    }

    [Test]
    public void Unity_side_path_constants_resolve_under_repo_root()
    {
        var root = FindRepoRoot();
        Assert.That(root, Is.Not.Null);
        Assert.That(
            SpeccedC2PanelStylePaths.TryResolveUnderRepoRoot(
                root!,
                SpeccedC2PanelStylePaths.UnityLeftDrawerUss,
                out _),
            Is.True);
        Assert.That(
            SpeccedC2PanelStylePaths.TryResolveUnderRepoRoot(
                root!,
                SpeccedC2PanelStylePaths.UnityRightUnitDetailUss,
                out _),
            Is.True);
    }

    [Test]
    public void Paths_do_not_claim_Approved_status_in_constants()
    {
        // Specced path surface — constants must not be named/hosted as Approved* graduates.
        Assert.That(nameof(SpeccedC2PanelStylePaths), Does.Contain("Specced"));
        Assert.That(SpeccedC2PanelStylePaths.Asset007LeftDrawerUss, Does.Not.Contain("Approved"));
        Assert.That(SpeccedC2PanelStylePaths.Asset008RightUnitDetailUss, Does.Not.Contain("Approved"));
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
