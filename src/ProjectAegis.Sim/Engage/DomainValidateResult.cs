namespace ProjectAegis.Sim.Engage;

using ProjectAegis.Sim.Policy;

/// <summary>ADR-009 validator outcome mapped to order-log abort codes when denied.</summary>
public readonly struct DomainValidateResult
{
    private DomainValidateResult(bool allowed, FireAbortReason? abortReason)
    {
        Allowed = allowed;
        AbortReason = abortReason;
    }

    public bool Allowed { get; }

    public FireAbortReason? AbortReason { get; }

    public static DomainValidateResult Allow { get; } = new(true, null);

    public static DomainValidateResult Deny(FireAbortReason reason) => new(false, reason);
}