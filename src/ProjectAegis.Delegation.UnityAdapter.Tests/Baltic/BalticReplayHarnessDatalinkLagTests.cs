namespace ProjectAegis.Delegation.UnityAdapter.Tests.Baltic;

using ProjectAegis.Delegation.UnityAdapter.Baltic;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;

/// <summary>
/// TR-sensor-004 share-lag fixture — isolated from ReplayGolden 6/6 catalog.
/// </summary>
[TestFixture]
public sealed class BalticReplayHarnessDatalinkLagTests
{
    private const string PolicyId = "baltic-patrol-datalink-lag";
    private const int Seed = 42;
    private const int Ticks = 4;

    [Test]
    public void Policy_loads_shareLagTicks_greater_than_zero()
    {
        ScenarioPolicyRepository.EnsureDefaultJsonLoaded();
        var profile = ScenarioPolicyRepository.TryGet(PolicyId);

        Assert.That(profile, Is.Not.Null);
        Assert.That(profile!.DatalinkDoctrine.ShareLagTicks, Is.EqualTo(2));
        Assert.That(profile.DatalinkDoctrine.IsSharingEnabled, Is.True);
    }

    [Test]
    public void Policy_is_not_in_replay_golden_regression_catalog()
    {
        var catalogPolicyIds = ReplayGoldenRegressionCatalog.All.Select(c => c.PolicyId).ToArray();

        Assert.That(catalogPolicyIds, Does.Not.Contain(PolicyId));
        Assert.That(catalogPolicyIds, Has.Length.EqualTo(6));
    }

    [Test]
    public void Lag_fixture_replay_is_deterministic()
    {
        var a = BalticReplayHarness.Run(Seed, PolicyId, Ticks);
        var b = BalticReplayHarness.Run(Seed, PolicyId, Ticks);

        Assert.That(b.Fingerprint, Is.EqualTo(a.Fingerprint));
        Assert.That(b.WorldHash, Is.EqualTo(a.WorldHash));
        Assert.That(b.DetectionWorldHash, Is.EqualTo(a.DetectionWorldHash));
        Assert.That(b.FingerprintSha256, Is.EqualTo(a.FingerprintSha256));
    }

    [Test]
    public void Lag_fixture_defers_datalink_contact_change_until_after_detection_tick()
    {
        var result = BalticReplayHarness.Run(Seed, PolicyId, Ticks);

        Assert.That(result.Fingerprint, Does.Contain("ContactChange|"));
        Assert.That(result.Fingerprint, Does.Contain("|u2|dl-hostile-1|hostile-1|Unknown|Detected"));
        Assert.That(result.Fingerprint, Does.Not.Contain("|u2|dl-hostile-1|hostile-1|Unknown|Detected|1|"));
    }
}