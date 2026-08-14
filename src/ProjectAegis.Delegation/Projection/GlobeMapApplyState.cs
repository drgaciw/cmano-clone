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
        IReadOnlyList<CesiumBillboardMarker>? markers) =>
        Apply(view, markers, envelopeRings: null, datalinkEdges: null);

    /// <summary>
    /// Apply view + markers + optional globe overlay markers (CMD-21/32/34, DRG-161).
    /// Status line includes ring/edge counts when non-zero:
    /// <c>GLOBE · Baltic · 2 markers · 2 rings · 1 links · 3D</c>.
    /// </summary>
    public static GlobeMapPresentation Apply(
        GlobeViewState? view,
        IReadOnlyList<CesiumBillboardMarker>? markers,
        IReadOnlyList<GlobeEnvelopeRingMarker>? envelopeRings,
        IReadOnlyList<GlobeDatalinkEdgeMarker>? datalinkEdges)
    {
        var theater = GlobeViewProjection.ResolveTheaterLabel(view);
        var count = CountMarkers(markers);
        var ringCount = CountNonNull(envelopeRings);
        var edgeCount = CountNonNull(datalinkEdges);
        var modeLabel = ResolveModeLabel(view);
        var status = FormatStatusLine(theater, count, ringCount, edgeCount, modeLabel);

        var bookmarks = GlobeViewProjection.PresentBookmarks(view?.Bookmarks);
        return new GlobeMapPresentation(
            StatusLine: status,
            TheaterLabel: theater,
            MarkerCount: count,
            EnvelopeRingCount: ringCount,
            DatalinkEdgeCount: edgeCount,
            ModeLabel: modeLabel,
            ActiveBookmarkId: view?.ActiveBookmarkId,
            Bookmarks: bookmarks,
            Camera: view?.Camera,
            EnvelopeRings: envelopeRings ?? Array.Empty<GlobeEnvelopeRingMarker>(),
            DatalinkEdges: datalinkEdges ?? Array.Empty<GlobeDatalinkEdgeMarker>());
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

    private static string FormatStatusLine(
        string theater,
        int markerCount,
        int ringCount,
        int edgeCount,
        string modeLabel)
    {
        if (ringCount > 0 && edgeCount > 0)
        {
            return $"GLOBE · {theater} · {markerCount} markers · {ringCount} rings · {edgeCount} links · {modeLabel}";
        }

        if (ringCount > 0)
        {
            return $"GLOBE · {theater} · {markerCount} markers · {ringCount} rings · {modeLabel}";
        }

        if (edgeCount > 0)
        {
            return $"GLOBE · {theater} · {markerCount} markers · {edgeCount} links · {modeLabel}";
        }

        return $"GLOBE · {theater} · {markerCount} markers · {modeLabel}";
    }

    private static int CountNonNull<T>(IReadOnlyList<T>? items)
    {
        if (items is null || items.Count == 0)
        {
            return 0;
        }

        var count = 0;
        foreach (var item in items)
        {
            if (item is not null)
            {
                count++;
            }
        }

        return count;
    }
}

/// <summary>Applied product globe presentation fields for Toolkit / Cesium hosts.</summary>
public sealed record GlobeMapPresentation(
    string StatusLine,
    string TheaterLabel,
    int MarkerCount,
    int EnvelopeRingCount,
    int DatalinkEdgeCount,
    string ModeLabel,
    string? ActiveBookmarkId,
    GlobeBookmarksPresentation Bookmarks,
    GlobeCameraState? Camera,
    IReadOnlyList<GlobeEnvelopeRingMarker> EnvelopeRings,
    IReadOnlyList<GlobeDatalinkEdgeMarker> DatalinkEdges)
{
    public static GlobeMapPresentation Empty { get; } = GlobeMapApplyState.Apply(
        GlobeViewState.Empty,
        Array.Empty<CesiumBillboardMarker>());
}
