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
        var agentIds = log.Records
            .Select(r => r.AgentId.Value)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (agentIds.Length == 0)
        {
            agentIds = ["session"];
        }

        foreach (var agentId in agentIds)
        {
            var decisions = log.Records.Count(r => r.AgentId.Value == agentId);
            var overrides = log.ControllerChanges.Count(c =>
                c.NewKind == "Human" &&
                (c.AgentId?.Value == agentId || agentId == "session"));
            var roeViolations = log.PolicyDenials.Count;

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
