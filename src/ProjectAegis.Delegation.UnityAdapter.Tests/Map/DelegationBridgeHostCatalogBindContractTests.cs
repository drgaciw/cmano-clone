using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Map;

/// <summary>
/// Source contract: Play Mode host binds a catalog so CMD-32 datalink edges can project (DRG-162).
/// </summary>
public sealed class DelegationBridgeHostCatalogBindContractTests
{
    [Test]
    public void DelegationBridgeHost_binds_CatalogReader_in_Awake()
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
            "DelegationBridgeHost.cs");
        Assert.That(File.Exists(hostPath), Is.True, hostPath);

        var text = File.ReadAllText(hostPath);
        Assert.That(text, Does.Contain("CatalogReader ??="));
        Assert.That(text, Does.Contain("Session?.CatalogReader"));
        Assert.That(text, Does.Contain("CatalogReaderFactory"));
        Assert.That(text, Does.Not.Contain("DelegationBridge.Tick"));
        Assert.That(text, Does.Not.Contain("CatalogWriteGate"));
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
