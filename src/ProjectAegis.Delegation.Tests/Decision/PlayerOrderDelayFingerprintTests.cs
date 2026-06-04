using ProjectAegis.Delegation.Comms;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Decision;

public sealed class PlayerOrderDelayFingerprintTests
{
    [Test]
    public void Fingerprint_includes_execute_sim_tick_for_delayed_orders()
    {
        var log = new DecisionLog();
        log.AppendPlayerOrder(new PlayerOrderRecord(
            0,
            2.0,
            2,
            new TargetId("u1"),
            OrderKind.Engage,
            ExecuteSimTick: CommsOrderDelay.ComputeExecuteSimTick(
                2,
                CommsState.Degraded,
                new ScenarioCommsDisplaySettings(2, 0.06f, 0.04f, degradedOrderDelayTicks: 2, degradedStaleThresholdDivisor: 2))));

        Assert.That(log.ComputeFingerprint(), Does.Contain("PlayerOrder|").And.Contain("|2|4|u1|"));
    }
}