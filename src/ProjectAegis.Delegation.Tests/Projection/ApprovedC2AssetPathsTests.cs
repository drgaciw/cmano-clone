using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

public sealed class ApprovedC2AssetPathsTests
{
    [Test]
    public void Production_ASSET005_top_bar_uss_resolves_under_repo_root()
    {
        var root = FindRepoRoot();
        Assert.That(root, Is.Not.Null);

        Assert.That(
            ApprovedC2AssetPaths.TryResolveUnderRepoRoot(
                root!,
                ApprovedC2AssetPaths.Asset005TopBarUss,
                out var full),
            Is.True);
        Assert.That(File.Exists(full), Is.True);

        var text = File.ReadAllText(full);
        Assert.That(text, Does.Contain("C2TopBar").IgnoreCase.Or.Contain("topbar").IgnoreCase.Or.Contain("AegisTokens"));
    }

    [Test]
    public void ProductionTopBarUssHasTokenParity_true_for_repo()
    {
        var root = FindRepoRoot();
        Assert.That(root, Is.Not.Null);
        Assert.That(ApprovedC2AssetPaths.ProductionTopBarUssHasTokenParity(root!), Is.True);
    }

    [Test]
    public void Approved_paths_for_006_021_004_exist()
    {
        var root = FindRepoRoot();
        Assert.That(root, Is.Not.Null);
        Assert.That(ApprovedC2AssetPaths.TryResolveUnderRepoRoot(root!, ApprovedC2AssetPaths.Asset006MessageLogUss, out _), Is.True);
        Assert.That(ApprovedC2AssetPaths.TryResolveUnderRepoRoot(root!, ApprovedC2AssetPaths.Asset021CombatDomainsUss, out _), Is.True);
        Assert.That(ApprovedC2AssetPaths.TryResolveUnderRepoRoot(root!, ApprovedC2AssetPaths.Asset004App6Atlas, out _), Is.True);
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
