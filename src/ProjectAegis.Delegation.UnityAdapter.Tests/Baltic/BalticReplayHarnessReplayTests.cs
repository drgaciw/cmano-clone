namespace ProjectAegis.Delegation.UnityAdapter.Tests.Baltic;

using ProjectAegis.Delegation.UnityAdapter.Baltic;
using NUnit.Framework;

[TestFixture]
public sealed class BalticReplayHarnessReplayTests
{
    [Test]
    public void Replay_scenario_emits_stable_checkpoints()
    {
        var a = BalticReplayHarness.Run(42, "baltic-patrol-replay", ticks: 4);
        var b = BalticReplayHarness.Run(42, "baltic-patrol-replay", ticks: 4);

        Assert.That(a.Checkpoints.Count, Is.GreaterThan(0));
        Assert.That(b.Checkpoints.Count, Is.EqualTo(a.Checkpoints.Count));
        for (var i = 0; i < a.Checkpoints.Count; i++)
        {
            Assert.That(b.Checkpoints[i].WorldHash, Is.EqualTo(a.Checkpoints[i].WorldHash));
            Assert.That(b.Checkpoints[i].SimTick, Is.EqualTo(a.Checkpoints[i].SimTick));
        }
    }

    [Test]
    public void Mission_scenario_appends_transition_and_event_rows()
    {
        var result = BalticReplayHarness.Run(42, "baltic-patrol-mission", ticks: 3);
        Assert.That(result.Fingerprint, Does.Contain("MissionTransition|"));
        Assert.That(result.Fingerprint, Does.Contain("EventFired|"));
    }
}