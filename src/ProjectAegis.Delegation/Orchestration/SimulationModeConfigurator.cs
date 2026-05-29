namespace ProjectAegis.Delegation.Orchestration;

using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Targets;
using ProjectAegis.Delegation.Traits;

public static class SimulationModeConfigurator
{
    public static void Apply(
        DelegationOrchestrator orchestrator,
        SimulationModeProfile mode,
        IReadOnlyList<ICommandableTarget> friendly,
        IReadOnlyList<ICommandableTarget> opposing,
        TraitVector defaultTraits,
        AutonomyLevel agentAutonomy = AutonomyLevel.FullAutonomous)
    {
        switch (mode.Kind)
        {
            case SimulationModeKind.Human:
                AssignHuman(friendly);
                AssignAgents(orchestrator, opposing, defaultTraits, agentAutonomy, "opp");
                break;

            case SimulationModeKind.Mixed when mode.PlayerControlsFriendlySide:
                AssignHuman(friendly);
                AssignAgents(orchestrator, opposing, defaultTraits, agentAutonomy, "opp");
                break;

            case SimulationModeKind.Mixed:
                AssignAgents(orchestrator, friendly, defaultTraits, agentAutonomy, "friendly");
                AssignHuman(opposing);
                break;

            case SimulationModeKind.AgentVsAgent:
                AssignAgents(orchestrator, friendly, defaultTraits, agentAutonomy, "friendly");
                AssignAgents(orchestrator, opposing, defaultTraits, agentAutonomy, "opp");
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(mode), mode.Kind, "Unknown simulation mode.");
        }
    }

    private static void AssignHuman(IEnumerable<ICommandableTarget> targets)
    {
        foreach (var target in targets)
        {
            target.Slot.SetActive(new HumanController());
        }
    }

    private static void AssignAgents(
        DelegationOrchestrator orchestrator,
        IReadOnlyList<ICommandableTarget> targets,
        TraitVector traits,
        AutonomyLevel autonomy,
        string idPrefix)
    {
        for (var i = 0; i < targets.Count; i++)
        {
            var agent = orchestrator.CreateAgent(
                new AgentId($"{idPrefix}-{i}"),
                traits,
                autonomy);
            targets[i].Slot.SetActive(agent);
        }
    }
}
