namespace ProjectAegis.Delegation.Trust;

using ProjectAegis.Delegation.Decision;

public static class TrustSignalEmitter
{
    public static IReadOnlyList<TrustSignal> EmitFromSession(
        DecisionLog log,
        bool missionSucceeded,
        double objectivesMetRatio = 1.0)
    {
        var signals = new List<TrustSignal>();

        // Seed agent ids from both decision records AND controller-change events so agents
        // that were overridden before producing any DecisionRecord still get trust signals.
        var agentIds = log.Records
            .Select(r => r.AgentId.Value)
            .Union(
                log.ControllerChanges
                    .Where(c => c.AgentId is not null)
                    .Select(c => c.AgentId!.Value),
                StringComparer.Ordinal)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (agentIds.Length == 0)
        {
            agentIds = ["session"];
        }

        foreach (var agentId in agentIds)
        {
            var isSessionFallback = agentId == "session";

            var decisions = log.Records.Count(r =>
                isSessionFallback || r.AgentId.Value == agentId);

            var overrides = log.ControllerChanges.Count(c =>
                c.NewKind == "Human" &&
                (isSessionFallback || c.AgentId?.Value == agentId));

            // Filter policy denials to the responsible agent; session fallback includes all.
            var roeViolations = log.PolicyDenials.Count(d =>
                isSessionFallback || d.AgentId.Value == agentId);

            signals.Add(new TrustSignal(agentId, "missions_succeeded", missionSucceeded ? 1.0 : 0.0));
            signals.Add(new TrustSignal(agentId, "objectives_met_ratio", objectivesMetRatio));
            signals.Add(new TrustSignal(agentId, "roe_violations", roeViolations));
            signals.Add(new TrustSignal(agentId, "friendly_fire_incidents", 0.0));
            signals.Add(new TrustSignal(
                agentId,
                "player_override_rate",
                decisions == 0 ? 0.0 : overrides / (double)decisions));
        }

        return signals;
    }
}
