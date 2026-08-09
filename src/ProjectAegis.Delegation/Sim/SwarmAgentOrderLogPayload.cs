namespace ProjectAegis.Delegation.Sim;

using ProjectAegis.Delegation.Core;
using ProjectAegis.Sim.Swarm;

/// <summary>SWARM-23: order-log style attribution payload for agent/player swarm intents.</summary>
public sealed record SwarmAgentOrderLogPayload(
    ulong SequenceId,
    ulong SimTick,
    double SimTime,
    string UnitId,
    SwarmIntentKind Intent,
    SwarmOrderActor Actor,
    AgentId? AgentId,
    SwarmOperationalMode? Mode = null,
    double? TargetLatDeg = null,
    double? TargetLonDeg = null,
    string? AttackTargetUnitId = null)
{
    /// <summary>Stable fingerprint fragment for determinism checks.</summary>
    public string Fingerprint() =>
        $"{SequenceId}|{SimTick}|{UnitId}|{Intent}|{Actor}|{AgentId?.Value}|{Mode}|{TargetLatDeg}|{TargetLonDeg}|{AttackTargetUnitId}";
}
