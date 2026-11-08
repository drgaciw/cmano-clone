namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// DRG-179: Find / Fix / Track / Target phase for C2. Distinct from sensor
/// <c>Detected/Classified/Identified</c> labels. Presentation binds this; it must not
/// re-derive combat truth (ADR-010).
/// </summary>
public enum KillChainPhase
{
    None = 0,
    Find = 1,
    Fix = 2,
    Track = 3,
    Target = 4,
}

/// <summary>Loss or degradation overlay on a kill-chain contact. Independent of phase.</summary>
public enum KillChainLossKind
{
    None = 0,
    Stale = 1,
    DegradedL1 = 2,
    DegradedL2 = 3,
    Lost = 4,
}

/// <summary>Published kill-chain transition kinds, including loss/degradation.</summary>
public enum KillChainTransitionKind
{
    Find = 1,
    Fix = 2,
    Track = 3,
    Target = 4,
    Degraded = 5,
    Lost = 6,
}

/// <summary>
/// Sim-authored fire-control facts for a contact. Must come from order-log / world
/// indicators, never from UI selection or chrome.
/// </summary>
public interface IKillChainFireControlSource
{
    bool HasFireControlTrack(string contactId, string targetId);
}

/// <summary>Current kill-chain contact picture row for C2 presentation.</summary>
public sealed record KillChainContactState(
    string ContactId,
    string TargetId,
    string ObserverId,
    KillChainPhase Phase,
    KillChainLossKind Loss,
    bool DetectionCaptured,
    bool LocationSufficient,
    bool TrackContinuous,
    bool Targetable,
    ulong FirstSimTick,
    double FirstSimTime,
    ulong LastSimTick,
    double LastSimTime,
    ulong CorrelationSequenceId,
    IReadOnlyList<ulong> SourceSequenceIds,
    IReadOnlyList<string> SourceRefs);

/// <summary>One deterministic F2T2 or loss transition, correlated to an order-log sequence.</summary>
public sealed record KillChainContactTransition(
    KillChainTransitionKind Kind,
    string ContactId,
    string TargetId,
    string ObserverId,
    KillChainPhase PreviousPhase,
    KillChainPhase NewPhase,
    KillChainLossKind Loss,
    ulong SimTick,
    double SimTime,
    ulong CorrelationSequenceId,
    IReadOnlyList<string> SourceRefs);

/// <summary>Replay-stable kill-chain projection: current contacts plus published transitions.</summary>
public sealed record KillChainContactSnapshot(
    IReadOnlyList<KillChainContactState> Contacts,
    IReadOnlyList<KillChainContactTransition> Transitions)
{
    public static KillChainContactSnapshot Empty { get; } =
        new(Array.Empty<KillChainContactState>(), Array.Empty<KillChainContactTransition>());
}

/// <summary>One C2 row for a kill-chain contact. Display-only; hosts must not re-derive phase.</summary>
public sealed record KillChainContactRow(
    string ContactId,
    string TargetId,
    string PhaseLabel,
    string PhaseClass,
    string DetectionLabel,
    string LocationLabel,
    string TrackLabel,
    string TargetabilityLabel,
    string LossLabel,
    string TimeLabel,
    string CorrelationLabel,
    string SourceLabel);

/// <summary>Headless C2 panel state for kill-chain contacts and published transitions.</summary>
public sealed record KillChainContactPanelState(
    string ContactCountLabel,
    string TransitionCountLabel,
    IReadOnlyList<KillChainContactRow> Rows,
    IReadOnlyList<string> TransitionLines)
{
    public static KillChainContactPanelState Empty { get; } =
        new("KC: 0", "KC-TX: 0", Array.Empty<KillChainContactRow>(), Array.Empty<string>());
}
