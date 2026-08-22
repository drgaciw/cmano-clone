namespace ProjectAegis.Delegation.Projection;

using System.Globalization;

/// <summary>
/// DRG-179: maps kill-chain contact state into C2 display rows.
/// Presentation binds labels; it must not re-derive Find/Fix/Track/Target (ADR-010).
/// Unity host rendering is DRG-180.
/// </summary>
public static class KillChainContactPanelBinder
{
    public static KillChainContactPanelState Bind(KillChainContactSnapshot? snapshot)
    {
        if (snapshot is null || snapshot.Contacts.Count == 0 && snapshot.Transitions.Count == 0)
        {
            return KillChainContactPanelState.Empty;
        }

        var rows = new KillChainContactRow[snapshot.Contacts.Count];
        for (var i = 0; i < snapshot.Contacts.Count; i++)
        {
            rows[i] = BindRow(snapshot.Contacts[i]);
        }

        var lines = new string[snapshot.Transitions.Count];
        for (var i = 0; i < snapshot.Transitions.Count; i++)
        {
            lines[i] = FormatTransition(snapshot.Transitions[i]);
        }

        return new KillChainContactPanelState(
            $"KC: {snapshot.Contacts.Count}",
            $"KC-TX: {snapshot.Transitions.Count}",
            rows,
            lines);
    }

    private static KillChainContactRow BindRow(KillChainContactState state) =>
        new(
            state.ContactId,
            state.TargetId,
            FormatPhaseLabel(state.Phase),
            FormatPhaseClass(state.Phase),
            state.DetectionCaptured ? "DET" : "DET: —",
            state.LocationSufficient ? "LOC" : "LOC: —",
            state.TrackContinuous ? "TRK" : "TRK: —",
            state.Targetable ? "TGT" : "TGT: —",
            FormatLoss(state.Loss),
            $"T {state.LastSimTime.ToString("R", CultureInfo.InvariantCulture)}",
            $"SEQ: {state.CorrelationSequenceId}",
            string.Join(" ", state.SourceRefs));

    private static string FormatPhaseLabel(KillChainPhase phase) =>
        phase switch
        {
            KillChainPhase.Find => "KC: FIND",
            KillChainPhase.Fix => "KC: FIX",
            KillChainPhase.Track => "KC: TRACK",
            KillChainPhase.Target => "KC: TARGET",
            _ => "KC: —",
        };

    private static string FormatPhaseClass(KillChainPhase phase) =>
        phase switch
        {
            KillChainPhase.Find => "kill-chain-phase--find",
            KillChainPhase.Fix => "kill-chain-phase--fix",
            KillChainPhase.Track => "kill-chain-phase--track",
            KillChainPhase.Target => "kill-chain-phase--target",
            _ => "kill-chain-phase--none",
        };

    private static string FormatLoss(KillChainLossKind loss) =>
        loss switch
        {
            KillChainLossKind.Stale => "LOSS: STALE",
            KillChainLossKind.DegradedL1 => "LOSS: DEGRADED-L1",
            KillChainLossKind.DegradedL2 => "LOSS: DEGRADED-L2",
            KillChainLossKind.Lost => "LOSS: LOST",
            _ => "LOSS: —",
        };

    private static string FormatTransition(KillChainContactTransition transition)
    {
        var kind = transition.Kind.ToString().ToUpperInvariant();
        return string.Format(
            CultureInfo.InvariantCulture,
            "{0} {1} {2} {3}->{4} SEQ:{5}",
            transition.SimTick,
            transition.ContactId,
            kind,
            transition.PreviousPhase,
            transition.NewPhase,
            transition.CorrelationSequenceId);
    }
}
