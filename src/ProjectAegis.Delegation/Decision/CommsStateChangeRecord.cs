namespace ProjectAegis.Delegation.Decision;

using ProjectAegis.Delegation.Comms;

/// <summary>Order-log comms transition (doc 19 / cyber-comms GDD).</summary>
public sealed record CommsStateChangeRecord(
    ulong SequenceId,
    double SimTime,
    ulong SimTick,
    string NodeId,
    CommsState PreviousState,
    CommsState NewState,
    string Reason);