namespace ProjectAegis.Delegation.Roe;

using ProjectAegis.Sim.Policy;

public readonly record struct RoeEvaluation(RoeVerdict Verdict, FireAbortReason Reason = FireAbortReason.None)
{
    public static RoeEvaluation Allow() => new(RoeVerdict.Allow);

    public static RoeEvaluation Reject(FireAbortReason reason) =>
        new(RoeVerdict.Reject, reason);
}
