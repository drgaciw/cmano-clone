using ProjectAegis.Sim.Logistics;
using Xunit;

namespace ProjectAegis.Sim.Tests.Logistics;

public sealed class FuelLedgerTests
{
    [Fact]
    public void AdvanceTick_decreases_remaining_deterministically()
    {
        var ledger = new FuelLedger(10_000, burnRateKgPerSecond: 80);
        ledger.EnsureUnit("u1");

        var (d1, r1) = ledger.AdvanceTick("u1", 1.0);
        var (d2, r2) = ledger.AdvanceTick("u1", 1.0);

        Assert.Equal(-80, d1, 3);
        Assert.Equal(9920, r1, 3);
        Assert.Equal(-80, d2, 3);
        Assert.Equal(9840, r2, 3);
    }

    [Fact]
    public void ResolveBand_crosses_joker_then_bingo()
    {
        var ledger = new FuelLedger(10_000, 80);
        ledger.EnsureUnit("u1");
        Assert.Equal("NOMINAL", ledger.ResolveBand("u1", 0.25, 0.10));

        for (var i = 0; i < 94; i++)
        {
            ledger.AdvanceTick("u1", 1.0);
        }

        Assert.Equal("JOKER", ledger.ResolveBand("u1", 0.25, 0.10));

        for (var i = 0; i < 19; i++)
        {
            ledger.AdvanceTick("u1", 1.0);
        }

        Assert.Equal("BINGO", ledger.ResolveBand("u1", 0.25, 0.10));
    }

    [Fact]
    public void GetRemainingKg_returns_capacity_for_unknown_unit()
    {
        var ledger = new FuelLedger(10_000, burnRateKgPerSecond: 80);

        Assert.Equal(10_000, ledger.GetRemainingKg("unknown"), 3);
    }

    [Fact]
    public void EnsureUnit_is_idempotent()
    {
        var ledger = new FuelLedger(10_000, burnRateKgPerSecond: 80);
        ledger.EnsureUnit("u1");
        ledger.AdvanceTick("u1", 1.0);
        ledger.EnsureUnit("u1");

        Assert.Equal(9920, ledger.GetRemainingKg("u1"), 3);
    }
}