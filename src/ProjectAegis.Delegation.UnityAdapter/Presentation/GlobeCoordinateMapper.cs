using ProjectAegis.Delegation.Projection;

namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

/// <summary>
/// Pure WGS84 ↔ map NDC (0..1) mapper for globe pick and drag-box parity with the placeholder map
/// (req 20 TR-c2-004 / TR-c2-005, ADR-018). No UnityEngine types.
/// </summary>
/// <remarks>
/// <para><b>Coordinate mapping (document for hosts):</b></para>
/// <list type="bullet">
/// <item>
/// <description>
/// Normalized device / canvas coords used by <see cref="SelectionBoxResolver"/> and
/// <see cref="MapSymbolEntry"/>: <c>NormalizedX</c> runs west→east with the theater longitude span;
/// <c>NormalizedY</c> runs south→north with the theater latitude span.
/// </description>
/// </item>
/// <item>
/// <description>
/// Inverse of <c>CesiumBillboardProjection</c> Baltic geo placement:
/// <c>lon = LonMin + NormalizedX * LonSpan</c>, <c>lat = LatMin + NormalizedY * LatSpan</c>.
/// </description>
/// </item>
/// <item>
/// <description>
/// Globe drag-box: host records two surface lat/lon corners (pointer-down / pointer-up), calls
/// <see cref="ToNormalizedRect"/>, then reuses <see cref="SelectionBoxResolver.Resolve"/> against
/// symbols already expressed in the same NDC space (or mapped via <see cref="ToNormalized"/>).
/// </description>
/// </item>
/// <item>
/// <description>
/// Points outside the theater bounds are still projected (extrapolated NDC, may be &lt;0 or &gt;1) so
/// off-edge picks remain deterministic; hosts may clamp for camera UI only.
/// </description>
/// </item>
/// </list>
/// </remarks>
public static class GlobeCoordinateMapper
{
    /// <summary>
    /// Map a WGS84 point into theater NDC. Returns null when <paramref name="bounds"/> is invalid.
    /// </summary>
    public static (float NormalizedX, float NormalizedY)? ToNormalized(
        double latitude,
        double longitude,
        GeographicBounds bounds)
    {
        if (!bounds.IsValid)
        {
            return null;
        }

        var x = (float)((longitude - bounds.MinLongitude) / bounds.LongitudeSpan);
        var y = (float)((latitude - bounds.MinLatitude) / bounds.LatitudeSpan);
        return (x, y);
    }

    /// <summary>
    /// Map theater NDC back to WGS84. Returns null when <paramref name="bounds"/> is invalid.
    /// </summary>
    public static (double Latitude, double Longitude)? ToGeographic(
        float normalizedX,
        float normalizedY,
        GeographicBounds bounds)
    {
        if (!bounds.IsValid)
        {
            return null;
        }

        var lon = bounds.MinLongitude + normalizedX * bounds.LongitudeSpan;
        var lat = bounds.MinLatitude + normalizedY * bounds.LatitudeSpan;
        return (lat, lon);
    }

    /// <summary>
    /// Build a <see cref="NormalizedRect"/> from two arbitrary globe surface corners (lat/lon),
    /// suitable for <see cref="SelectionBoxResolver.Resolve"/>. Returns null when bounds invalid.
    /// </summary>
    public static NormalizedRect? ToNormalizedRect(
        double latitude0,
        double longitude0,
        double latitude1,
        double longitude1,
        GeographicBounds bounds)
    {
        var a = ToNormalized(latitude0, longitude0, bounds);
        var b = ToNormalized(latitude1, longitude1, bounds);
        if (a is null || b is null)
        {
            return null;
        }

        return NormalizedRect.FromCorners(a.Value.NormalizedX, a.Value.NormalizedY, b.Value.NormalizedX, b.Value.NormalizedY);
    }

    /// <summary>
    /// Project a <see cref="MapSymbolEntry"/>'s NDC into WGS84 under the given theater bounds.
    /// </summary>
    public static (double Latitude, double Longitude)? SymbolToGeographic(
        MapSymbolEntry symbol,
        GeographicBounds bounds) =>
        ToGeographic(symbol.NormalizedX, symbol.NormalizedY, bounds);
}
