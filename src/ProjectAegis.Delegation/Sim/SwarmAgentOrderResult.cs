namespace ProjectAegis.Delegation.Sim;

using ProjectAegis.Delegation.Core;
using ProjectAegis.Sim.Swarm;

/// <summary>Result of <see cref="SwarmAgentIntentIssuer.TryIssue"/> with actor attribution.</summary>
public sealed record SwarmAgentOrderResult(
    bool Success,
    string? FailureReason,
    ulong SequenceId,
    SwarmOrderActor Actor,
    AgentId? AgentId,
    string UnitId,
    SwarmIntentKind Intent,
    SwarmOperationalMode? Mode);
