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
}