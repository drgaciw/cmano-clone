namespace ProjectAegis.Sim.Scenario;

using ProjectAegis.Sim.Policy;

public static class ScenarioPolicyCatalog
{
    /// <summary>Default training scenario — weapons free both sides.</summary>
    public static ScenarioPolicyProfile BalticPatrol { get; } = new(
        EffectivePolicy.DefaultFree,
        EffectivePolicy.DefaultFree);

    /// <summary>Opposing force holds fire (escalation training).</summary>
    public static ScenarioPolicyProfile BalticPatrolOpposingHoldFire { get; } = new(
        EffectivePolicy.DefaultFree,
        new EffectivePolicy(RoeLevel.HoldFire));

    /// <summary>Both sides weapons tight (restricted engage).</summary>
    public static ScenarioPolicyProfile RestrictedEngagement { get; } = new(
        new EffectivePolicy(RoeLevel.WeaponsTight),
        new EffectivePolicy(RoeLevel.WeaponsTight));

    public static ScenarioPolicyProfile? TryGet(string scenarioId) =>
        ScenarioPolicyRepository.TryGet(scenarioId);

    internal static ScenarioPolicyProfile? TryGetBuiltIn(string scenarioId) =>
        scenarioId switch
        {
            "baltic-patrol" => BalticPatrol,
            "baltic-patrol-opp-hold-fire" => BalticPatrolOpposingHoldFire,
            "restricted-engagement" => RestrictedEngagement,
            _ => null,
        };
}
