using ProjectAegis.Sim.Engage;
using Xunit;

namespace ProjectAegis.Sim.Tests.Engage;

public sealed class MagazineSalvoTests
{
    [Fact]
    public void TryConsumeSalvo_drains_multiple_rounds()
    {
        var ledger = new MagazineLedger();
        ledger.SetRounds(1, 0, 2);
        Assert.True(ledger.TryConsumeSalvo(1, 0, 2));
        Assert.Equal(0, ledger.GetRounds(1, 0));
        Assert.False(ledger.TryConsumeSalvo(1, 0, 1));
    }

    /// <summary>Wave 2 adversarial: partial salvo is atomic — no nibble (doc 16 LOG-01).</summary>
    [Fact]
    public void TryConsumeSalvo_when_insufficient_rounds_returns_false_and_leaves_count_unchanged()
    {
        var ledger = new MagazineLedger();
        ledger.SetRounds(shooterUnitId: 7, mountId: 1, rounds: 2);

        Assert.False(ledger.TryConsumeSalvo(7, 1, salvoSize: 3));
        Assert.Equal(2, ledger.GetRounds(7, 1));

        Assert.False(ledger.TryConsume(shooterUnitId: 99, mountId: 0));
        Assert.Equal(0, ledger.GetRounds(99, 0));
    }
}