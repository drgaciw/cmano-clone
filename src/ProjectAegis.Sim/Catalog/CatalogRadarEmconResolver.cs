namespace ProjectAegis.Sim.Catalog;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Sim.Policy;

/// <summary>
/// Req-21 Phase B: resolves radar EMCON from scenario overrides with catalog posture fallback.
/// Additive-only — scenario dict wins; absent catalog rows default to <see cref="EmconState.Active"/>.
/// </summary>
public static class CatalogRadarEmconResolver
{
    public const string DefaultCondition = "free";
    public const string DefaultEmitterId = "radar-1";

    public static EmconState ResolveRadar(
        string unitId,
        IReadOnlyDictionary<string, EmconState>? unitRadarEmcon,
        ICatalogReader? catalog = null,
        string condition = DefaultCondition,
        string emitterId = DefaultEmitterId)
    {
        if (unitRadarEmcon != null && unitRadarEmcon.TryGetValue(unitId, out var scenarioState))
        {
            return scenarioState;
        }

        if (catalog != null && catalog.TryGetEmcon(unitId, condition, emitterId, out var emcon))
        {
            return MapPosture(emcon.Posture);
        }

        return EmconState.Active;
    }

    public static EmconState MapPosture(string posture) =>
        // PlatformWorkbookValidator.AllowedEmconPostures validates off/standby/active case-insensitively
        // (StringComparer.OrdinalIgnoreCase), so any casing accepted there must map correctly here too —
        // otherwise a validly-authored "Off"/"Standby" posture silently (and dangerously) resolves to
        // EmconState.Active, defeating EMCON discipline for detection and the engage RadarEmconActive gate.
        posture?.Trim().ToLowerInvariant() switch
        {
            "off" => EmconState.Off,
            "standby" => EmconState.Passive,
            "active" => EmconState.Active,
            _ => EmconState.Active,
        };
}