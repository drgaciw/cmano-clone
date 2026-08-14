namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Pure geometry for map-canvas envelope rings and datalink edges (DRG-160 / S121-DRAW).
/// Converts projected overlay entries + symbol positions into normalized canvas shapes.
/// </summary>
public static class MapCanvasOverlayGeometry
{
    /// <summary>Placeholder theater width (nm) for normalized radius on the Baltic canvas.</summary>
    public const double DefaultTheaterWidthNm = 800.0;

    public const string RingStyleSensor = "map-overlay-ring--sensor";
    public const string RingStyleWeapon = "map-overlay-ring--weapon";

    public const string EdgeStyleUp = "map-overlay-edge--up";
    public const string EdgeStyleDegraded = "map-overlay-edge--degraded";
    public const string EdgeStyleDown = "map-overlay-edge--down";

    /// <summary>Converts nautical-mile range to normalized canvas radius (0–1 relative to canvas width).</summary>
    public static float NmToNormalizedRadius(double rangeNm, double theaterWidthNm = DefaultTheaterWidthNm)
    {
        if (rangeNm <= 0 || theaterWidthNm <= 0)
        {
            return 0f;
        }

        var radius = rangeNm / theaterWidthNm;
        return radius > 1f ? 1f : (float)radius;
    }

    /// <summary>
    /// Builds a unit-id position index from live (non-ghost) map symbol rows.
    /// Ghost rows (<c>ghost:</c> prefix) are excluded; first live row wins per id.
    /// </summary>
    public static IReadOnlyDictionary<string, (float X, float Y)> BuildUnitPositionIndex(
        IReadOnlyList<MapSymbolDisplayRow>? symbols)
    {
        if (symbols is null || symbols.Count == 0)
        {
            return EmptyPositions;
        }

        var positions = new Dictionary<string, (float X, float Y)>(StringComparer.Ordinal);
        foreach (var row in symbols)
        {
            if (row is null || row.IsGhost || string.IsNullOrWhiteSpace(row.SymbolId))
            {
                continue;
            }

            if (row.SymbolId.StartsWith("ghost:", StringComparison.Ordinal))
            {
                continue;
            }

            positions.TryAdd(row.SymbolId, (row.NormalizedX, row.NormalizedY));
        }

        return positions;
    }

    /// <summary>Projects envelope rings onto normalized canvas shapes at unit positions.</summary>
    public static IReadOnlyList<MapCanvasRingShape> ProjectRings(
        IReadOnlyList<EnvelopeRingEntry>? rings,
        IReadOnlyDictionary<string, (float X, float Y)> positions,
        double theaterWidthNm = DefaultTheaterWidthNm)
    {
        if (rings is null || rings.Count == 0 || positions.Count == 0)
        {
            return Array.Empty<MapCanvasRingShape>();
        }

        var shapes = new List<MapCanvasRingShape>(rings.Count);
        foreach (var ring in rings)
        {
            if (ring is null || string.IsNullOrWhiteSpace(ring.UnitId))
            {
                continue;
            }

            if (!positions.TryGetValue(ring.UnitId, out var center))
            {
                continue;
            }

            var radius = NmToNormalizedRadius(ring.RangeNm, theaterWidthNm);
            if (radius <= 0f)
            {
                continue;
            }

            shapes.Add(new MapCanvasRingShape(
                Key: $"{ring.UnitId}:{ring.RingKind}",
                CenterX: center.X,
                CenterY: center.Y,
                RadiusNormalized: radius,
                RingKind: ring.RingKind,
                StyleClass: ResolveRingStyle(ring.RingKind)));
        }

        return shapes;
    }

    /// <summary>Projects datalink edges onto normalized line segments between unit positions.</summary>
    public static IReadOnlyList<MapCanvasEdgeShape> ProjectEdges(
        IReadOnlyList<DatalinkEdgeEntry>? edges,
        IReadOnlyDictionary<string, (float X, float Y)> positions)
    {
        if (edges is null || edges.Count == 0 || positions.Count == 0)
        {
            return Array.Empty<MapCanvasEdgeShape>();
        }

        var shapes = new List<MapCanvasEdgeShape>(edges.Count);
        foreach (var edge in edges)
        {
            if (edge is null
                || string.IsNullOrWhiteSpace(edge.FromUnitId)
                || string.IsNullOrWhiteSpace(edge.ToUnitId))
            {
                continue;
            }

            if (!positions.TryGetValue(edge.FromUnitId, out var from)
                || !positions.TryGetValue(edge.ToUnitId, out var to))
            {
                continue;
            }

            shapes.Add(new MapCanvasEdgeShape(
                Key: $"{edge.FromUnitId}->{edge.ToUnitId}",
                FromX: from.X,
                FromY: from.Y,
                ToX: to.X,
                ToY: to.Y,
                Status: edge.Status,
                StyleClass: ResolveEdgeStyle(edge.Status)));
        }

        return shapes;
    }

    private static string ResolveRingStyle(string ringKind) =>
        string.Equals(ringKind, TacticalOverlayProjection.RingKindWeapon, StringComparison.Ordinal)
            ? RingStyleWeapon
            : RingStyleSensor;

    private static string ResolveEdgeStyle(string status) =>
        status switch
        {
            DatalinkPictureProjection.StatusDegraded => EdgeStyleDegraded,
            DatalinkPictureProjection.StatusDown => EdgeStyleDown,
            _ => EdgeStyleUp,
        };

    private static readonly IReadOnlyDictionary<string, (float X, float Y)> EmptyPositions =
        new Dictionary<string, (float X, float Y)>(StringComparer.Ordinal);
}

/// <summary>Normalized canvas circle for an envelope ring overlay.</summary>
public sealed record MapCanvasRingShape(
    string Key,
    float CenterX,
    float CenterY,
    float RadiusNormalized,
    string RingKind,
    string StyleClass);

/// <summary>Normalized canvas line segment for a datalink edge overlay.</summary>
public sealed record MapCanvasEdgeShape(
    string Key,
    float FromX,
    float FromY,
    float ToX,
    float ToY,
    string Status,
    string StyleClass);
