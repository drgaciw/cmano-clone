namespace ProjectAegis.Ai.Host.Authoring;

/// <summary>Typed proposed operation from a fake/scripted model (Propose lane only).</summary>
public sealed record AuthoringProposedOperation(
    string Op,
    string MissionId,
    IReadOnlyList<string> AssignedUnitIds,
    IReadOnlyList<string>? TargetIds = null,
    IReadOnlyList<AuthoringWaypoint>? Waypoints = null,
    string? Role = null);

/// <summary>Waypoint for patrol/support ops.</summary>
public sealed record AuthoringWaypoint(double Lat, double Lon);
