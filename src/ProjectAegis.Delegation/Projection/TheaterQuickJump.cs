namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Named theater catalog entry for globe quick-jump (req 20 §Map §6.6 / TR-c2-004, ADR-018).
/// Presentation-only — hosts apply the bounds to the camera/georeference; never mutates sim (ADR-010).
/// </summary>
public sealed record TheaterDefinition(
    string Id,
    string DisplayName,
    GeographicBounds Bounds,
    MapZoomBand DefaultZoomBand = MapZoomBand.Theater);

/// <summary>
/// Pure theater quick-jump helper: Baltic production bbox + named theaters and scenario aliases.
/// No UnityEngine dependency — Cesium hosts resolve ids here then fly the georeference camera.
/// </summary>
/// <remarks>
/// Baltic bounds match the S20/S25 Cesium billboard demo mapping
/// (<see cref="CesiumBillboardProjection"/>): lat [59.5, 60.5], lon [24.0, 25.5].
/// </remarks>
public static class TheaterQuickJump
{
    /// <summary>Canonical Baltic theater id (matches production scenario policy prefix).</summary>
    public const string BalticId = "baltic";

    /// <summary>
    /// Production Baltic bbox used by Cesium spike / billboard projection and default quick-jump.
    /// </summary>
    public static GeographicBounds BalticBounds { get; } = new(
        MinLatitude: 59.5,
        MinLongitude: 24.0,
        MaxLatitude: 60.5,
        MaxLongitude: 25.5);

    /// <summary>Baltic Sea theater — default for Project Aegis vertical slice.</summary>
    public static TheaterDefinition Baltic { get; } = new(
        Id: BalticId,
        DisplayName: "Baltic Sea",
        Bounds: BalticBounds,
        DefaultZoomBand: MapZoomBand.Theater);

    /// <summary>GIUK gap overview (named catalog entry for multi-theater jump UI).</summary>
    public static TheaterDefinition Giuk { get; } = new(
        Id: "giuk",
        DisplayName: "GIUK Gap",
        Bounds: new GeographicBounds(55.0, -35.0, 70.0, 10.0),
        DefaultZoomBand: MapZoomBand.Theater);

    /// <summary>Eastern Mediterranean overview.</summary>
    public static TheaterDefinition EastMed { get; } = new(
        Id: "east-med",
        DisplayName: "Eastern Mediterranean",
        Bounds: new GeographicBounds(30.0, 20.0, 42.0, 36.0),
        DefaultZoomBand: MapZoomBand.Theater);

    /// <summary>Persian / Arabian Gulf overview.</summary>
    public static TheaterDefinition PersianGulf { get; } = new(
        Id: "persian-gulf",
        DisplayName: "Persian Gulf",
        Bounds: new GeographicBounds(23.0, 48.0, 30.5, 57.0),
        DefaultZoomBand: MapZoomBand.Theater);

    private static readonly TheaterDefinition[] Catalog =
    [
        Baltic,
        Giuk,
        EastMed,
        PersianGulf,
    ];

    private static readonly Dictionary<string, TheaterDefinition> ById =
        BuildLookup(Catalog);

    /// <summary>All registered theaters in stable catalog order.</summary>
    public static IReadOnlyList<TheaterDefinition> All => Catalog;

    /// <summary>
    /// Resolve a theater by id, display-name, or scenario-policy alias (e.g. <c>baltic-patrol</c>,
    /// <c>baltic-v3-*</c>). Case-insensitive. Returns null when unknown or null/empty.
    /// </summary>
    public static TheaterDefinition? Resolve(string? theaterIdOrAlias)
    {
        if (string.IsNullOrWhiteSpace(theaterIdOrAlias))
        {
            return null;
        }

        var key = theaterIdOrAlias.Trim();
        if (ById.TryGetValue(key, out var exact))
        {
            return exact;
        }

        // Scenario policy aliases (Baltic production family).
        if (key.StartsWith("baltic", StringComparison.OrdinalIgnoreCase))
        {
            return Baltic;
        }

        return null;
    }

    /// <summary>
    /// Resolve or fall back to <see cref="Baltic"/> when the id is unknown (safe default for CI /
    /// Baltic vertical slice hosts).
    /// </summary>
    public static TheaterDefinition ResolveOrBaltic(string? theaterIdOrAlias) =>
        Resolve(theaterIdOrAlias) ?? Baltic;

    private static Dictionary<string, TheaterDefinition> BuildLookup(IReadOnlyList<TheaterDefinition> theaters)
    {
        var map = new Dictionary<string, TheaterDefinition>(StringComparer.OrdinalIgnoreCase);
        foreach (var theater in theaters)
        {
            map[theater.Id] = theater;
            map[theater.DisplayName] = theater;
        }

        return map;
    }
}
