namespace ProjectAegis.Sim.Scenario;

using ProjectAegis.Sim.Policy;

public static class ScenarioEmconResolver
{
    public static EmconState ResolveRadar(
        string unitId,
        IReadOnlyDictionary<string, EmconState>? unitRadarEmcon) =>
        unitRadarEmcon != null &&
        unitRadarEmcon.TryGetValue(unitId, out var state)
            ? state
            : EmconState.Active;
}