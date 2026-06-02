namespace ProjectAegis.Delegation.UnityAdapter.Bridge;

using ProjectAegis.Delegation.Decision;
using ProjectAegis.Sim.Sensors;

/// <summary>Bridge compat — prefer <see cref="OrderLogExtensions.AppendContactTransition"/>.</summary>
public static class ContactChangeLogExtensions
{
    public static void AppendContactTransition(this DecisionLog log, ContactTransition transition) =>
        OrderLogExtensions.AppendContactTransition(log, transition);
}