namespace ProjectAegis.Delegation.UnityAdapter.Bridge;

using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;

/// <summary>Headless/Unity facade: project order log to HUD message lines.</summary>
public static class MessageLogBridge
{
    public static IReadOnlyList<MessageLogLine> ProjectFrom(DecisionLog log) =>
        MessageLogProjection.Project(log);

    public static IReadOnlyList<MessageLogLine> ProjectCombatMessages(DecisionLog log) =>
        ProjectFrom(log)
            .Where(m => m.Category is "KILL_CONFIRMED" or "INTERCEPT_SUCCESS" or "HIT" or "MISS" or "MAGAZINE")
            .ToArray();
}