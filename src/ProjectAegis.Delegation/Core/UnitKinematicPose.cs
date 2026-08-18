namespace ProjectAegis.Delegation.Core;

/// <summary>
/// Per-entity kinematic pose published on the snapshot → map seam (CMD-38 / ADR-010).
/// Either WGS84 lat/lon or normalized canvas xy is sufficient; course/speed advance the picture.
/// </summary>
public readonly record struct UnitKinematicPose(
    double? Latitude,
    double? Longitude,
    float? NormalizedX,
    float? NormalizedY,
    float CourseDeg,
    float SpeedNmPerHour);

/// <summary>Plotted-course waypoint (normalized canvas and optional WGS84).</summary>
public readonly record struct CourseWaypoint(
    float NormalizedX,
    float NormalizedY,
    double? Latitude = null,
    double? Longitude = null);
