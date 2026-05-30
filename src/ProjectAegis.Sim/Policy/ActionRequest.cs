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

public readonly record struct ActionRequest(
    ActionKind Kind,
    ulong TargetId,
    ulong MountId);
