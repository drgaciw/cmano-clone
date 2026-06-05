using ProjectAegis.Delegation.Comms;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Comms;

public sealed class CommsOrderDelayTests
{
    [Test]
    public void Degraded_comms_adds_configured_order_delay_ticks()
    {
        var display = new ScenarioCommsDisplaySettings(2, 0.06f, 0.04f, degradedOrderDelayTicks: 2, degradedStaleThresholdDivisor: 2);
        var execute = CommsOrderDelay.ComputeExecuteSimTick(5, CommsState.Degraded, display);
        Assert.That(execute, Is.EqualTo(7UL));
    }

    [Test]
    public void Nominal_comms_execute_tick_equals_queued_tick()
    {
        var display = new ScenarioCommsDisplaySettings(2, 0.06f, 0.04f, degradedOrderDelayTicks: 2, degradedStaleThresholdDivisor: 2);
        var execute = CommsOrderDelay.ComputeExecuteSimTick(5, CommsState.Nominal, display);
        Assert.That(execute, Is.EqualTo(5UL));
    }
}