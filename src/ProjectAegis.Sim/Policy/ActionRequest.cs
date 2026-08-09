namespace ProjectAegis.Sim.Policy;

public enum ActionKind
{
    Observe = 0,
    Illuminate = 1,
    Designate = 2,
    FireBallistic = 3,
    FireGuided = 4,
    Jam = 5,
}

/// <summary>
/// Policy action request. Optional <see cref="IsAutoEngage"/> / <see cref="IsExpend"/> flags
/// gate SWARM-15 doctrine without changing existing call sites (defaults false).
/// </summary>
public readonly record struct ActionRequest(
    ActionKind Kind,
    ulong TargetId,
    ulong MountId,
    bool IsAutoEngage = false,
    bool IsExpend = false);
