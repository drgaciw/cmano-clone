namespace ProjectAegis.Delegation.Projection;

/// <summary>CMD-30.7 / CMD-38 plotted-course polyline for the map canvas (not combat VFX).</summary>
public sealed record MapCourseOverlayEntry(
    string UnitId,
    IReadOnlyList<MapCourseVertex> Vertices);

/// <summary>Normalized canvas vertex on a plotted course.</summary>
public readonly record struct MapCourseVertex(float NormalizedX, float NormalizedY);
