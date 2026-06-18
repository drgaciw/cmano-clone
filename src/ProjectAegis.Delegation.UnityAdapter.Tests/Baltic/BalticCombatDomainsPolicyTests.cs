namespace ProjectAegis.Delegation.UnityAdapter.Tests.Baltic;

using ProjectAegis.Delegation.UnityAdapter.Baltic;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;

/// <summary>
/// ADR-009 flag-on Baltic golden fixture — isolated from ReplayGolden 6/6 and combat-domains-smoke pin.
/// Production <c>baltic-patrol</c> remains <c>combatDomainsEnabled=false</c> until post-merge flip.
/// </summary>
[TestFixture]
public sealed class BalticCombatDomainsPolicyTests
{
    private const string PolicyId = "baltic-patrol-combat-domains";
    private const string SmokePolicyId = "combat-domains-smoke";
    private const int Seed = 42;
    private const int Ticks = 4;

    /// <summary>Pinned via harness seed=42 ticks=4; distinct policy from smoke and ReplayGolden catalog.</summary>
    private const ulong PinnedWorldHash = 17144800277401907079UL;

    private const ulong PinnedDetectionWorldHash = 15600UL;

    private const string PinnedFingerprintSha256 =
        "080a4cbf18f620043a4e6401ac1f60749e9604760b91d2adfc770de816649917";

    private const string GoldenFile = "replay-golden-baltic-combat-domains-2026-06-18.txt";

    [Test]
    public void Policy_loads_combatDomainsEnabled_true()
    {
        ScenarioPolicyRepository.EnsureDefaultJsonLoaded();
        var profile = ScenarioPolicyRepository.TryGet(PolicyId);

        Assert.That(profile, Is.Not.Null);
        Assert.That(profile!.EngageDefaults, Is.Not.Null);
        Assert.That(profile.EngageDefaults!.CombatDomainsEnabled, Is.True);
    }

    [Test]
    public void Production_baltic_patrol_remains_combatDomainsEnabled_false()
    {
        ScenarioPolicyRepository.EnsureDefaultJsonLoaded();
        var profile = ScenarioPolicyRepository.TryGet("baltic-patrol");

        Assert.That(profile, Is.Not.Null);
        Assert.That(profile!.EngageDefaults, Is.Not.Null);
        Assert.That(profile.EngageDefaults!.CombatDomainsEnabled, Is.False);
    }

    [Test]
    public void Policy_is_not_in_replay_golden_regression_catalog()
    {
        var catalogPolicyIds = ReplayGoldenRegressionCatalog.All.Select(c => c.PolicyId).ToArray();

        Assert.That(catalogPolicyIds, Does.Not.Contain(PolicyId));
        Assert.That(catalogPolicyIds, Has.Length.EqualTo(6));
    }

    [Test]
    public void Flag_on_baltic_replay_is_deterministic_with_allow_path_no_aborts()
    {
        var a = BalticReplayHarness.Run(Seed, PolicyId, Ticks);
        var b = BalticReplayHarness.Run(Seed, PolicyId, Ticks);

        Assert.That(b.Fingerprint, Is.EqualTo(a.Fingerprint));
        Assert.That(b.WorldHash, Is.EqualTo(a.WorldHash));
        Assert.That(b.DetectionWorldHash, Is.EqualTo(a.DetectionWorldHash));
        Assert.That(a.Fingerprint, Does.Not.Contain("AIR_ASPECT_BLOCK"));
        Assert.That(a.Fingerprint, Does.Not.Contain("SURFACE_ASPECT_BLOCK"));
        Assert.That(a.Fingerprint, Does.Not.Contain("SUBSURFACE_ASPECT_BLOCK"));
        Assert.That(a.Fingerprint, Does.Not.Contain("|Abort|"));
        Assert.That(a.Fingerprint, Does.Contain("EngagementOutcome|"));
        Assert.That(a.Fingerprint, Does.Contain("|Kill|"));
    }

    [Test]
    public void Flag_on_baltic_pins_world_and_detection_hash_separate_from_catalog()
    {
        var result = BalticReplayHarness.Run(Seed, PolicyId, Ticks);

        Assert.That(result.WorldHash, Is.EqualTo(PinnedWorldHash));
        Assert.That(result.DetectionWorldHash, Is.EqualTo(PinnedDetectionWorldHash));
        Assert.That(result.FingerprintSha256, Is.EqualTo(PinnedFingerprintSha256));
        Assert.That(result.ScenarioPolicyId, Is.EqualTo(PolicyId));
    }

    [Test]
    public void Stored_regression_golden_matches_harness_output()
    {
        var goldenPath = ReplayGoldenAssertions.ResolveRegressionGoldenPath(GoldenFile);
        Assert.That(File.Exists(goldenPath), Is.True, $"Missing golden file: {GoldenFile}");

        var golden = File.ReadAllLines(goldenPath);
        var result = BalticReplayHarness.Run(Seed, PolicyId, Ticks);

        ReplayGoldenAssertions.AssertPinnedHashes(result, golden);
    }

    [Test]
    public void Baltic_combat_domains_fixture_is_isolated_from_smoke_policy_id()
    {
        Assert.That(PolicyId, Is.Not.EqualTo(SmokePolicyId));

        ScenarioPolicyRepository.EnsureDefaultJsonLoaded();
        var baltic = ScenarioPolicyRepository.TryGet(PolicyId);
        var smoke = ScenarioPolicyRepository.TryGet(SmokePolicyId);

        Assert.That(baltic, Is.Not.Null);
        Assert.That(smoke, Is.Not.Null);
        Assert.That(baltic!.EngageDefaults!.CombatDomainsEnabled, Is.True);
        Assert.That(smoke!.EngageDefaults!.CombatDomainsEnabled, Is.True);
        Assert.That(baltic.DetectionTrials[0].JamStrength, Is.EqualTo(0.0));
        Assert.That(baltic.DetectionTrials[0].TargetId, Is.EqualTo("hostile-1"));
        Assert.That(smoke.DetectionTrials[0].TargetId, Is.EqualTo("hostile-1"));
    }
}