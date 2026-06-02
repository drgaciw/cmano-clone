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
}