namespace ProjectAegis.Sim.Swarm.Expend;

/// <summary>
/// SWARM-19 expend/kamikaze pulse outcome. Irreversible integrity spend when authorized.
/// </summary>
public sealed record SwarmExpendResult(
    bool Applied,
    int DronesExpended,
    int PreviousDroneCount,
    int NewDroneCount,
    string? DenyReason,
    ulong? OrderSequenceId,
    SwarmIntegrityChange? IntegrityChange)
{
    public static SwarmExpendResult Denied(string reason, int previous) =>
        new(false, 0, previous, previous, reason, null, null);

    public static SwarmExpendResult Succeeded(
        int spent,
        int previous,
        int next,
        ulong orderSeq,
        SwarmIntegrityChange change) =>
        new(true, spent, previous, next, null, orderSeq, change);
}
