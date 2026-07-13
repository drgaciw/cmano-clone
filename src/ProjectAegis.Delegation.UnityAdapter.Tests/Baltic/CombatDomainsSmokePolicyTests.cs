namespace ProjectAegis.Delegation.UnityAdapter.Tests.Baltic;

using ProjectAegis.Delegation.UnityAdapter.Baltic;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;

/// <summary>ADR-009 flag-on smoke fixture — isolated from ReplayGolden Baltic 6/6.</summary>
[TestFixture]
public sealed class CombatDomainsSmokePolicyTests
{
    private const string PolicyId = "combat-domains-smoke";
    private const int Seed = 42;
    private const int Ticks = 4;

    /// <summary>Pinned via harness seed=42 ticks=4; NOT Baltic ReplayGolden catalog.</summary>
    private const ulong PinnedWorldHash = 17144800277401907079UL;

    private const ulong PinnedDetectionWorldHash = 15600UL;

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
    public void Flag_on_smoke_replay_is_deterministic_with_empty_abort_set()
    {
        var a = BalticReplayHarness.Run(Seed, PolicyId, Ticks);
        var b = BalticReplayHarness.Run(Seed, PolicyId, Ticks);

        Assert.That(b.Fingerprint, Is.EqualTo(a.Fingerprint));
        Assert.That(b.WorldHash, Is.EqualTo(a.WorldHash));
        Assert.That(b.DetectionWorldHash, Is.EqualTo(a.DetectionWorldHash));
        Assert.That(a.Fingerprint, Does.Not.Contain("AIR_ASPECT_BLOCK"));
        Assert.That(a.Fingerprint, Does.Not.Contain("|Abort|"));
    }

    [Test]
    public void Flag_on_smoke_pins_world_and_detection_hash_separate_from_baltic_golden()
    {
        var result = BalticReplayHarness.Run(Seed, PolicyId, Ticks);

        Assert.That(result.WorldHash, Is.EqualTo(PinnedWorldHash));
        Assert.That(result.DetectionWorldHash, Is.EqualTo(PinnedDetectionWorldHash));
        Assert.That(result.ScenarioPolicyId, Is.EqualTo(PolicyId));
    }
}