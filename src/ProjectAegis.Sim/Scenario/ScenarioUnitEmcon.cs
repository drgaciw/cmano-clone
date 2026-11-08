namespace ProjectAegis.Sim.Scenario;

using Policy;

/// <summary>Scenario-default radar EMCON for a unit (observer).</summary>
public sealed record ScenarioUnitEmcon(string UnitId, EmconState Radar);