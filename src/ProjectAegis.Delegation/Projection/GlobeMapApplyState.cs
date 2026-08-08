namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Headless apply path for product globe status chrome (ADR-007 Phase B / CMD-06 · CMD-13).
/// Unity hosts bind <see cref="GlobeMapPresentation.StatusLine"/> without re-formatting.
/// </summary>
public static class GlobeMapApplyState
{
    /// <summary>
    /// Apply view + marker projection into presentation fields.
    /// Status line form: <c>GLOBE · Baltic · 2 markers · 3D</c>.
    /// </summary>
    public static GlobeMapPresentation Apply(
        GlobeViewState? view,
        IReadOnlyList<CesiumBillboardMarker>? markers)
    {
        var theater = GlobeViewProjection.ResolveTheaterLabel(view);
        var count = CountMarkers(markers);
        var modeLabel = ResolveModeLabel(view);
        var status = $"GLOBE · {theater} · {count} markers · {modeLabel}";

        var bookmarks = GlobeViewProjection.PresentBookmarks(view?.Bookmarks);
        return new GlobeMapPresentation(
            StatusLine: status,
            TheaterLabel: theater,
            MarkerCount: count,
            ModeLabel: modeLabel,
            ActiveBookmarkId: view?.ActiveBookmarkId,
            Bookmarks: bookmarks,
            Camera: view?.Camera);
    }

    /// <summary>
    /// Project symbols then apply — product path for headless globe status refresh tests.
    /// Theater quick-jump does not mutate sim; markers are a pure projection of symbols.
    /// </summary>
    public static GlobeMapPresentation ProjectAndApply(
        GlobeViewState view,
        IReadOnlyList<MapSymbolEntry> symbols,
        int layoutSeed = 7)
    {
        if (view is null)
        {
            throw new ArgumentNullException(nameof(view));
        }

        if (symbols is null)
        {
            throw new ArgumentNullException(nameof(symbols));
        }

        var markers = CesiumBillboardProjection.ProjectWithCamera(symbols, view.Camera, layoutSeed);
        return Apply(view, markers);
    }

    private static int CountMarkers(IReadOnlyList<CesiumBillboardMarker>? markers)
    {
        if (markers is null || markers.Count == 0)
        {
            return 0;
        }

        var count = 0;
        foreach (var m in markers)
        {
            if (m is not null)
            {
                count++;
            }
        }

        return count;
    }

    private static string ResolveModeLabel(GlobeViewState? view)
    {
        if (view is null)
        {
            return "3D";
        }

        return view.Mode2d3d == GlobeViewMode2d3d.TwoD ? "2D" : "3D";
    }
}

/// <summary>Applied product globe presentation fields for Toolkit / Cesium hosts.</summary>
public sealed record GlobeMapPresentation(
    string StatusLine,
    string TheaterLabel,
    int MarkerCount,
    string ModeLabel,
    string? ActiveBookmarkId,
    GlobeBookmarksPresentation Bookmarks,
    GlobeCameraState? Camera)
{
    public static GlobeMapPresentation Empty { get; } = GlobeMapApplyState.Apply(
        GlobeViewState.Empty,
        Array.Empty<CesiumBillboardMarker>());
}
