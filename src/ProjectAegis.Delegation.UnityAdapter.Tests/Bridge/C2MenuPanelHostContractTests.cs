using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Bridge;

/// <summary>
/// S122-11 / DRG-189: C2 menu host stays on existing C2MenuProjection.ProjectDefault.
/// Honest empty-state — no hardcoded Baltic package painted as live.
/// </summary>
[TestFixture]
public sealed class C2MenuPanelHostContractTests
{
    [Test]
    public void Host_calls_project_default_without_fake_live_package()
    {
        var host = File.ReadAllText(RequireHostPath());
        Assert.That(host, Does.Contain("C2MenuProjection.ProjectDefault(_bookmarkCount, stack)"));
        Assert.That(host, Does.Contain("item.StatusNote"));
        Assert.That(host, Does.Contain("item.DisabledReason"));
        Assert.That(host, Does.Not.Contain("DelegationBridge.Tick"));
        Assert.That(host, Does.Not.Contain("CatalogWriteGate"));
        Assert.That(host, Does.Not.Contain("BalticAsuwPackage"));
        Assert.That(host, Does.Not.Contain("elem-c2-1"));
        Assert.That(host, Does.Not.Contain("MissionPackageProjection.Project"));
        Assert.That(host, Does.Not.Contain("new UIDocument"));
    }

    private static string RequireHostPath()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(
                dir.FullName,
                "unity",
                "ProjectAegis",
                "Assets",
                "Scripts",
                "Runtime",
                "C2MenuPanelHost.cs");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new FileNotFoundException("C2MenuPanelHost.cs not found from test directory.");
    }
}
