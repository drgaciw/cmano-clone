namespace ProjectAegis.Delegation.Orchestration;

using Core;
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

    /// <summary>
    /// W2-DEL-02 explicit Rebrief Agent action. Under <see cref="PersonalityEditPolicy.TieredRebrief"/> this is
    /// the sanctioned path past the Semi-Autonomous+ hot-edit denial; <see cref="PersonalityEditPolicy.PlanningOnly"/>
    /// still locks personalities once execution begins. Rebrief sim-time cost remains a future policy field.
    /// </summary>
    public static LoopPolicyVerdict CanRebriefAgent(ScenarioPolicyProfile? policy, SimulationPhase phase)
    {
        var editPolicy = policy?.PersonalityEditPolicy ?? PersonalityEditPolicy.Anytime;

        return editPolicy == PersonalityEditPolicy.PlanningOnly && phase != SimulationPhase.Planning
            ? LoopPolicyVerdict.Deny("Personality locked after Begin Execution.")
            : LoopPolicyVerdict.Allow();
    }

    public static LoopPolicyVerdict CanEditAutonomy(ScenarioPolicyProfile? policy, SimulationPhase phase)
    {
        _ = policy;
        _ = phase;
        return LoopPolicyVerdict.Allow();
    }
}
