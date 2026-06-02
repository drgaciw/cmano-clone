using ProjectAegis.Delegation.UnityAdapter.Baltic;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Baltic;

[TestFixture]
public sealed class BalticReplayHarnessWorldHashTests
{
    [Test]
    public void World_hash_is_non_zero_and_stable()
    {
        var a = BalticReplayHarness.Run(42, "baltic-patrol", ticks: 3);
        var b = BalticReplayHarness.Run(42, "baltic-patrol", ticks: 3);
        Assert.That(a.WorldHash, Is.Not.EqualTo(0UL));
        Assert.That(a.WorldHash, Is.EqualTo(b.WorldHash));
    }
}