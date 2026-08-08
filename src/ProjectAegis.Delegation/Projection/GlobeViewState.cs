namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// WGS84 camera pose for product globe (ADR-007 Phase B / CMD-06 · CMD-13 · CMD-28.10/11).
/// Presentation-only; never mutates sim or DecisionLog.
/// </summary>
public sealed record GlobeCameraState(
    double Latitude,
    double Longitude,
    double AltitudeMeters,
    double HeadingDeg,
    double PitchDeg);

/// <summary>2D (top-down) vs 3D (pitched) globe view mode (CMD-28.10).</summary>
public enum GlobeViewMode2d3d
{
    TwoD = 0,
    ThreeD = 1,
}

/// <summary>
/// Named theater or operator camera bookmark (CMD-28.11 quick-jump).
/// Empty slots report <see cref="IsEmpty"/> honestly — not disabled.
/// </summary>
public sealed record GlobeTheaterBookmark(
    string Id,
    string Label,
    GlobeCameraState Camera,
    bool IsEmpty = false)
{
    /// <summary>Honest empty slot (no saved view yet).</summary>
    public static GlobeTheaterBookmark EmptySlot(string id) =>
        new(id, string.Empty, new GlobeCameraState(0, 0, 0, 0, 0), IsEmpty: true);
}

/// <summary>
/// Headless product globe view bag: camera, bookmarks, active jump, 2D/3D mode.
/// Shared by Toolkit status chrome and Cesium hosts (ADR-007 Phase B).
/// </summary>
public sealed record GlobeViewState(
    GlobeCameraState Camera,
    IReadOnlyList<GlobeTheaterBookmark> Bookmarks,
    string? ActiveBookmarkId,
    GlobeViewMode2d3d Mode2d3d)
{
    public static GlobeViewState Empty { get; } = new(
        new GlobeCameraState(0, 0, 0, 0, 0),
        Array.Empty<GlobeTheaterBookmark>(),
        ActiveBookmarkId: null,
        Mode2d3d: GlobeViewMode2d3d.ThreeD);
}
