using ProjectAegis.Sim.Engage;
using NUnit.Framework;

namespace ProjectAegis.Sim.Tests.Engage;

public sealed class MagazineSalvoTests
{
    [Test]
    public void TryConsumeSalvo_drains_multiple_rounds()
    {
        var ledger = new MagazineLedger();
        ledger.SetRounds(1, 0, 2);
        Assert.That(ledger.TryConsumeSalvo(1, 0, 2), Is.True);
        Assert.That(ledger.GetRounds(1, 0), Is.EqualTo(0));
        Assert.That(ledger.TryConsumeSalvo(1, 0, 1), Is.False);
    }

    /// <summary>Wave 2 adversarial: partial salvo is atomic — no nibble (doc 16 LOG-01).</summary>
    [Test]
    public void TryConsumeSalvo_when_insufficient_rounds_returns_false_and_leaves_count_unchanged()
    {
        var ledger = new MagazineLedger();
        ledger.SetRounds(shooterUnitId: 7, mountId: 1, rounds: 2);

        Assert.That(ledger.TryConsumeSalvo(7, 1, salvoSize: 3), Is.False);
        Assert.That(ledger.GetRounds(7, 1), Is.EqualTo(2));

        Assert.That(ledger.TryConsume(shooterUnitId: 99, mountId: 0), Is.False);
        Assert.That(ledger.GetRounds(99, 0), Is.EqualTo(0));
    }
}