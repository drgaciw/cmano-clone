namespace ProjectAegis.Delegation.UnityAdapter.Tests.Bridge;

using NUnit.Framework;

/// <summary>
/// ASSET-006 (Approved) wire: production USS remains authority; Unity MessageLog panel
/// consumes tokenized category classes matching the Approved asset.
/// </summary>
[TestFixture]
public sealed class MessageLogAsset006WireTests
{
    [Test]
    public void Production_ASSET006_uss_exists_and_declares_category_rows()
    {
        var repoRoot = FindRepoRoot();
        Assert.That(repoRoot, Is.Not.Null);

        var path = Path.Combine(
            repoRoot!,
            "production",
            "assets",
            "c2",
            "MessageLogPanel.uss");
        Assert.That(File.Exists(path), Is.True, "Approved production ASSET-006 path missing");

        var uss = File.ReadAllText(path);
        Assert.That(uss, Does.Contain("ASSET-006"));
        Assert.That(uss, Does.Contain(".message-log-panel"));
        Assert.That(uss, Does.Contain(".message-log-row--kill"));
        Assert.That(uss, Does.Contain(".message-log-row--magazine"));
        Assert.That(uss, Does.Contain(".message-log-row--comms"));
        Assert.That(uss, Does.Contain(".message-log-row--contact"));
        Assert.That(uss, Does.Contain(".message-log-row--mission"));
        Assert.That(uss, Does.Contain("var(--log-kill)"));
    }

    [Test]
    public void Unity_MessageLog_uss_wires_ASSET006_tokens_and_preserves_live_chrome()
    {
        var repoRoot = FindRepoRoot();
        Assert.That(repoRoot, Is.Not.Null);

        var path = Path.Combine(
            repoRoot!,
            "unity",
            "ProjectAegis",
            "Assets",
            "UI",
            "MessageLog",
            "MessageLogPanel.uss");
        Assert.That(File.Exists(path), Is.True);

        var uss = File.ReadAllText(path);
        Assert.That(uss, Does.Contain("ASSET-006"));
        Assert.That(uss, Does.Contain("AegisTokens.uss"));
        Assert.That(uss, Does.Contain("var(--log-kill)"));
        Assert.That(uss, Does.Contain("var(--log-magazine)"));
        Assert.That(uss, Does.Contain("var(--log-comms)"));
        Assert.That(uss, Does.Contain("var(--log-contact)"));
        Assert.That(uss, Does.Contain("var(--log-mission)"));
        Assert.That(uss, Does.Contain(".message-log-title"));
        Assert.That(uss, Does.Contain(".message-log-onboarding-hint"));
        Assert.That(uss, Does.Contain(".message-log-list"));
        Assert.That(uss, Does.Contain("position: absolute"));
    }

    [Test]
    public void Unity_and_production_share_category_class_names()
    {
        var repoRoot = FindRepoRoot();
        Assert.That(repoRoot, Is.Not.Null);

        var prod = File.ReadAllText(Path.Combine(repoRoot!, "production", "assets", "c2", "MessageLogPanel.uss"));
        var unity = File.ReadAllText(Path.Combine(
            repoRoot!, "unity", "ProjectAegis", "Assets", "UI", "MessageLog", "MessageLogPanel.uss"));

        string[] classes =
        {
            ".message-log-row--kill",
            ".message-log-row--magazine",
            ".message-log-row--comms",
            ".message-log-row--contact",
            ".message-log-row--mission",
        };

        foreach (var c in classes)
        {
            Assert.That(prod, Does.Contain(c), $"production missing {c}");
            Assert.That(unity, Does.Contain(c), $"unity missing {c}");
        }
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
