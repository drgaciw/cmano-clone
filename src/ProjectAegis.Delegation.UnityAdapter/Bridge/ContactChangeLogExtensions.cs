namespace ProjectAegis.Delegation.UnityAdapter.Bridge;

using ProjectAegis.Delegation.Decision;
using ProjectAegis.Sim.Sensors;

public static class ContactChangeLogExtensions
{
    public static void AppendContactTransition(this DecisionLog log, ContactTransition transition)
    {
        log.AppendContactChange(new ContactChangeRecord(
            SequenceId: 0,
            transition.SimTime,
            transition.SimTick,
            transition.ObserverId,
            transition.ContactId,
            transition.TargetId,
            transition.PreviousState.ToString(),
            transition.NewState.ToString()));
    }
}