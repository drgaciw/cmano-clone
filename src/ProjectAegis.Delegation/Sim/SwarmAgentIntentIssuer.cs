namespace ProjectAegis.Delegation.Sim;

using ProjectAegis.Delegation.Core;
using ProjectAegis.Sim.Swarm;

/// <summary>
/// SWARM-23 / B8 (DRG-100): agents issue the same swarm intents as humans via
/// <see cref="SwarmController"/> public APIs, with actor attribution on the result/payload.
/// Pure Delegation surface — does not modify Sim or Projection swarm files.
/// </summary>
public sealed class SwarmAgentIntentIssuer
{
    public const string ReasonUnknownUnit = "UNKNOWN_UNIT";
    public const string ReasonMissingAgentId = "MISSING_AGENT_ID";
    public const string ReasonInvalidRequest = "INVALID_REQUEST";
    public const string ReasonLinkLost = "LINK_LOST";
    public const string ReasonInvalidAttackTarget = "INVALID_ATTACK_TARGET";
    public const string ReasonControllerError = "CONTROLLER_ERROR";

    private readonly SwarmController _controller;
    private readonly List<SwarmAgentOrderLogPayload> _attributionLog = new();

    public SwarmAgentIntentIssuer(SwarmController controller)
    {
        _controller = controller ?? throw new ArgumentNullException(nameof(controller));
    }

    public IReadOnlyList<SwarmAgentOrderLogPayload> AttributionLog => _attributionLog;

    public SwarmAgentOrderResult TryIssue(SwarmAgentOrderRequest request)
    {
        if (request is null)
        {
            return Fail(ReasonInvalidRequest, SwarmOrderActor.Player, null, "", SwarmIntentKind.Hold, null);
        }

        if (string.IsNullOrWhiteSpace(request.UnitId))
        {
            return Fail(ReasonInvalidRequest, request.Actor, request.AgentId, "", request.Intent, request.Mode);
        }

        var unitId = request.UnitId.Trim();

        if (request.Actor == SwarmOrderActor.Agent &&
            (request.AgentId is null || string.IsNullOrWhiteSpace(request.AgentId.Value.Value)))
        {
            return Fail(ReasonMissingAgentId, request.Actor, request.AgentId, unitId, request.Intent, request.Mode);
        }

        if (!_controller.Contains(unitId))
        {
            return Fail(ReasonUnknownUnit, request.Actor, request.AgentId, unitId, request.Intent, request.Mode);
        }

        try
        {
            SwarmOperationalMode? mode = request.Mode;
            if (mode is { } m)
            {
                _controller.IssueMode(unitId, m, request.SimTick, request.SimTime);
            }

            ulong sequenceId = request.Intent switch
            {
                SwarmIntentKind.Hold => _controller.IssueHold(unitId, request.SimTick, request.SimTime),
                SwarmIntentKind.Move => IssueMove(unitId, request),
                SwarmIntentKind.Attack => IssueAttack(unitId, request),
                _ => throw new ArgumentException($"Unsupported intent {request.Intent}."),
            };

            var payload = new SwarmAgentOrderLogPayload(
                sequenceId,
                request.SimTick,
                request.SimTime,
                unitId,
                request.Intent,
                request.Actor,
                request.AgentId,
                mode,
                request.TargetLatDeg,
                request.TargetLonDeg,
                request.AttackTargetUnitId);
            _attributionLog.Add(payload);

            return new SwarmAgentOrderResult(
                true,
                null,
                sequenceId,
                request.Actor,
                request.AgentId,
                unitId,
                request.Intent,
                mode);
        }
        catch (InvalidOperationException ex) when (
            ex.Message.Contains("link lost", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("Orders blocked", StringComparison.OrdinalIgnoreCase))
        {
            return Fail(ReasonLinkLost, request.Actor, request.AgentId, unitId, request.Intent, request.Mode);
        }
        catch (ArgumentException)
        {
            return Fail(ReasonInvalidRequest, request.Actor, request.AgentId, unitId, request.Intent, request.Mode);
        }
        catch (Exception)
        {
            return Fail(ReasonControllerError, request.Actor, request.AgentId, unitId, request.Intent, request.Mode);
        }
    }

    /// <summary>Mode-only issue (Assault/Screen/etc.) keeping Hold intent.</summary>
    public SwarmAgentOrderResult TryIssueMode(
        string unitId,
        SwarmOperationalMode mode,
        SwarmOrderActor actor,
        ulong simTick,
        double simTime,
        AgentId? agentId = null)
    {
        return TryIssue(new SwarmAgentOrderRequest(
            unitId,
            SwarmIntentKind.Hold,
            actor,
            simTick,
            simTime,
            agentId,
            Mode: mode));
    }

    private ulong IssueMove(string unitId, SwarmAgentOrderRequest request)
    {
        if (request.TargetLatDeg is null || request.TargetLonDeg is null)
        {
            throw new ArgumentException("Move requires TargetLatDeg and TargetLonDeg.");
        }

        return _controller.IssueMove(
            unitId,
            request.TargetLatDeg.Value,
            request.TargetLonDeg.Value,
            request.SimTick,
            request.SimTime);
    }

    private ulong IssueAttack(string unitId, SwarmAgentOrderRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.AttackTargetUnitId))
        {
            throw new ArgumentException(ReasonInvalidAttackTarget);
        }

        return _controller.IssueAttack(
            unitId,
            request.AttackTargetUnitId,
            request.SimTick,
            request.SimTime,
            request.TargetLatDeg,
            request.TargetLonDeg);
    }

    private static SwarmAgentOrderResult Fail(
        string reason,
        SwarmOrderActor actor,
        AgentId? agentId,
        string unitId,
        SwarmIntentKind intent,
        SwarmOperationalMode? mode) =>
        new(false, reason, 0, actor, agentId, unitId, intent, mode);
}
