namespace ProjectAegis.Sim.Scenario;

using ProjectAegis.Sim.Engage;

/// <summary>Deterministic TL / BLACK_PROJECT_MODE checks before engage resolution (req 10).</summary>
public static class SpeculativeEngageGate
{
    public static EngagementAbortReason? Evaluate(
        ScenarioSpeculativeSettings settings,
        in EngageContext context)
    {
        if (context.WeaponTechnologyLevel > settings.MaxTechnologyLevel)
        {
            return EngagementAbortReason.TechnologyLevelExceeded;
        }

        if (context.WeaponRequiresBlackProject && !settings.BlackProjectMode)
        {
            return EngagementAbortReason.BlackProjectRequired;
        }

        return null;
    }
}