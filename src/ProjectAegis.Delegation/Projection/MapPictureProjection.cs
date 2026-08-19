namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Delegation.Core;

/// <summary>Deterministic tactical map symbols; hash-places until a snapshot pose is published.</summary>
public static class MapPictureProjection
{
    /// <summary>Baltic demo box shared with <see cref="CesiumBillboardProjection"/> (ADR-007 Phase B).</summary>
    public const double BalticLatMin = 59.5;

    public const double BalticLatSpan = 1.0;

    public const double BalticLonMin = 24.0;

    public const double BalticLonSpan = 1.5;

    public static IReadOnlyList<MapSymbolEntry> Project(
        IReadOnlyList<OobTreeEntry> oob,
        IReadOnlyList<ContactPictureEntry> contacts,
        int layoutSeed,
        IReadOnlyDictionary<string, UnitKinematicPose>? poses = null)
    {
        var symbols = new List<MapSymbolEntry>(oob.Count + contacts.Count);
        foreach (var unit in oob)
        {
            var (x, y, lat, lon, authoritative, course, speed) =
                ResolvePlacement(unit.UnitId, layoutSeed, poses);
            var isDestroyed = !unit.IsAlive;
            var resolution = App6Sidc.ResolveMapGlyph("Friendly", isDestroyed);
            symbols.Add(new MapSymbolEntry(
                unit.UnitId,
                "Friendly",
                resolution.UnicodeGlyph,
                unit.UnitId,
                x,
                y,
                isDestroyed,
                resolution.Sidc,
                resolution.UssFrameId,
                lat,
                lon,
                HasAuthoritativePose: authoritative,
                CourseDeg: course,
                SpeedNmPerHour: speed));
        }

        foreach (var contact in contacts)
        {
            var (x, y, lat, lon, authoritative, course, speed) =
                ResolvePlacement(contact.ContactId, layoutSeed + 17, poses);
            var resolution = App6Sidc.ResolveMapGlyph("Hostile");
            symbols.Add(new MapSymbolEntry(
                contact.ContactId,
                "Hostile",
                resolution.UnicodeGlyph,
                $"{contact.ContactId} {contact.LifecycleState}",
                x,
                y,
                IsDestroyed: false,
                resolution.Sidc,
                resolution.UssFrameId,
                lat,
                lon,
                HasAuthoritativePose: authoritative,
                CourseDeg: course,
                SpeedNmPerHour: speed));
        }

        return symbols
            .OrderBy(s => s.Affiliation, StringComparer.Ordinal)
            .ThenBy(s => s.SymbolId, StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>
    /// Projects plotted-course polylines for units that have waypoint lists.
    /// Current pose (or hash fallback) is prepended when it is not already the first vertex.
    /// Destroyed units emit no course.
    /// </summary>
    public static IReadOnlyList<MapCourseOverlayEntry> ProjectCourses(
        IReadOnlyList<OobTreeEntry> oob,
        IReadOnlyDictionary<string, IReadOnlyList<CourseWaypoint>>? courses,
        IReadOnlyDictionary<string, UnitKinematicPose>? poses,
        int layoutSeed)
    {
        if (courses is null || courses.Count == 0 || oob.Count == 0)
        {
            return Array.Empty<MapCourseOverlayEntry>();
        }

        var overlays = new List<MapCourseOverlayEntry>(courses.Count);
        foreach (var unit in oob.OrderBy(u => u.UnitId, StringComparer.Ordinal))
        {
            if (!unit.IsAlive
                || !courses.TryGetValue(unit.UnitId, out var waypoints)
                || waypoints is null
                || waypoints.Count == 0)
            {
                continue;
            }

            var (x, y, _, _, _, _, _) = ResolvePlacement(unit.UnitId, layoutSeed, poses);
            var vertices = new List<MapCourseVertex>(waypoints.Count + 1);
            if (!SameVertex(x, y, waypoints[0]))
            {
                vertices.Add(new MapCourseVertex(x, y));
            }

            foreach (var wp in waypoints)
            {
                vertices.Add(new MapCourseVertex(wp.NormalizedX, wp.NormalizedY));
            }

            if (vertices.Count < 2)
            {
                continue;
            }

            overlays.Add(new MapCourseOverlayEntry(unit.UnitId, vertices));
        }

        return overlays;
    }

    public static (float X, float Y) Place(string key, int seed)
    {
        var h = DeterministicHash.OrdinalHash($"{seed}:{key}");
        var hx = (uint)(h & 0xFFFF);
        var hy = (uint)((h >> 16) & 0xFFFF);
        var x = hx / 65535f * 0.75f + 0.1f;
        var y = hy / 65535f * 0.75f + 0.1f;
        return (x, y);
    }

    /// <summary>Projects WGS84 onto the Baltic demo canvas (inverse of Cesium hash→geo).</summary>
    public static (float X, float Y) ProjectLatLon(double latitude, double longitude)
    {
        var x = (float)((longitude - BalticLonMin) / BalticLonSpan);
        var y = (float)((latitude - BalticLatMin) / BalticLatSpan);
        return (Clamp01(x), Clamp01(y));
    }

    /// <summary>Inverse of <see cref="ProjectLatLon"/> for canvas-only poses.</summary>
    public static (double Lat, double Lon) CanvasToBalticGeo(float normalizedX, float normalizedY) =>
        (BalticLatMin + normalizedY * BalticLatSpan, BalticLonMin + normalizedX * BalticLonSpan);

    private static (float X, float Y, double? Lat, double? Lon, bool Authoritative, float? Course, float? Speed)
        ResolvePlacement(
            string id,
            int seed,
            IReadOnlyDictionary<string, UnitKinematicPose>? poses)
    {
        if (poses is not null && poses.TryGetValue(id, out var pose))
        {
            if (pose.NormalizedX is float nx && pose.NormalizedY is float ny)
            {
                var (lat, lon) = pose.Latitude is double plat && pose.Longitude is double plon
                    ? (plat, plon)
                    : CanvasToBalticGeo(nx, ny);
                return (nx, ny, lat, lon, true, pose.CourseDeg, pose.SpeedNmPerHour);
            }

            if (pose.Latitude is double latOnly && pose.Longitude is double lonOnly)
            {
                var (x, y) = ProjectLatLon(latOnly, lonOnly);
                return (x, y, latOnly, lonOnly, true, pose.CourseDeg, pose.SpeedNmPerHour);
            }
        }

        var hash = Place(id, seed);
        return (hash.X, hash.Y, null, null, false, null, null);
    }

    private static bool SameVertex(float x, float y, CourseWaypoint waypoint) =>
        Math.Abs(x - waypoint.NormalizedX) < 1e-5f
        && Math.Abs(y - waypoint.NormalizedY) < 1e-5f;

    private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
}
