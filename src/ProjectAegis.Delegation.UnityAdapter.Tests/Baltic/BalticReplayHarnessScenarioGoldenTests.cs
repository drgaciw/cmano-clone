namespace ProjectAegis.Delegation.UnityAdapter.Tests.Baltic;

using ProjectAegis.Delegation.UnityAdapter.Baltic;
using NUnit.Framework;

[TestFixture]
public sealed class BalticReplayHarnessScenarioGoldenTests
{
    [TestCase("baltic-patrol-catalog", 4)]
    [TestCase("baltic-patrol-mission", 4)]
    [TestCase("baltic-patrol-replay", 4)]
    public void Scenario_fingerprint_and_world_hash_are_stable(string scenario, int ticks)
    {
        var a = BalticReplayHarness.Run(42, scenario, ticks);
        var b = BalticReplayHarness.Run(42, scenario, ticks);

        Assert.That(b.Fingerprint, Is.EqualTo(a.Fingerprint));
        Assert.That(b.WorldHash, Is.EqualTo(a.WorldHash));
        Assert.That(b.DetectionWorldHash, Is.EqualTo(a.DetectionWorldHash));
        Assert.That(a.Fingerprint, Is.Not.Empty);
    }
}