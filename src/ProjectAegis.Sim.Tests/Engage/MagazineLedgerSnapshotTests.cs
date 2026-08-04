using ProjectAegis.Sim.Engage;
using Xunit;

namespace ProjectAegis.Sim.Tests.Engage;

public sealed class MagazineLedgerSnapshotTests
{
    [Fact]
    public void Snapshot_includes_remaining_and_capacity_after_consume()
    {
        var ledger = new MagazineLedger();
        ledger.EnsureInitialRounds(1, 10, 4);
        Assert.True(ledger.TryConsume(1, 10));

        var snap = ledger.Snapshot();
        Assert.Single(snap);
        Assert.Equal(1ul, snap[0].ShooterUnitId);
        Assert.Equal(10ul, snap[0].MountId);
        Assert.Equal(3, snap[0].Remaining);
        Assert.Equal(4, snap[0].Capacity);
        Assert.Equal(4, ledger.GetCapacity(1, 10));
    }

    [Fact]
    public void Snapshot_empty_when_unseeded()
    {
        var ledger = new MagazineLedger();
        Assert.Empty(ledger.Snapshot());
    }
}
