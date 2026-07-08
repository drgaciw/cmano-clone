namespace ProjectAegis.Delegation.Projection;

/// <summary>Map camera zoom bands per req 20 rev 2 §Map and Symbology (theater / regional / tactical).</summary>
public enum MapZoomBand
{
    Theater = 0,
    Regional = 1,
    Tactical = 2,
}

/// <summary>
/// Pure icon-size ladder: <c>size = f(zoomBand)</c> (req 20 rev 2 §Map and Symbology — "Icon size ladder
/// per zoom band"). Also governs the label-visibility rule ("labels appear from regional zoom").
/// No UnityEngine dependency — safe for headless tests and the Unity host alike.
/// </summary>
public static class MapIconSizeLadder
{
    /// <summary>Smallest icon size — theater view, thousands of symbols, density over detail.</summary>
    public const float TheaterIconSizePx = 10f;

    /// <summary>Mid-tier icon size — regional view, labels begin appearing.</summary>
    public const float RegionalIconSizePx = 14f;

    /// <summary>Largest icon size — tactical view, full detail.</summary>
    public const float TacticalIconSizePx = 20f;

    /// <summary>Resolve the icon edge length (px) for a given zoom band. Pure function of the band only.</summary>
    public static float ResolveIconSizePx(MapZoomBand zoomBand) => zoomBand switch
    {
        MapZoomBand.Theater => TheaterIconSizePx,
        MapZoomBand.Regional => RegionalIconSizePx,
        MapZoomBand.Tactical => TacticalIconSizePx,
        _ => TheaterIconSizePx,
    };

    /// <summary>
    /// Req 20 rev 2: "labels appear from regional zoom" — theater band suppresses text labels entirely
    /// (icon/frame shape remains visible; only the text label is gated by zoom band).
    /// </summary>
    public static bool AreLabelsVisible(MapZoomBand zoomBand) => zoomBand != MapZoomBand.Theater;
}
