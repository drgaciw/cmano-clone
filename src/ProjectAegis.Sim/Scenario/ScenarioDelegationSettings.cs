namespace ProjectAegis.Sim.Scenario;

/// <summary>Headless delegation harness knobs from scenario JSON.</summary>
public sealed record ScenarioDelegationSettings(bool UsePatrolCandidates = false)
{
    public static ScenarioDelegationSettings Default { get; } = new();
}