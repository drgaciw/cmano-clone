namespace ProjectAegis.Delegation.Orchestration;

using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Targets;

public sealed class OverrideService
{
    public void TakeDirectControl(ICommandableTarget target, HumanController human)
    {
        if (target.Slot.Active is AgentController agent)
        {
            target.Slot.SuspendAgent(agent);
        }

        target.Slot.SetActive(human);
    }

    public void ReleaseDirectControl(ICommandableTarget target)
    {
        target.Slot.ResumeSuspendedAgent();
    }
}
