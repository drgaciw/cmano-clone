using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Logistics;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Decision;

public sealed class FuelStateChangeOrderLogTests
{
    [Test]
    public void AppendFuelStateChange_appears_in_fingerprint()
    {
        var log = new DecisionLog();
        log.AppendFuelStateChange(new FuelStateChangeRecord(
            0,
            100,
            100,
            new TargetId("u1"),
            "NOMINAL",
            "JOKER",
            2500));

        var fingerprint = log.ComputeFingerprint();
        Assert.That(fingerprint, Does.Contain("FuelStateChange|"));
        Assert.That(fingerprint, Does.Contain("u1|NOMINAL|JOKER"));
    }
}