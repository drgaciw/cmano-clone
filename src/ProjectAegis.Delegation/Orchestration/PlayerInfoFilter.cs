namespace ProjectAegis.Delegation.Orchestration;

using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Sim.Scenario;

/// <summary>Filters order-log entries for live player HUD (req 02). Replay/AAR uses full log.</summary>
public static class PlayerInfoFilter
{
    public static IReadOnlyList<OrderLogEntry> FilterLiveEntries(
        IReadOnlyList<OrderLogEntry> entries,
        PlayerInfoModel model)
    {
        if (model == PlayerInfoModel.FullTransparency)
        {
            return entries;
        }

        return entries.Where(e => IsVisibleInLiveView(e, model)).ToArray();
    }

    public static bool IsDecisionVisibleInLiveView(AutonomyLevel autonomy, PlayerInfoModel model) =>
        model switch
        {
            PlayerInfoModel.FullTransparency => true,
            PlayerInfoModel.DelegationFog => autonomy < AutonomyLevel.FullAutonomous,
            PlayerInfoModel.TieredByAutonomy => autonomy < AutonomyLevel.FullAutonomous,
            _ => true,
        };

    private static bool IsVisibleInLiveView(OrderLogEntry entry, PlayerInfoModel model) =>
        entry.Kind switch
        {
            OrderLogEntryKind.AgentDecision when entry.Payload is DecisionRecord record =>
                IsDecisionVisibleInLiveView(record.AutonomyLevel, model),
            OrderLogEntryKind.PolicyDenial or OrderLogEntryKind.Engagement => true,
            _ => true,
        };
}
