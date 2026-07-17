namespace ProjectAegis.Delegation.UnityAdapter.Tests.Bridge;

using NUnit.Framework;

/// <summary>
/// ASSET-021 (Approved) wire: production CombatDomainsHotTick USS is authority;
/// Unity C2 panel mirrors class names and tokenized engaged/degraded states.
/// </summary>
[TestFixture]
public sealed class CombatDomainsAsset021WireTests
{
    [Test]
    public void Production_ASSET021_uss_exists_and_declares_hot_tick_classes()
    {
        var repoRoot = FindRepoRoot();
        Assert.That(repoRoot, Is.Not.Null);

        var path = Path.Combine(
            repoRoot!,
            "production",
            "assets",
            "baltic",
            "CombatDomainsHotTick.uss");
        Assert.That(File.Exists(path), Is.True, "Approved production ASSET-021 path missing");

        var uss = File.ReadAllText(path);
        Assert.That(uss, Does.Contain("ASSET-021"));
        Assert.That(uss, Does.Contain(".combat-domains-hot-tick"));
        Assert.That(uss, Does.Contain(".combat-domains-hot-tick__title"));
        Assert.That(uss, Does.Contain(".combat-domains-hot-tick__state--engaged"));
        Assert.That(uss, Does.Contain(".combat-domains-hot-tick__state--degraded"));
    }

    [Test]
    public void Unity_CombatDomains_uss_wires_tokens_and_preserves_layout()
    {
        var repoRoot = FindRepoRoot();
        Assert.That(repoRoot, Is.Not.Null);

        var path = Path.Combine(
            repoRoot!,
            "unity",
            "ProjectAegis",
            "Assets",
            "UI",
            "CombatDomains",
            "CombatDomainsHotTick.uss");
        Assert.That(File.Exists(path), Is.True);

        var uss = File.ReadAllText(path);
        Assert.That(uss, Does.Contain("ASSET-021"));
        Assert.That(uss, Does.Contain("AegisTokens.uss"));
        Assert.That(uss, Does.Contain("var(--log-kill)"));
        Assert.That(uss, Does.Contain("var(--comms-degraded)"));
        Assert.That(uss, Does.Contain("position: absolute"));
        Assert.That(uss, Does.Contain(".combat-domains-hot-tick__state--engaged"));
    }

    [Test]
    public void Unity_uxml_declares_domain_state_labels()
    {
        var repoRoot = FindRepoRoot();
        Assert.That(repoRoot, Is.Not.Null);

        var path = Path.Combine(
            repoRoot!,
            "unity",
            "ProjectAegis",
            "Assets",
            "UI",
            "CombatDomains",
            "CombatDomainsHotTick.uxml");
        Assert.That(File.Exists(path), Is.True);

        var uxml = File.ReadAllText(path);
        Assert.That(uxml, Does.Contain("name=\"combat-domains-hot-tick-root\""));
        Assert.That(uxml, Does.Contain("name=\"domain-air-state\""));
        Assert.That(uxml, Does.Contain("name=\"domain-surface-state\""));
        Assert.That(uxml, Does.Contain("name=\"domain-sub-state\""));
        Assert.That(uxml, Does.Contain("COMBAT DOMAINS"));
    }

    [Test]
    public void Unity_and_production_share_hot_tick_class_names()
    {
        var repoRoot = FindRepoRoot();
        Assert.That(repoRoot, Is.Not.Null);

        var prod = File.ReadAllText(Path.Combine(
            repoRoot!, "production", "assets", "baltic", "CombatDomainsHotTick.uss"));
        var unity = File.ReadAllText(Path.Combine(
            repoRoot!, "unity", "ProjectAegis", "Assets", "UI", "CombatDomains", "CombatDomainsHotTick.uss"));

        string[] classes =
        {
            ".combat-domains-hot-tick",
            ".combat-domains-hot-tick__title",
            ".combat-domains-hot-tick__row",
            ".combat-domains-hot-tick__domain",
            ".combat-domains-hot-tick__state",
            ".combat-domains-hot-tick__state--engaged",
            ".combat-domains-hot-tick__state--degraded",
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
