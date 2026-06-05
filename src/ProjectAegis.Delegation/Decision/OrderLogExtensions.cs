namespace ProjectAegis.Delegation.Decision;

using ProjectAegis.Sim.Sensors;

/// <summary>C1 append helpers for sim-emitted rows.</summary>
public static class OrderLogExtensions
{
    public static void AppendContactTransition(this IOrderLog log, ContactTransition transition) =>
        log.Append(OrderLogEntryFactories.FromContactTransition(transition));
}