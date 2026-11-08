namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// SWARM-05 / SWARM-09: project a swarm as one map symbol with density-safe glyph
/// and textual integrity label (not color-only).
/// </summary>
public static class SwarmMapSymbolProjection
{
    /// <summary>Distinct from single light aircraft / surface unit glyph family.</summary>
    public const string FriendlySwarmGlyph = "☷";

    public const string FriendlySwarmSidc = "SFAPM----------"; // air friendly multipoint abstract

    public const string FriendlySwarmFrame = "map-app6-frame--friendly-swarm";

    public static MapSymbolEntry Project(
        SwarmIntegrityReadout integrity,
        string affiliation = "Friendly",
        float normalizedX = 0.5f,
        float normalizedY = 0.5f,
        double? latitude = null,
        double? longitude = null)
    {
        if (integrity is null)
        {
            throw new ArgumentNullException(nameof(integrity));
        }

        var destroyed = integrity.IsDestroyed || integrity.DroneCount <= 0;
        var glyph = destroyed ? App6Sidc.FriendlyDestroyedGlyph : FriendlySwarmGlyph;
        var frame = destroyed ? App6Sidc.FriendlyDestroyedFrame : FriendlySwarmFrame;
        var label = $"{integrity.UnitId} {integrity.MapLabelSuffix}";
        return new MapSymbolEntry(
            integrity.UnitId,
            affiliation,
            glyph,
            label,
            normalizedX,
            normalizedY,
            destroyed,
            FriendlySwarmSidc,
            frame,
            latitude,
            longitude,
            IsSwarm: true,
            IntegrityLabel: integrity.CountLabel);
    }
}
