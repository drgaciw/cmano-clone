namespace ProjectAegis.Sim.Scenario;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Sim.Catalog;
using ProjectAegis.Sim.Policy;

public static class ScenarioEmconResolver
{
    public static EmconState ResolveRadar(
        string unitId,
        IReadOnlyDictionary<string, EmconState>? unitRadarEmcon,
        ICatalogReader? catalog = null,
        string condition = CatalogRadarEmconResolver.DefaultCondition,
        string emitterId = CatalogRadarEmconResolver.DefaultEmitterId) =>
        CatalogRadarEmconResolver.ResolveRadar(unitId, unitRadarEmcon, catalog, condition, emitterId);
}