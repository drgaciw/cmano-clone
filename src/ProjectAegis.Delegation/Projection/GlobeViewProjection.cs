namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Pure projection helpers for product globe view state (ADR-007 Phase B).
/// Theater presets + quick-jump; no sim mutation, no Cesium package dependency.
/// </summary>
public static class GlobeViewProjection
{
    /// <summary>Honest empty bookmark list copy (CMD-28.11 / REQ empty-not-disabled).</summary>
    public const string EmptyBookmarksLine =
        "No saved views — press Ctrl+1 to save one";

    // Baltic bbox overview (matches CesiumBillboardProjection Baltic demo geo).
    private const double BalticLat = 60.0;
    private const double BalticLon = 24.8;
    private const double BalticAltMeters = 1_000_000.0;

    // GIUK gap overview (Greenland–Iceland–UK).
    private const double GiukLat = 65.0;
    private const double GiukLon = -20.0;
    private const double GiukAltMeters = 1_500_000.0;

    // Western Pacific overview.
    private const double PacificLat = 20.0;
    private const double PacificLon = 140.0;
    private const double PacificAltMeters = 2_000_000.0;

    public const string BalticBookmarkId = "baltic";
    public const string GiukBookmarkId = "giuk";
    public const string PacificBookmarkId = "pacific";

    /// <summary>Named theater camera: Baltic Sea (default product theater).</summary>
    public static GlobeTheaterBookmark BalticTheater() =>
        new(
            BalticBookmarkId,
            "Baltic",
            new GlobeCameraState(BalticLat, BalticLon, BalticAltMeters, HeadingDeg: 0, PitchDeg: -45),
            IsEmpty: false);

    /// <summary>Named theater camera: GIUK gap.</summary>
    public static GlobeTheaterBookmark GiukTheater() =>
        new(
            GiukBookmarkId,
            "GIUK",
            new GlobeCameraState(GiukLat, GiukLon, GiukAltMeters, HeadingDeg: 0, PitchDeg: -45),
            IsEmpty: false);

    /// <summary>Named theater camera: Pacific.</summary>
    public static GlobeTheaterBookmark PacificTheater() =>
        new(
            PacificBookmarkId,
            "Pacific",
            new GlobeCameraState(PacificLat, PacificLon, PacificAltMeters, HeadingDeg: 0, PitchDeg: -45),
            IsEmpty: false);

    /// <summary>Built-in theater preset bookmarks (Baltic, GIUK, Pacific).</summary>
    public static IReadOnlyList<GlobeTheaterBookmark> TheaterPresets { get; } =
    [
        BalticTheater(),
        GiukTheater(),
        PacificTheater(),
    ];

    /// <summary>
    /// Default product globe state: camera over Baltic bbox, theater presets as bookmarks, 3D mode.
    /// </summary>
    public static GlobeViewState DefaultBalticTheater()
    {
        var baltic = BalticTheater();
        return new GlobeViewState(
            Camera: baltic.Camera,
            Bookmarks: TheaterPresets,
            ActiveBookmarkId: baltic.Id,
            Mode2d3d: GlobeViewMode2d3d.ThreeD);
    }

    /// <summary>
    /// Quick-jump to a bookmark by id. Returns a new view state; does not mutate sim.
    /// Unknown id returns the input state unchanged.
    /// </summary>
    public static GlobeViewState WithQuickJump(GlobeViewState state, string bookmarkId)
    {
        if (state is null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        if (string.IsNullOrWhiteSpace(bookmarkId))
        {
            return state;
        }

        var bookmarks = state.Bookmarks ?? Array.Empty<GlobeTheaterBookmark>();
        for (var i = 0; i < bookmarks.Count; i++)
        {
            var bookmark = bookmarks[i];
            if (bookmark is null || bookmark.IsEmpty)
            {
                continue;
            }

            if (string.Equals(bookmark.Id, bookmarkId, StringComparison.OrdinalIgnoreCase))
            {
                return state with
                {
                    Camera = bookmark.Camera,
                    ActiveBookmarkId = bookmark.Id,
                };
            }
        }

        return state;
    }

    /// <summary>
    /// Honest empty bookmarks presentation — empty is not disabled (CMD-28.11).
    /// </summary>
    public static GlobeBookmarksPresentation EmptyBookmarksPresentation() =>
        new(
            Bookmarks: Array.Empty<GlobeTheaterBookmark>(),
            IsEmpty: true,
            EmptyStateLine: EmptyBookmarksLine,
            IsDisabled: false);

    /// <summary>
    /// Present bookmarks for chrome binding. Empty / all-empty-slot lists use honest empty copy.
    /// </summary>
    public static GlobeBookmarksPresentation PresentBookmarks(IReadOnlyList<GlobeTheaterBookmark>? bookmarks)
    {
        if (bookmarks is null || bookmarks.Count == 0)
        {
            return EmptyBookmarksPresentation();
        }

        var live = new List<GlobeTheaterBookmark>(bookmarks.Count);
        foreach (var b in bookmarks)
        {
            if (b is not null && !b.IsEmpty)
            {
                live.Add(b);
            }
        }

        if (live.Count == 0)
        {
            return EmptyBookmarksPresentation();
        }

        return new GlobeBookmarksPresentation(
            Bookmarks: live,
            IsEmpty: false,
            EmptyStateLine: string.Empty,
            IsDisabled: false);
    }

    /// <summary>Toggle 2D (top-down pitch) vs 3D (pitched) without changing lat/lon/alt/heading.</summary>
    public static GlobeViewState WithMode(GlobeViewState state, GlobeViewMode2d3d mode)
    {
        if (state is null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        var pitch = mode == GlobeViewMode2d3d.TwoD ? -90.0 : -45.0;
        return state with
        {
            Mode2d3d = mode,
            Camera = state.Camera with { PitchDeg = pitch },
        };
    }

    /// <summary>Resolve theater label from active bookmark, else first non-empty bookmark, else "Globe".</summary>
    public static string ResolveTheaterLabel(GlobeViewState? state)
    {
        if (state is null)
        {
            return "Globe";
        }

        var bookmarks = state.Bookmarks ?? Array.Empty<GlobeTheaterBookmark>();
        if (!string.IsNullOrWhiteSpace(state.ActiveBookmarkId))
        {
            foreach (var b in bookmarks)
            {
                if (b is null || b.IsEmpty)
                {
                    continue;
                }

                if (string.Equals(b.Id, state.ActiveBookmarkId, StringComparison.OrdinalIgnoreCase))
                {
                    return string.IsNullOrWhiteSpace(b.Label) ? b.Id : b.Label;
                }
            }
        }

        foreach (var b in bookmarks)
        {
            if (b is not null && !b.IsEmpty && !string.IsNullOrWhiteSpace(b.Label))
            {
                return b.Label;
            }
        }

        return "Globe";
    }
}

/// <summary>Bookmark list presentation for globe chrome (honest empty vs disabled).</summary>
public sealed record GlobeBookmarksPresentation(
    IReadOnlyList<GlobeTheaterBookmark> Bookmarks,
    bool IsEmpty,
    string EmptyStateLine,
    bool IsDisabled);
