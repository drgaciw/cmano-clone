using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Decision;

public sealed class FuelBurnOrderLogTests
{
    [Test]
    public void AppendFuelBurn_appears_in_fingerprint()
    {
        var log = new DecisionLog();
        log.AppendFuelBurn(new FuelBurnRecord(0, 1, 1, new TargetId("u1"), -80, 9920));

        var fingerprint = log.ComputeFingerprint();
        Assert.That(fingerprint, Does.Contain("FuelBurn|"));
        Assert.That(fingerprint, Does.Contain("u1|-80"));
    }
}