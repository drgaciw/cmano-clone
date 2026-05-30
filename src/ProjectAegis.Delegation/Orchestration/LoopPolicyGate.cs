namespace ProjectAegis.Delegation.Orchestration;

using ProjectAegis.Delegation.Core;
using ProjectAegis.Sim.Scenario;

public readonly record struct LoopPolicyVerdict(bool Allowed, string? DenialReason)
{
    public static LoopPolicyVerdict Allow() => new(true, null);

    public static LoopPolicyVerdict Deny(string reason) => new(false, reason);
}

/// <summary>Req 02 scenario-configurable loop rules (personality edit, player info).</summary>
public static class LoopPolicyGate
{
    public static PlayerInfoModel ResolvePlayerInfoModel(ScenarioPolicyProfile? policy) =>
        policy?.PlayerInfoModel ?? PlayerInfoModel.FullTransparency;

    public static LoopPolicyVerdict CanEditPersonality(
        ScenarioPolicyProfile? policy,
        SimulationPhase phase,
        AutonomyLevel autonomy)
    {
        var editPolicy = policy?.PersonalityEditPolicy ?? PersonalityEditPolicy.Anytime;

        return editPolicy switch
        {
            PersonalityEditPolicy.Anytime => LoopPolicyVerdict.Allow(),
            PersonalityEditPolicy.PlanningOnly when phase == SimulationPhase.Planning =>
                LoopPolicyVerdict.Allow(),
            PersonalityEditPolicy.PlanningOnly =>
                LoopPolicyVerdict.Deny("Personality locked after Begin Execution."),
            PersonalityEditPolicy.TieredRebrief when autonomy <= AutonomyLevel.Assisted =>
                LoopPolicyVerdict.Allow(),
            PersonalityEditPolicy.TieredRebrief =>
                LoopPolicyVerdict.Deny("Rebrief Agent required at Semi-Autonomous or higher."),
            _ => LoopPolicyVerdict.Allow(),
        };
    }

    public static LoopPolicyVerdict CanEditAutonomy(ScenarioPolicyProfile? policy, SimulationPhase phase)
    {
        _ = policy;
        _ = phase;
        return LoopPolicyVerdict.Allow();
    }
}
