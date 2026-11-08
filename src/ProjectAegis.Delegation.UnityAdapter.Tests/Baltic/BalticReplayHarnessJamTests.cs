using ProjectAegis.Delegation.UnityAdapter.Baltic;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Baltic;

[TestFixture]
public sealed class BalticReplayHarnessJamTests
{
    [Test]
    public void Jammed_scenario_suppresses_contact_change()
    {
        var jammed = BalticReplayHarness.Run(42, "baltic-patrol-jammed", ticks: 2);
        var clear = BalticReplayHarness.Run(42, "baltic-patrol", ticks: 2);
        Assert.That(jammed.Fingerprint, Does.Not.Contain("ContactChange|"));
        Assert.That(clear.Fingerprint, Does.Contain("ContactChange|"));
    }

    [Test]
    public void Detection_world_hash_stable_for_jammed_run()
    {
        var a = BalticReplayHarness.Run(7, "baltic-patrol-jammed", ticks: 3);
        var b = BalticReplayHarness.Run(7, "baltic-patrol-jammed", ticks: 3);
        Assert.That(a.DetectionWorldHash, Is.EqualTo(b.DetectionWorldHash));
    }

    [Test]
    public void Eccm_jammed_fixture_not_in_replay_golden_regression_catalog()
    {
        var catalogPolicyIds = ReplayGoldenRegressionCatalog.All.Select(c => c.PolicyId).ToArray();

        Assert.That(catalogPolicyIds, Does.Not.Contain("baltic-patrol-jammed"));
        Assert.That(catalogPolicyIds, Has.Length.EqualTo(6));
    }

    [Test]
    public void Eccm_jammed_fixture_replay_is_deterministic()
    {
        var a = BalticReplayHarness.Run(42, "baltic-patrol-jammed", ticks: 4);
        var b = BalticReplayHarness.Run(42, "baltic-patrol-jammed", ticks: 4);

        Assert.That(b.FingerprintSha256, Is.EqualTo(a.FingerprintSha256));
        Assert.That(b.WorldHash, Is.EqualTo(a.WorldHash));
        Assert.That(b.DetectionWorldHash, Is.EqualTo(a.DetectionWorldHash));
    }
}