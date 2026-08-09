namespace ProjectAegis.Delegation.Sim;

using ProjectAegis.Delegation.Core;
using ProjectAegis.Sim.Swarm;

/// <summary>SWARM-23: agent/player request to issue a swarm intent through Delegation.</summary>
public sealed record SwarmAgentOrderRequest(
    string UnitId,
    SwarmIntentKind Intent,
    SwarmOrderActor Actor,
    ulong SimTick,
    double SimTime,
    AgentId? AgentId = null,
    double? TargetLatDeg = null,
    double? TargetLonDeg = null,
    string? AttackTargetUnitId = null,
    SwarmOperationalMode? Mode = null);
