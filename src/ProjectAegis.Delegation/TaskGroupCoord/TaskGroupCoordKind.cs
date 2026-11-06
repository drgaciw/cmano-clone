namespace ProjectAegis.Delegation.TaskGroupCoord;

/// <summary>
/// DRG-223: advisory classification for task-group coordination output.
/// Only <see cref="AdvisoryCoordination"/> is emitted — never orders, detach, or replan.
/// </summary>
public enum TaskGroupCoordKind
{
    /// <summary>Headless coordination posture for UI review — not a C2 order.</summary>
    AdvisoryCoordination = 0,
}
