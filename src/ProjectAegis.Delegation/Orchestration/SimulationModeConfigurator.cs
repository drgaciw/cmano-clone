namespace ProjectAegis.Delegation.Orchestration;

using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Targets;
using ProjectAegis.Delegation.Traits;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;

public static class SimulationModeConfigurator
{
    public static void Apply(
        DelegationOrchestrator orchestrator,
        SimulationModeProfile mode,
        IReadOnlyList<ICommandableTarget> friendly,
        IReadOnlyList<ICommandableTarget> opposing,
        TraitVector defaultTraits,
        AutonomyLevel agentAutonomy = AutonomyLevel.FullAutonomous,
        string? scenarioPolicyId = null,
        PersonalityPreset? defaultPersonality = null)
    {
        orchestrator.ScenarioPolicy = ResolveScenarioPolicy(scenarioPolicyId);
        var traits = defaultPersonality?.Traits ?? defaultTraits;
        var attentionBudget = defaultPersonality != null
            ? PersonalityCatalog.ResolveAttentionBudget(defaultPersonality)
            : PersonalityCatalog.DefaultAttentionBudget;

        switch (mode.Kind)
        {
            case SimulationModeKind.Human:
                AssignHuman(friendly);
                AssignAgents(orchestrator, opposing, traits, agentAutonomy, "opp", attentionBudget);
                break;

            case SimulationModeKind.Mixed when orchestrator.ScenarioPolicy?.AllowDualSideControl == true:
                AssignHuman(friendly);
                AssignHuman(opposing);
                break;

            case SimulationModeKind.Mixed when mode.PlayerControlsFriendlySide:
                AssignHuman(friendly);
                AssignAgents(orchestrator, opposing, traits, agentAutonomy, "opp", attentionBudget);
                break;

            case SimulationModeKind.Mixed:
                AssignAgents(orchestrator, friendly, traits, agentAutonomy, "friendly", attentionBudget);
                AssignHuman(opposing);
                break;

            case SimulationModeKind.AgentVsAgent:
                AssignAgents(orchestrator, friendly, traits, agentAutonomy, "friendly", attentionBudget);
                AssignAgents(orchestrator, opposing, traits, agentAutonomy, "opp", attentionBudget);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(mode), mode.Kind, "Unknown simulation mode.");
        }

        if (mode.Kind == SimulationModeKind.AgentVsAgent)
        {
            orchestrator.BeginExecution();
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
        string idPrefix,
        double attentionBudget)
    {
        for (var i = 0; i < targets.Count; i++)
        {
            var agent = orchestrator.CreateAgent(
                new AgentId($"{idPrefix}-{i}"),
                traits,
                autonomy,
                attentionBudget);
            var isFriendly = idPrefix.StartsWith("friendly", StringComparison.Ordinal);
            orchestrator.AssignAgentToTarget(
                agent,
                targets[i],
                effectivePolicy: null,
                isFriendly: isFriendly,
                capturedAtSimTick: 0);
        }
    }

    private static ScenarioPolicyProfile? ResolveScenarioPolicy(string? scenarioPolicyId) =>
        string.IsNullOrWhiteSpace(scenarioPolicyId)
            ? null
            : ScenarioPolicyRepository.TryGet(scenarioPolicyId);
}
