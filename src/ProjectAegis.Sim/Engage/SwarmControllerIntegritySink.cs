namespace ProjectAegis.Sim.Engage;

using ProjectAegis.Sim.Swarm;

/// <summary>Forwards engagement integrity losses to <see cref="SwarmController"/> authorized API.</summary>
public sealed class SwarmControllerIntegritySink : ISwarmIntegrityDamageSink
{
    private readonly SwarmController _controller;

    public SwarmControllerIntegritySink(SwarmController controller)
    {
        _controller = controller ?? throw new ArgumentNullException(nameof(controller));
    }

    public bool TryApply(
        string targetUnitId,
        int dronesLost,
        ulong simTick,
        double simTime,
        string reasonCode) =>
        _controller.TryApplyIntegrityDamage(
            targetUnitId,
            dronesLost,
            simTick,
            simTime,
            reasonCode,
            out _);
}
