using ProjectAegis.Sim.Engage;
using NUnit.Framework;
using ProjectAegis.Sim.Tests;

namespace ProjectAegis.Sim.Tests.Engage;

public sealed class MagazineLedgerSnapshotTests
{
    [Test]
    public void Snapshot_includes_remaining_and_capacity_after_consume()
    {
        var ledger = new MagazineLedger();
        ledger.EnsureInitialRounds(1, 10, 4);
        Assert.That(ledger.TryConsume(1, 10), Is.True);

        var snap = ledger.Snapshot();
        AssertEx.Single(snap);
        Assert.That(snap[0].ShooterUnitId, Is.EqualTo(1ul));
        Assert.That(snap[0].MountId, Is.EqualTo(10ul));
        Assert.That(snap[0].Remaining, Is.EqualTo(3));
        Assert.That(snap[0].Capacity, Is.EqualTo(4));
        Assert.That(ledger.GetCapacity(1, 10), Is.EqualTo(4));
    }

    [Test]
    public void Snapshot_empty_when_unseeded()
    {
        var ledger = new MagazineLedger();
        Assert.That(ledger.Snapshot(), Is.Empty);
    }
}
