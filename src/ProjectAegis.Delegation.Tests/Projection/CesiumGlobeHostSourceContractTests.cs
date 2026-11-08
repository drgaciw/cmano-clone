using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

/// <summary>
/// Source-level contract: CesiumGlobeHost documents env token names without embedding secrets.
/// </summary>
public sealed class CesiumGlobeHostSourceContractTests
{
    [Test]
    public void CesiumGlobeHost_mentions_env_vars_without_embedding_secrets()
    {
        var root = FindRepoRoot();
        Assert.That(root, Is.Not.Null, "repo root not found");

        var hostPath = Path.Combine(
            root!,
            "unity",
            "ProjectAegis",
            "Assets",
            "Scripts",
            "Runtime",
            "Cesium",
            "CesiumGlobeHost.cs");
        Assert.That(File.Exists(hostPath), Is.True, hostPath);

        var text = File.ReadAllText(hostPath);

        Assert.That(text, Does.Contain("CESIUM_ION_TOKEN"));
        Assert.That(text, Does.Contain("ProjectAegis_CesiumIonToken"));
        Assert.That(text, Does.Contain("IsIonTokenPresent"));
        Assert.That(text, Does.Contain("NEVER commit"));

        // No long opaque token-like literals (ion tokens are typically eyJ... JWT-ish).
        Assert.That(text, Does.Not.Match(@"eyJ[A-Za-z0-9_\-]{20,}"));
        Assert.That(text, Does.Not.Contain("defaultIonAccessToken = \""));
        Assert.That(text, Does.Not.Contain("ionAccessToken = \"ey"));
    }

    [Test]
    public void Scene_builder_never_writes_ion_token()
    {
        var root = FindRepoRoot();
        Assert.That(root, Is.Not.Null);

        var builderPath = Path.Combine(
            root!,
            "unity",
            "ProjectAegis",
            "Assets",
            "Editor",
            "DelegationSmokeSceneBuilder.cs");
        Assert.That(File.Exists(builderPath), Is.True, builderPath);

        var text = File.ReadAllText(builderPath);
        Assert.That(text, Does.Contain("BuildCesiumSpikeScene"));
        Assert.That(text, Does.Contain("do NOT set ionAccessToken"));
        Assert.That(text, Does.Contain("GlobeTileStreamingHost"));
        Assert.That(text, Does.Not.Contain("SetString(cesiumHost, \"ionAccessToken\""));
        Assert.That(text, Does.Not.Contain("SetString(host, \"ionAccessToken\""));
        Assert.That(text, Does.Not.Match(@"eyJ[A-Za-z0-9_\-]{20,}"));
    }

    [Test]
    public void Visual_gate_runbook_exists_and_forbids_committing_token()
    {
        var root = FindRepoRoot();
        Assert.That(root, Is.Not.Null);

        var runbook = Path.Combine(
            root!,
            "docs",
            "engineering",
            "cesium-ion-visual-gate-2026-08-01.md");
        Assert.That(File.Exists(runbook), Is.True, runbook);

        var text = File.ReadAllText(runbook);
        Assert.That(text, Does.Contain("NEVER commit"));
        Assert.That(text, Does.Contain("CESIUM_ION_TOKEN"));
        Assert.That(text, Does.Contain("production/qa"));
        Assert.That(text, Does.Not.Match(@"eyJ[A-Za-z0-9_\-]{20,}"));
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

            var kickoff = Path.Combine(
                dir.FullName,
                "production",
                "agentic",
                "sprint-ui-maturity-wave4-residual-kickoff-2026-08-01.md");
            if (File.Exists(kickoff))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        dir = new DirectoryInfo(Directory.GetCurrentDirectory());
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
