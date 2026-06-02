using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

public sealed class FuelStateProjectionTests
{
    [Test]
    public void FormatUnitFuelLine_escalates_with_sim_time()
    {
        Assert.That(FuelStateProjection.FormatUnitFuelLine("u1", 100), Does.Contain("NOMINAL"));
        Assert.That(FuelStateProjection.FormatUnitFuelLine("u1", 400), Does.Contain("JOKER"));
        Assert.That(FuelStateProjection.FormatUnitFuelLine("u1", 700), Does.Contain("BINGO"));
    }
}