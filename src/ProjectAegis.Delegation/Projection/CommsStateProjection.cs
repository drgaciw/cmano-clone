namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Delegation.Comms;
using ProjectAegis.Delegation.Decision;

public sealed record CommsStateSnapshot(
    CommsState State,
    string NodeId,
    string TopBarLabel);

/// <summary>Rebuilds comms HUD state from order log (authoritative for replay).</summary>
public static class CommsStateProjection
{
    public static CommsStateSnapshot Project(DecisionLog log)
    {
        var state = CommsState.Nominal;
        var node = "c2-net";
        foreach (var change in log.CommsStateChanges.OrderBy(c => c.SequenceId))
        {
            state = change.NewState;
            node = change.NodeId;
        }

        return new CommsStateSnapshot(state, node, FormatTopBar(state));
    }

    public static string FormatTopBar(CommsState state) =>
        state switch
        {
            CommsState.Nominal => "COMMS: NOMINAL",
            CommsState.Degraded => "COMMS: DEGRADED",
            CommsState.Denied => "COMMS: DENIED",
            _ => "COMMS: —",
        };

    public static bool BlocksNewEngagement(CommsState state) => state == CommsState.Denied;
}