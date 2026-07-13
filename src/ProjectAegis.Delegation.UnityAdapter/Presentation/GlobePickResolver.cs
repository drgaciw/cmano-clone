using ProjectAegis.Delegation.Projection;

namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

/// <summary>
/// Pure globe-surface pick → unit id resolver (req 20 TR-c2-004 pick + TR-c2-005 selection).
/// Hosts feed lat/lon (or NDC) from Cesium/map raycasts; result applies to
/// <see cref="C2PresentationController"/> / <see cref="SelectionSet"/> only (ADR-010).
/// </summary>
public static class GlobePickResolver
{
    /// <summary>
    /// Default hit radius in NDC space (~2% of theater span) for "click near symbol" picking.
    /// </summary>
    public const float DefaultHitRadiusNdc = 0.02f;

    /// <summary>
    /// Resolve the nearest live friendly unit under a WGS84 pick, or null when none within
    /// <paramref name="hitRadiusNdc"/>. Destroyed / non-friendly symbols are never returned.
    /// When multiple symbols tie on distance, the earliest in <paramref name="symbols"/> wins
    /// (deterministic; mirrors map-picture build order).
    /// </summary>
    public static string? ResolveNearestFriendly(
        double latitude,
        double longitude,
        GeographicBounds theaterBounds,
        IReadOnlyList<MapSymbolEntry> symbols,
        float hitRadiusNdc = DefaultHitRadiusNdc)
    {
        var ndc = GlobeCoordinateMapper.ToNormalized(latitude, longitude, theaterBounds);
        if (ndc is null)
        {
            return null;
        }

        return ResolveNearestFriendlyNdc(ndc.Value.NormalizedX, ndc.Value.NormalizedY, symbols, hitRadiusNdc);
    }

    /// <summary>
    /// NDC-space variant for placeholder map hosts and tests that already work in 0..1 canvas space.
    /// </summary>
    public static string? ResolveNearestFriendlyNdc(
        float normalizedX,
        float normalizedY,
        IReadOnlyList<MapSymbolEntry> symbols,
        float hitRadiusNdc = DefaultHitRadiusNdc)
    {
        if (symbols == null || symbols.Count == 0 || hitRadiusNdc < 0f)
        {
            return null;
        }

        string? bestId = null;
        var bestDistSq = float.PositiveInfinity;
        var radiusSq = hitRadiusNdc * hitRadiusNdc;

        foreach (var symbol in symbols)
        {
            if (symbol.IsDestroyed)
            {
                continue;
            }

            if (!string.Equals(symbol.Affiliation, "Friendly", StringComparison.Ordinal))
            {
                continue;
            }

            var dx = symbol.NormalizedX - normalizedX;
            var dy = symbol.NormalizedY - normalizedY;
            var distSq = dx * dx + dy * dy;
            if (distSq > radiusSq || distSq >= bestDistSq)
            {
                // Strict < on distance; ties keep the first (input order).
                continue;
            }

            bestDistSq = distSq;
            bestId = symbol.SymbolId;
        }

        return bestId;
    }

    /// <summary>
    /// Apply a single-select globe pick onto the shared presentation controller (replace selection).
    /// Returns true when a unit was selected; false when the pick missed (selection unchanged).
    /// </summary>
    public static bool ApplyPickReplace(
        C2PresentationController controller,
        double latitude,
        double longitude,
        GeographicBounds theaterBounds,
        IReadOnlyList<MapSymbolEntry> symbols,
        float hitRadiusNdc = DefaultHitRadiusNdc)
    {
        if (controller is null)
        {
            throw new ArgumentNullException(nameof(controller));
        }

        var unitId = ResolveNearestFriendly(latitude, longitude, theaterBounds, symbols, hitRadiusNdc);
        if (unitId is null)
        {
            return false;
        }

        controller.SelectFriendlyUnit(unitId);
        return true;
    }

    /// <summary>
    /// Apply a shift/ctrl-style toggle pick onto the shared presentation controller.
    /// Returns true when a unit was under the pick (toggled on or off); false on miss.
    /// </summary>
    public static bool ApplyPickToggle(
        C2PresentationController controller,
        double latitude,
        double longitude,
        GeographicBounds theaterBounds,
        IReadOnlyList<MapSymbolEntry> symbols,
        float hitRadiusNdc = DefaultHitRadiusNdc)
    {
        if (controller is null)
        {
            throw new ArgumentNullException(nameof(controller));
        }

        var unitId = ResolveNearestFriendly(latitude, longitude, theaterBounds, symbols, hitRadiusNdc);
        if (unitId is null)
        {
            return false;
        }

        controller.ToggleFriendlyUnit(unitId);
        return true;
    }
}
