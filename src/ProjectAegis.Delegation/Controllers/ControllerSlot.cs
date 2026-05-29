namespace ProjectAegis.Delegation.Controllers;

public sealed class ControllerSlot
{
    public IController? Active { get; private set; }

    public AgentController? SuspendedAgent { get; private set; }

    public void SetActive(IController controller)
    {
        Active = controller;
    }

    public void SuspendAgent(AgentController agent)
    {
        SuspendedAgent = agent;
        Active = null;
    }

    public void ResumeSuspendedAgent()
    {
        if (SuspendedAgent is null)
        {
            throw new InvalidOperationException("No suspended agent to resume.");
        }

        Active = SuspendedAgent;
        SuspendedAgent = null;
    }
}
