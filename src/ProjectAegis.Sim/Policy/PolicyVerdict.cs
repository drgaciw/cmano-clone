namespace ProjectAegis.Sim.Policy;

public readonly record struct PolicyVerdict(bool Allowed, FireAbortReason Reason)
{
    public static PolicyVerdict Allow() => new(true, FireAbortReason.None);

    public static PolicyVerdict Deny(FireAbortReason reason) => new(false, reason);
}
