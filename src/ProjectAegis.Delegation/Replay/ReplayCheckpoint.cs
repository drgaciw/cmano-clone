namespace ProjectAegis.Delegation.Replay;

/// <summary>World + log snapshot boundary for scrub-to-tick replay (order-log-replay GDD).</summary>
public sealed record ReplayCheckpoint(
    ulong SimTick,
    ulong WorldHash,
    string LogFingerprint,
    ulong LastSequenceId);